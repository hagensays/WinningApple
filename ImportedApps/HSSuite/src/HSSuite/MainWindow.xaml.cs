using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HSSuite.Infrastructure;
using HSSuite.Models;
using HSSuite.Services;

namespace HSSuite
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
            Activated += MainWindow_Activated;
            RefreshApps();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            RefreshApps();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshApps();
        }

        private void AppTile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var path = button == null ? null : button.Tag as string;

            string error;
            if (!AppLaunchService.TryLaunch(path, out error))
            {
                StatusTextBlock.Text = error;
                return;
            }

            StatusTextBlock.Text = "Anwendung geöffnet.";
        }

        private void RefreshApps()
        {
            IReadOnlyList<SuiteApp> apps;
            try
            {
                apps = AppDiscoveryService.Discover();
            }
            catch (Exception ex)
            {
                AppsItemsControl.ItemsSource = null;
                EmptyStateBorder.Visibility = Visibility.Visible;
                StatusTextBlock.Text = "Apps konnten nicht gelesen werden: " + ex.Message;
                return;
            }

            AppsItemsControl.ItemsSource = apps;
            EmptyStateBorder.Visibility = apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusTextBlock.Text = apps.Count == 1
                ? "1 HS-App gefunden."
                : string.Format("{0} HS-Apps gefunden.", apps.Count);
        }
    }
}
