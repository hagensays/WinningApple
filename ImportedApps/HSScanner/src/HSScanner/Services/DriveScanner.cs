using HSScanner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HSScanner.Services
{
    public sealed class DriveScanner
    {
        private sealed class PendingDirectory
        {
            public string Path { get; set; }
            public long? ParentId { get; set; }
            public int Level { get; set; }
        }

        public ScanResult Scan(
            string rootPath,
            int cutoffYears,
            bool includeHidden,
            bool includeSystem,
            bool skipReparsePoints,
            CancellationToken cancellationToken,
            Action<string, long, long> progress)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("A scan path is required.", "rootPath");
            rootPath = Path.GetFullPath(rootPath);
            if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException(rootPath);
            if (cutoffYears < 1 || cutoffYears > 100) throw new ArgumentOutOfRangeException("cutoffYears");

            var result = new ScanResult
            {
                RootPath = rootPath,
                StartedAt = DateTime.Now,
                CutoffYears = cutoffYears,
                CutoffDate = DateTime.Today.AddYears(-cutoffYears),
                IncludeHidden = includeHidden,
                IncludeSystem = includeSystem,
                SkipReparsePoints = skipReparsePoints
            };

            var foldersById = new Dictionary<long, FolderRecord>();
            var pending = new Stack<PendingDirectory>();
            pending.Push(new PendingDirectory { Path = rootPath, ParentId = null, Level = 0 });
            long nextFolderId = 1;
            long nextFileSequence = 1;

            while (pending.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.WasCancelled = true;
                    break;
                }

                var current = pending.Pop();
                var folder = new FolderRecord
                {
                    Id = nextFolderId++,
                    ParentId = current.ParentId,
                    Level = current.Level,
                    FullPath = current.Path,
                    RelativePath = GetRelativePath(rootPath, current.Path),
                    Name = GetFolderName(current.Path)
                };
                foldersById[folder.Id] = folder;
                result.Folders.Add(folder);

                progress?.Invoke(current.Path, result.Folders.Count, result.Files.Count);

                FileInfo[] files;
                try
                {
                    files = new DirectoryInfo(current.Path).GetFiles();
                }
                catch (Exception ex) when (IsExpectedFileSystemException(ex))
                {
                    result.Errors.Add(CreateError(current.Path, "Dateien auflisten", ex));
                    files = new FileInfo[0];
                }

                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        if (!ShouldInclude(file.Attributes, includeHidden, includeSystem)) continue;

                        var creation = file.CreationTime;
                        var modified = file.LastWriteTime;
                        var referenceDate = creation > modified ? creation : modified;
                        var older = referenceDate.Date < result.CutoffDate.Date;
                        var record = new FileRecord
                        {
                            Sequence = nextFileSequence++,
                            FolderId = folder.Id,
                            FolderLevel = folder.Level,
                            FullPath = file.FullName,
                            RelativePath = GetRelativePath(rootPath, file.FullName),
                            Name = file.Name,
                            Extension = file.Extension,
                            SizeBytes = file.Length,
                            CreationTime = creation,
                            LastWriteTime = modified,
                            LastAccessTime = file.LastAccessTime,
                            ReferenceDate = referenceDate,
                            OlderThanCutoff = older,
                            Attributes = file.Attributes.ToString()
                        };

                        result.Files.Add(record);
                        folder.DirectFileCount++;
                        folder.DirectSizeBytes += record.SizeBytes;
                        folder.RecursiveFileCount++;
                        folder.RecursiveSizeBytes += record.SizeBytes;
                        if (older)
                        {
                            folder.OlderThanCutoffFileCount++;
                            folder.OlderThanCutoffSizeBytes += record.SizeBytes;
                        }
                        folder.OldestReferenceDate = Min(folder.OldestReferenceDate, referenceDate);
                        folder.NewestReferenceDate = Max(folder.NewestReferenceDate, referenceDate);
                    }
                    catch (Exception ex) when (IsExpectedFileSystemException(ex))
                    {
                        result.Errors.Add(CreateError(file.FullName, "Dateimetadaten lesen", ex));
                    }
                }

                if (result.WasCancelled) break;

                DirectoryInfo[] directories;
                try
                {
                    directories = new DirectoryInfo(current.Path).GetDirectories();
                }
                catch (Exception ex) when (IsExpectedFileSystemException(ex))
                {
                    result.Errors.Add(CreateError(current.Path, "Unterordner auflisten", ex));
                    directories = new DirectoryInfo[0];
                }

                for (var i = directories.Length - 1; i >= 0; i--)
                {
                    var directory = directories[i];
                    try
                    {
                        var attributes = directory.Attributes;
                        if (!ShouldInclude(attributes, includeHidden, includeSystem)) continue;
                        if (skipReparsePoints && (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) continue;

                        pending.Push(new PendingDirectory
                        {
                            Path = directory.FullName,
                            ParentId = folder.Id,
                            Level = folder.Level + 1
                        });
                    }
                    catch (Exception ex) when (IsExpectedFileSystemException(ex))
                    {
                        result.Errors.Add(CreateError(directory.FullName, "Ordnerattribute lesen", ex));
                    }
                }
            }

            AggregateFolders(result.Folders, foldersById);
            result.FinishedAt = DateTime.Now;
            return result;
        }

        private static void AggregateFolders(List<FolderRecord> folders, Dictionary<long, FolderRecord> foldersById)
        {
            foreach (var folder in folders.OrderByDescending(x => x.Level))
            {
                if (!folder.ParentId.HasValue) continue;
                FolderRecord parent;
                if (!foldersById.TryGetValue(folder.ParentId.Value, out parent)) continue;

                parent.RecursiveFileCount += folder.RecursiveFileCount;
                parent.RecursiveSizeBytes += folder.RecursiveSizeBytes;
                parent.OlderThanCutoffFileCount += folder.OlderThanCutoffFileCount;
                parent.OlderThanCutoffSizeBytes += folder.OlderThanCutoffSizeBytes;
                parent.OldestReferenceDate = Min(parent.OldestReferenceDate, folder.OldestReferenceDate);
                parent.NewestReferenceDate = Max(parent.NewestReferenceDate, folder.NewestReferenceDate);
            }
        }

        private static bool ShouldInclude(FileAttributes attributes, bool includeHidden, bool includeSystem)
        {
            if (!includeHidden && (attributes & FileAttributes.Hidden) == FileAttributes.Hidden) return false;
            if (!includeSystem && (attributes & FileAttributes.System) == FileAttributes.System) return false;
            return true;
        }

        private static string GetRelativePath(string rootPath, string fullPath)
        {
            var rootFull = Path.GetFullPath(rootPath);
            var full = Path.GetFullPath(fullPath);
            if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), rootFull.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return ".";
            var root = EnsureTrailingSeparator(rootFull);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return full.Substring(root.Length);
            return full;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) return path;
            return path + Path.DirectorySeparatorChar;
        }

        private static string GetFolderName(string path)
        {
            var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(name) ? path : name;
        }

        private static DateTime? Min(DateTime? current, DateTime candidate)
        {
            return !current.HasValue || candidate < current.Value ? candidate : current;
        }

        private static DateTime? Min(DateTime? left, DateTime? right)
        {
            if (!left.HasValue) return right;
            if (!right.HasValue) return left;
            return left.Value < right.Value ? left : right;
        }

        private static DateTime? Max(DateTime? current, DateTime candidate)
        {
            return !current.HasValue || candidate > current.Value ? candidate : current;
        }

        private static DateTime? Max(DateTime? left, DateTime? right)
        {
            if (!left.HasValue) return right;
            if (!right.HasValue) return left;
            return left.Value > right.Value ? left : right;
        }

        private static ScanError CreateError(string path, string operation, Exception ex)
        {
            return new ScanError
            {
                Time = DateTime.Now,
                Path = path,
                Operation = operation,
                Message = ex.Message
            };
        }

        private static bool IsExpectedFileSystemException(Exception ex)
        {
            return ex is UnauthorizedAccessException
                || ex is IOException
                || ex is System.Security.SecurityException
                || ex is PathTooLongException
                || ex is NotSupportedException;
        }
    }
}
