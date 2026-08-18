using HSScanner.Infrastructure;
using HSScanner.Models;
using HSScanner.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace HSScanner
{
    public partial class MainWindow : Window
    {
        private sealed class DriveOption
        {
            public string Path { get; set; }
            public string Label { get; set; }
            public override string ToString() { return Label; }
        }

        private CancellationTokenSource _scanCancellation;
        private ScanResult _lastResult;

        public MainWindow()
        {
            InitializeComponent();
            OutputFolderText.Text = OutputPathService.ApplicationDirectory;
            LoadDriveOptions();
        }

        private void LoadDriveOptions()
        {
            var options = new List<DriveOption>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (!drive.IsReady) continue;
                    var label = drive.Name + "  •  " + drive.DriveType;
                    if (!string.IsNullOrWhiteSpace(drive.VolumeLabel)) label += "  •  " + drive.VolumeLabel;
                    options.Add(new DriveOption { Path = drive.RootDirectory.FullName, Label = label });
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            DriveComboBox.ItemsSource = options;
            if (options.Count > 0) DriveComboBox.SelectedIndex = 0;
        }

        private void DriveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var option = DriveComboBox.SelectedItem as DriveOption;
            if (option != null) SourcePathTextBox.Text = option.Path;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = "Laufwerk oder Stammordner für den read-only Scan auswählen";
                dialog.ShowNewFolderButton = false;
                if (Directory.Exists(SourcePathTextBox.Text)) dialog.SelectedPath = SourcePathTextBox.Text;
                if (dialog.ShowDialog() == Forms.DialogResult.OK) SourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var path = (SourcePathTextBox.Text ?? string.Empty).Trim();
            int cutoffYears;
            if (!Directory.Exists(path))
            {
                MessageBox.Show(this, "Der gewählte Scan-Pfad ist nicht erreichbar.", "HSScanner", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(CutoffYearsTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out cutoffYears) || cutoffYears < 1 || cutoffYears > 100)
            {
                MessageBox.Show(this, "Bitte einen Stichtag zwischen 1 und 100 Jahren eingeben.", "HSScanner", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _scanCancellation = new CancellationTokenSource();
            SetScanningState(true);
            ClearResultViews();
            StatusText.Text = "Scan wird gestartet…";

            var scanner = new DriveScanner();
            var includeHidden = IncludeHiddenCheckBox.IsChecked == true;
            var includeSystem = IncludeSystemCheckBox.IsChecked == true;
            var skipReparse = SkipReparseCheckBox.IsChecked == true;
            try
            {
                var progress = new Action<string, long, long>((currentPath, folderCount, fileCount) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FolderCountText.Text = folderCount.ToString("N0", CultureInfo.GetCultureInfo("de-DE"));
                        FileCountText.Text = fileCount.ToString("N0", CultureInfo.GetCultureInfo("de-DE"));
                        StatusText.Text = "Lese: " + currentPath;
                    }));
                });

                _lastResult = await Task.Run(() => scanner.Scan(
                    path,
                    cutoffYears,
                    includeHidden,
                    includeSystem,
                    skipReparse,
                    _scanCancellation.Token,
                    progress));

                BindResult(_lastResult);
                StatusText.Text = _lastResult.WasCancelled
                    ? "Scan abgebrochen. Das Teilergebnis kann trotzdem exportiert werden."
                    : "Scan abgeschlossen. Keine Quelldatei wurde verändert.";
            }
            catch (Exception ex)
            {
                _lastResult = null;
                MessageBox.Show(this, "Scan fehlgeschlagen:\n\n" + ex.Message, "HSScanner", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Scan fehlgeschlagen.";
            }
            finally
            {
                SetScanningState(false);
                if (_scanCancellation != null)
                {
                    _scanCancellation.Dispose();
                    _scanCancellation = null;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scanCancellation != null)
            {
                _scanCancellation.Cancel();
                StatusText.Text = "Abbruch angefordert…";
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResult == null) return;

            ExportButton.IsEnabled = false;
            StatusText.Text = "Excel-Datei wird neben der EXE erstellt…";
            try
            {
                var exporter = new ExcelExporter();
                var path = await Task.Run(() => exporter.Export(_lastResult));
                StatusText.Text = "Export erstellt: " + path;
                MessageBox.Show(this,
                    "Excel-Export erstellt.\n\n" + path + "\n\nExistierende Dateien wurden nicht überschrieben.",
                    "HSScanner",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Export fehlgeschlagen.";
                MessageBox.Show(this, "Excel-Export fehlgeschlagen:\n\n" + ex.Message, "HSScanner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExportButton.IsEnabled = _lastResult != null;
            }
        }

        private void SetScanningState(bool scanning)
        {
            ScanButton.IsEnabled = !scanning;
            CancelButton.IsEnabled = scanning;
            ExportButton.IsEnabled = !scanning && _lastResult != null;
            SourcePathTextBox.IsEnabled = !scanning;
            DriveComboBox.IsEnabled = !scanning;
            CutoffYearsTextBox.IsEnabled = !scanning;
            IncludeHiddenCheckBox.IsEnabled = !scanning;
            IncludeSystemCheckBox.IsEnabled = !scanning;
            SkipReparseCheckBox.IsEnabled = !scanning;
            ScanProgressBar.IsIndeterminate = scanning;
        }

        private void ClearResultViews()
        {
            _lastResult = null;
            FoldersGrid.ItemsSource = null;
            FilesGrid.ItemsSource = null;
            ErrorsGrid.ItemsSource = null;
            FolderCountText.Text = "0";
            FileCountText.Text = "0";
            OldFileCountText.Text = "0";
            ErrorCountText.Text = "0";
            SummaryFoldersText.Text = "0";
            SummaryFilesText.Text = "0";
            SummarySizeText.Text = "0 B";
            SummaryOldText.Text = "0";
            SummaryStatusText.Text = "Scan läuft…";
        }

        private void BindResult(ScanResult result)
        {
            FoldersGrid.ItemsSource = result.Folders;
            FilesGrid.ItemsSource = result.Files;
            ErrorsGrid.ItemsSource = result.Errors;

            var culture = CultureInfo.GetCultureInfo("de-DE");
            FolderCountText.Text = result.Folders.Count.ToString("N0", culture);
            FileCountText.Text = result.Files.Count.ToString("N0", culture);
            OldFileCountText.Text = result.OlderThanCutoffFiles.ToString("N0", culture);
            ErrorCountText.Text = result.Errors.Count.ToString("N0", culture);
            SummaryFoldersText.Text = FolderCountText.Text;
            SummaryFilesText.Text = FileCountText.Text;
            SummarySizeText.Text = FormatBytes(result.TotalBytes);
            SummaryOldText.Text = result.OlderThanCutoffFiles.ToString("N0", culture);
            SummaryStatusText.Text = string.Format(culture,
                "Quelle: {0}\nStichtag: {1:dd.MM.yyyy} ({2} Jahre)\nPrüffälle: {3:N0} Dateien / {4}\nNicht lesbar oder fehlerhaft: {5:N0}\nDauer: {6}",
                result.RootPath,
                result.CutoffDate,
                result.CutoffYears,
                result.OlderThanCutoffFiles,
                FormatBytes(result.OlderThanCutoffBytes),
                result.Errors.Count,
                result.FinishedAt - result.StartedAt);

            ExportButton.IsEnabled = true;
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
