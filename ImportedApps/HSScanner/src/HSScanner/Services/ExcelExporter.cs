using HSScanner.Infrastructure;
using HSScanner.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HSScanner.Services
{
    public sealed class ExcelExporter
    {
        private const int MaxDataRowsPerSheet = 1000000;

        public string Export(ScanResult result)
        {
            if (result == null) throw new ArgumentNullException("result");
            if (result.FinishedAt == default(DateTime)) throw new InvalidOperationException("The scan is not complete.");

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var output = OutputPathService.GetUniqueOutputPath("HSScanner_Export_" + stamp, ".xlsx");
            SimpleXlsxWriter.Write(output, BuildSheets(result));
            return output;
        }

        private static List<SimpleXlsxWriter.Sheet> BuildSheets(ScanResult result)
        {
            var sheets = new List<SimpleXlsxWriter.Sheet>();

            var summary = new List<object[]>
            {
                Row("Quelle", result.RootPath),
                Row("Scan gestartet", Date(result.StartedAt)),
                Row("Scan beendet", Date(result.FinishedAt)),
                Row("Dauer", (result.FinishedAt - result.StartedAt).ToString("c", CultureInfo.InvariantCulture)),
                Row("Status", result.WasCancelled ? "Abgebrochen – Teilergebnis" : "Vollständig durchlaufen (soweit lesbar)"),
                Row("Stichtag (Jahre)", result.CutoffYears),
                Row("Stichtagsdatum", result.CutoffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Row("Ordner", result.Folders.Count),
                Row("Dateien", result.Files.Count),
                Row("Gesamtgröße", FormatBytes(result.TotalBytes)),
                Row("Dateien älter als Stichtag", result.OlderThanCutoffFiles),
                Row("Größe älter als Stichtag", FormatBytes(result.OlderThanCutoffBytes)),
                Row("Nicht lesbar / fehlerhaft", result.Errors.Count),
                Row("Hidden einbezogen", YesNo(result.IncludeHidden)),
                Row("System einbezogen", YesNo(result.IncludeSystem)),
                Row("Reparse Points übersprungen", YesNo(result.SkipReparsePoints)),
                Row("Exportordner", OutputPathService.ApplicationDirectory),
                Row("Wichtiger Hinweis", "Der Stichtag ist nur ein Planungsfilter. HSScanner trifft keine rechtliche Löschentscheidung und löscht oder verändert keine Quelldateien.")
            };
            sheets.Add(SimpleXlsxWriter.FromRows("Übersicht", new[] { "Feld", "Wert" }, summary));

            var folderHeaders = new[]
            {
                "Ordner-ID", "Parent-ID", "Level", "Ordnername", "Relativer Pfad", "Vollständiger Pfad",
                "Dateien direkt", "Dateien inkl. Unterordner", "Größe direkt (Bytes)", "Größe inkl. Unterordner (Bytes)",
                "Älter als Stichtag – Dateien", "Älter als Stichtag – Bytes", "Ältestes Referenzdatum", "Neuestes Referenzdatum",
                "Prüfer / Verantwortlich", "Entscheidung", "Begründung / Notiz", "Prüfdatum", "Freigabe durch", "Löschdatum", "Unterschrift / Nachweis"
            };
            sheets.Add(new SimpleXlsxWriter.Sheet
            {
                Name = "Ordner",
                Headers = folderHeaders,
                RowCount = result.Folders.Count,
                RowFactory = i =>
                {
                    var f = result.Folders[i];
                    return new object[]
                    {
                        f.Id, f.ParentId.HasValue ? (object)f.ParentId.Value : "", f.Level, f.Name, f.RelativePath, f.FullPath,
                        f.DirectFileCount, f.RecursiveFileCount, f.DirectSizeBytes, f.RecursiveSizeBytes,
                        f.OlderThanCutoffFileCount, f.OlderThanCutoffSizeBytes, Date(f.OldestReferenceDate), Date(f.NewestReferenceDate),
                        "", "", "", "", "", "", ""
                    };
                }
            });

            AddFileSheets(sheets, result.Files);

            sheets.Add(new SimpleXlsxWriter.Sheet
            {
                Name = "Fehler",
                Headers = new[] { "Zeit", "Pfad", "Vorgang", "Meldung" },
                RowCount = result.Errors.Count,
                RowFactory = i =>
                {
                    var e = result.Errors[i];
                    return new object[] { Date(e.Time), e.Path, e.Operation, e.Message };
                }
            });

            var notes = new List<object[]>
            {
                Row("Read-only", "HSScanner liest Verzeichnis- und Dateimetadaten. Es löscht, verschiebt, kopiert oder verändert keine Quelldateien."),
                Row("Ausgabepfad", "Exporte werden ausschließlich im Ordner der gestarteten HSScanner.exe angelegt. Bestehende Dateien werden niemals überschrieben."),
                Row("Stichtag", "Standardmäßig 11 Jahre. Der Wert ist ein technischer Filter für die Planung, keine Aussage darüber, was rechtlich gelöscht werden muss."),
                Row("Referenzdatum", "Das spätere Datum aus Erstellungsdatum und Änderungsdatum. Nur dieses Datum wird für den Stichtagsfilter verwendet."),
                Row("Ordner-Level", "Level 0 ist die gewählte Scan-Wurzel, Level 1 deren direkte Unterordner, Level 2 die nächste Ebene usw."),
                Row("Parent-ID", "Verknüpft jeden Ordner mit seinem übergeordneten Ordner."),
                Row("Zugriffsfehler", "Nicht lesbare Elemente werden nicht erzwungen geöffnet; sie erscheinen im Blatt Fehler."),
                Row("Reparse Points", "Standardmäßig übersprungen, um Schleifen und unerwartete Sprünge in andere Verzeichnisbäume zu vermeiden."),
                Row("Excel-Limit", "Sehr große Dateilisten werden automatisch auf mehrere Dateien-Blätter verteilt.")
            };
            sheets.Add(SimpleXlsxWriter.FromRows("Hinweise", new[] { "Hinweis", "Bedeutung" }, notes));
            return sheets;
        }

        private static void AddFileSheets(List<SimpleXlsxWriter.Sheet> sheets, List<FileRecord> files)
        {
            var headers = new[]
            {
                "Nr.", "Ordner-ID", "Ordner-Level", "Dateiname", "Erweiterung", "Relativer Pfad", "Vollständiger Pfad",
                "Größe (Bytes)", "Erstellt", "Geändert", "Letzter Zugriff", "Referenzdatum", "Älter als Stichtag", "Attribute"
            };

            var sheetCount = Math.Max(1, (int)Math.Ceiling(files.Count / (double)MaxDataRowsPerSheet));
            for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                var start = sheetIndex * MaxDataRowsPerSheet;
                var count = Math.Min(MaxDataRowsPerSheet, Math.Max(0, files.Count - start));
                var localStart = start;
                var localCount = count;
                sheets.Add(new SimpleXlsxWriter.Sheet
                {
                    Name = sheetCount == 1 ? "Dateien" : "Dateien " + (sheetIndex + 1),
                    Headers = headers,
                    RowCount = localCount,
                    RowFactory = i =>
                    {
                        var f = files[localStart + i];
                        return new object[]
                        {
                            f.Sequence, f.FolderId, f.FolderLevel, f.Name, f.Extension, f.RelativePath, f.FullPath, f.SizeBytes,
                            Date(f.CreationTime), Date(f.LastWriteTime), Date(f.LastAccessTime), Date(f.ReferenceDate), YesNo(f.OlderThanCutoff), f.Attributes
                        };
                    }
                });
            }
        }

        private static object[] Row(object left, object right)
        {
            return new[] { left, right };
        }

        private static string Date(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string Date(DateTime? value)
        {
            return value.HasValue ? Date(value.Value) : string.Empty;
        }

        private static string YesNo(bool value)
        {
            return value ? "Ja" : "Nein";
        }

        private static string FormatBytes(long bytes)
        {
            var value = (double)bytes;
            var units = new[] { "B", "KiB", "MiB", "GiB", "TiB" };
            var unit = 0;
            while (value >= 1024.0 && unit < units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }
            return value.ToString(unit == 0 ? "N0" : "N2", CultureInfo.GetCultureInfo("de-DE")) + " " + units[unit];
        }
    }
}
