using System.Windows;
using System.Windows.Input;
using HSTemplate.Infrastructure;

namespace HSTemplate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = SuiteInfo.DisplayName;
            AppNameTextBlock.Text = SuiteInfo.DisplayName;
            AppDescriptionTextBlock.Text = SuiteInfo.Description;
            VersionTextBlock.Text = VersionInfo.DisplayVersion;

            var suiteAvailable = SuiteLauncherService.IsAvailable;
            SuiteHomeButton.IsHitTestVisible = suiteAvailable;
            SuiteHomeButton.Cursor = suiteAvailable ? Cursors.Hand : Cursors.Arrow;
            SuiteHomeButton.ToolTip = suiteAvailable ? "HSSuite öffnen" : null;
        }

        private void SuiteHomeButton_Click(object sender, RoutedEventArgs e)
        {
            string error;
            if (!SuiteLauncherService.TryOpen(out error))
            {
                StatusTextBlock.Text = error;
            }
        }

        private void PrimaryAction_Click(object sender, RoutedEventArgs e)
        {
            ExampleProgressBar.Value = ExampleProgressBar.Value >= 100 ? 0 : ExampleProgressBar.Value + 12;
            StatusTextBlock.Text = "Template action invoked — replace with product behavior.";
        }

        private void SecondaryAction_Click(object sender, RoutedEventArgs e)
        {
            ExampleInputTextBox.Clear();
            StatusTextBlock.Text = "Bereit";
        }
    }
}
