using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Net;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.IO.Compression;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace kOFR_Repo
{
    /// <summary>
    /// Interaction logic for Installer.xaml
    /// </summary>
    public partial class Installer : Window
    {
        string accentColour = SystemParameters.WindowGlassBrush.ToString();
        string AppsUseLightTheme = "#FFFFFFFF";
        string textColour = "#FF000000";
        string buttonColour = "#FFDDDDDD";

        private void setStyles()
        {
            windowBorder.Visibility = Visibility.Visible;

            try
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").GetValue("AppsUseLightTheme").ToString() == "0") //Dark theme
                {
                    AppsUseLightTheme = "#FF101011";
                    textColour = "#FFFFFF";
                    buttonColour = "#FF383838";
                }
            }
            catch { }

            Brush b(string hex) { return (Brush)new BrushConverter().ConvertFrom(hex); }

            background.Background = b(AppsUseLightTheme);
            titleBar.Background = b(AppsUseLightTheme);
            windowBorder.BorderBrush = b(accentColour);
            appTitle.Foreground = b(textColour);
            changeDIR.Background = b(AppsUseLightTheme);
            dir.Foreground = b(textColour);
            installDIR.Foreground = b(textColour);
            changeDIR.Foreground = b(textColour);
            closebtn.Background = b(AppsUseLightTheme);
            closebtn.Foreground = b(textColour);
            minimisebtn.Background = b(AppsUseLightTheme);
            minimisebtn.Foreground = b(textColour);
            downloadLogs.Foreground = b(textColour);
            downloadProgress.Background = b(buttonColour);
            downloadProgress.Foreground = b(accentColour);
            install.Background = b(AppsUseLightTheme);
            install.Foreground = b(textColour);
        }

        public Installer()
        {
            InitializeComponent();
            setStyles();

            string[] selectedPath = AppDomain.CurrentDomain.BaseDirectory.ToLower().Split('\\');
            if (selectedPath[selectedPath.Length - 2] != "kofr repo") { installDIR.Text = AppDomain.CurrentDomain.BaseDirectory + "kOFR Repo\\"; }
            else { installDIR.Text = AppDomain.CurrentDomain.BaseDirectory; }

            Activate();
        }

        private void titleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void closebtn_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void minimisebtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void changeDIR_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.InitialDirectory = installDIR.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string[] selectedPath = dialog.FileName.ToLower().Split('\\');
                if (selectedPath[selectedPath.Length - 1] != "kofr repo") { installDIR.Text = dialog.FileName + "\\kOFR Repo\\"; }
                else { installDIR.Text = dialog.FileName + "\\"; }
            }
        }

        private void install_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(installDIR.Text)) { Directory.CreateDirectory(installDIR.Text); }

            var bc = new BrushConverter();
            install.Background = (Brush)bc.ConvertFrom(AppsUseLightTheme);
            install.Foreground = (Brush)bc.ConvertFrom("#FF707070");
            changeDIR.Background = (Brush)bc.ConvertFrom(AppsUseLightTheme);
            changeDIR.Foreground = (Brush)bc.ConvertFrom("#FF707070");

            installDIR.IsEnabled = false;
            changeDIR.IsEnabled = false;
            install.IsEnabled = false;

            try
            {
                DirectoryInfo di = new DirectoryInfo(installDIR.Text);
                FileInfo[] files = di.GetFiles("*.zip").Where(p => p.Extension == ".zip").ToArray();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        File.Delete(file.FullName);
                    }
                    catch { }
                }
            }
            catch { }

            var webClient = new WebClient();
            webClient.Headers.Add("user-agent", "kOFReadie-RepoApp");

            Task.Run(async () =>
            {
                bool foundApp = false;
                bool foundDll = false;
                var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("kOFReadie-RepoApp"));
                var versions = await client.Repository.GetAllTags("kOFReadie", "Repo-App");
                foreach (Octokit.RepositoryTag release in versions)
                {
                    if (foundApp && foundDll) { break; }
                    else
                    {
                        if (!foundDll && release.Name.StartsWith("dll-")) { foundDll = true; kOFRRepoVersion = release.Name; }
                        else if (!foundApp && release.Name.StartsWith("app-")) { foundApp = true; hostVersion = release.Name; }
                    }
                }
            }).Wait();

            downloadLauncher();
        }

        string hostVersion = string.Empty;
        string kOFRRepoVersion = string.Empty;
        bool installDIRIsActiveDIR = false;

        private void downloadLauncher()
        {
            if (installDIR.Text != AppDomain.CurrentDomain.BaseDirectory)
            {
                try
                {
                    var webclient = new WebClient();
                    downloadLogs.AppendText($"Downloading 'kOFRRepoApp.exe'");
                    webclient.DownloadFileAsync(new Uri(
                        $"https://github.com/kOFReadie/Repo-App/releases/download/{hostVersion}/kOFRRepoApp.exe"), installDIR.Text + $"\\kOFRRepoApp.exe");
                    webclient.DownloadProgressChanged += client_DownloadProgressChanged;
                    webclient.DownloadFileCompleted += downloadkOFRRepoDLL;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }
            else { installDIRIsActiveDIR = true; downloadkOFRRepoDLL(null, null); }
        }

        private void downloadkOFRRepoDLL(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                var webclient = new WebClient();
                if (installDIRIsActiveDIR) { downloadLogs.AppendText($"Downloading 'kOFRRepo.dll'"); }
                else { downloadLogs.AppendText($"\nDownloading 'kOFRRepo.dll'"); }
                webclient.DownloadFileAsync(new Uri(
                        $"https://github.com/kOFReadie/Repo-App/releases/download/{kOFRRepoVersion}/kOFRRepo.dll"), installDIR.Text + $"\\kOFRRepo.dll");
                webclient.DownloadProgressChanged += client_DownloadProgressChanged;
                webclient.DownloadFileCompleted += fileTransfersComplete;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void fileTransfersComplete(object sender, AsyncCompletedEventArgs e)
        {
            downloadLogs.AppendText("\nCreating start menu shortcut...");
            CreateShortcut("kOFRRepoApp", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), installDIR.Text + "\\kOFRRepoApp.exe");
            downloadLogs.AppendText("\nLaunching kOFRRepoApp.exe");
            System.Threading.Thread.Sleep(50);
            Process.Start(installDIR.Text + "\\kOFRRepoApp.exe", "overrideInstances");
            Environment.Exit(0);
        }

        public void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            try
            {
                string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutLocation);
                shortcut.IconLocation = new Uri("https://raw.githubusercontent.com/kOFReadie/Repo-App/master/Icon.ico").ToString();
                shortcut.WorkingDirectory = installDIR.Text;
                shortcut.TargetPath = targetFileLocation;
                shortcut.Save();
            }
            catch (Exception ex) { downloadLogs.AppendText("\nFailed to make a shortcut in the Start Menu folder: " + ex); }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            downloadProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
        }

        private void downloadLogs_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { downloadLogs.ScrollToEnd(); }
    }
}
