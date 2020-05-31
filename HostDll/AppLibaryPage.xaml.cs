using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;

namespace kOFRRepo
{
    /// <summary>
    /// Interaction logic for AppLibaryPage.xaml
    /// </summary>
    public partial class AppLibaryPage : Page
    {
        AppPage ap;
        List<Grid> appTiles = new List<Grid>();
        List<WrapPanel> wrapPanels = new List<WrapPanel>();
        List<ProgressBar> progressBars = new List<ProgressBar>();
        public static List<Label> statusTexts = new List<Label>();
        public static List<string> runningPrograms = new List<string>(); //Consider using BindingList for update event triggers
        public bool completedStartupTasks = false;

        public AppLibaryPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            getApps();
            checkForUpdates();
            completedStartupTasks = true;
        }

        internal List<appData> appDataInfo = new List<appData>();

        private void getApps()
        {
            Task.Run(async () =>
            {
                if (AppLibaryWindow.startupArgs.Contains("noupdate")) { getOfflineModeApps(); }
                else
                {
                    try
                    {
                        IReadOnlyList<Octokit.Repository> repos = await httpGit.githubClient.Repository.GetAllForUser("kOFReadie");
                        foreach (Octokit.Repository repo in repos)
                        {
                            try
                            {
                                HttpResponseMessage response = await httpGit.httpClient.GetAsync
                                    ($"https://raw.githubusercontent.com/kOFReadie/{repo.Name}/master/AppInfo.json");
                                response.EnsureSuccessStatusCode();
                                try
                                {
                                    string responseContent = await response.Content.ReadAsStringAsync();
                                    appDataInfo.Add(JsonConvert.DeserializeObject<List<appData>>(responseContent)[0]);
                                }
                                catch (Exception ex) { msgBox.Show("Failed to get app information"); LogWriter.CreateLog(ex); }
                            }
                            catch { continue; }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.CreateLog(ex);
                        getOfflineModeApps();
                        msgBox.Show("Failed to get apps. Attempting to run in offline mode.\nError written to log file.");
                    }
                }
            }).Wait();

            if (appDataInfo == null) { msgBox.Show("Failed to get installed apps, no apps found.\nThis app will now close."); Environment.Exit(0); }
            foreach (var appInfo in appDataInfo)
            {
                if (appInfo.show == true || AppLibaryWindow.startupArgs.Contains("showall"))
                {
                    string aNameSub = string.Empty;
                    string[] sName = appInfo.appName.Split(' ');
                    foreach (string s in sName) { aNameSub = aNameSub + s; }

                    Grid appTilePlate = new Grid();
                    appTilePlate.Width = 105;
                    appTilePlate.HorizontalAlignment = HorizontalAlignment.Left;
                    appTilePlate.VerticalAlignment = VerticalAlignment.Top;
                    appTilePlate.Tag = appInfo.appName;
                    appTilePlate.Margin = new Thickness(5, 5, 5, 5);
                    appTilePlate.MouseDown += appPage;

                    ProgressBar progressBar = new ProgressBar();
                    progressBar.Visibility = Visibility.Hidden;
                    progressBar.Background = null;
                    progressBar.BorderBrush = null;
                    progressBar.Foreground = Styles.accent();
                    progressBar.Tag = $"pg{appInfo.appName}";

                    WrapPanel appTile = new WrapPanel();
                    appTile.Tag = appInfo.appName;
                    appTile.Orientation = Orientation.Vertical;
                    appTile.MouseLeave += appTile_MouseLeave;
                    appTile.MouseEnter += appTile_MouseEnter;

                    #region App Icon
                    BitmapImage appIconImage = new BitmapImage();
                    appIconImage.BeginInit();
                    appIconImage.StreamSource = new MemoryStream(Convert.FromBase64String(appInfo.icon));
                    appIconImage.EndInit();

                    Image appIcon = new Image();
                    appIcon.Height = 100;
                    appIcon.Width = 100;
                    appIcon.Margin = new Thickness(2.5, 0, 0, 0);
                    appIcon.Source = appIconImage;
                    #endregion

                    TextBlock appName = new TextBlock();
                    appName.FontWeight = FontWeights.Bold;
                    appName.Margin = new Thickness(5, 0, 0, 0);
                    appName.FontSize = 14;
                    appName.TextWrapping = TextWrapping.Wrap;
                    if (appInfo.appName.Length > 12) { appName.Text = appInfo.appName.Substring(0, 12) + "..."; }
                    else { appName.Text = appInfo.appName; }
                    appName.Foreground = Styles.text();

                    string installStatus;
                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll")) { installStatus = "Installed"; }
                    else { installStatus = "Install"; }

                    Label statusText = new Label();
                    statusText.FontSize = 11;
                    statusText.Margin = new Thickness(0, -5, 0, 0);
                    statusText.Content = installStatus;
                    statusText.Foreground = Styles.text();
                    statusText.Tag = appInfo.appName.ToLower().Replace(" ", "");

                    #region Tile Assembly
                    appTile.Children.Add(appIcon);
                    appTile.Children.Add(appName);
                    appTile.Children.Add(statusText);

                    appTilePlate.Children.Add(progressBar);
                    appTilePlate.Children.Add(appTile);
                    appTilesPane.Children.Insert(0, appTilePlate);
                    #endregion

                    appTiles.Add(appTilePlate);
                    wrapPanels.Add(appTile);
                    progressBars.Add(progressBar);
                    statusTexts.Add(statusText);
                }
            }
        }

        private void getOfflineModeApps()
        {
            AppLibaryWindow.startupArgs.Add("offlineMode");
            foreach (string dir in Directory.GetDirectories(Environment.CurrentDirectory))
            {
                foreach (string dll in Directory.GetFiles(dir, "*.dll"))
                {
                    try
                    {
                        var DLL = Assembly.LoadFile(dll); //Non-static only
                        foreach (Type type in DLL.GetExportedTypes())
                        {
                            if (type.ToString().Contains("AppInfo"))
                            {
                                dynamic AppInfo = Activator.CreateInstance(type);
                                appDataInfo.Add(new appData
                                {
                                    appName = AppInfo.name,
                                    description = AppInfo.description,
                                    show = AppInfo.showInOfflineMode,
                                    icon = AppInfo.icon,
                                    version = AppInfo.version
                                });
                                break;
                            }
                        }
                        GC.Collect(); //Collect all unused memory
                        GC.WaitForPendingFinalizers(); //Wait untill GC has finished working
                        GC.Collect(); //Finalises GC in background deleting all unused memory
                        //This is done because the DLLs cannot be unloaded using this method
                    }
                    catch (Exception ex) { LogWriter.CreateLog(ex); }
                }
            }
        }

        private void checkForUpdates()
        {
            if (!AppLibaryWindow.startupArgs.Contains("offlineMode"))
            {
                try
                {
                    foreach (Grid gridApp in appTiles)
                    {
                        foreach (var appInfo in appDataInfo)
                        {
                            if (appInfo.appName == gridApp.Tag.ToString())
                            {
                                string aNameSub = string.Empty;
                                string[] sName = appInfo.appName.Split(' ');
                                foreach (string s in sName) { aNameSub = aNameSub + s; }

                                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll"))
                                {
                                    var lv = FileVersionInfo.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll").FileVersion;
                                    var wv = appInfo.version + ".0";
                                    if (lv != wv) { downloadFile(appInfo.appName, appInfo.version + ".0"); }
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) { LogWriter.CreateLog(ex); }
            }
        }

        private void appTile_MouseLeave(object sender, MouseEventArgs e)
        {
            var appTile = sender as WrapPanel;
            appTile.Background = null;
        }

        private void appTile_MouseEnter(object sender, MouseEventArgs e)
        {
            var appTile = sender as WrapPanel;
            appTile.Background = Styles.accent();
        }

        private void appPage(object sender, RoutedEventArgs e)
        {
            var AI = sender as Grid;
            var mw = Application.Current.MainWindow as AppLibaryWindow;
            if (AI == null) { return; }
            else
            {
                foreach (var appInfo in appDataInfo)
                {
                    if (appInfo.appName == AI.Tag.ToString())
                    {
                        ap = new AppPage(appInfo.appName, appInfo.description, appInfo.icon, appInfo.version);
                        mw.appsFrame.Visibility = Visibility.Collapsed;
                        mw.appDetailsFrame.Visibility = Visibility.Visible;
                        mw.backBTN.Visibility = Visibility.Visible;
                        mw.appDetailsFrame.Content = ap;
                        mw.backBTN.Click += BackBTN_Click;
                        void BackBTN_Click(object senderB, RoutedEventArgs eB)
                        {
                            mw.appDetailsFrame.Content = null;
                            mw.backBTN.Visibility = Visibility.Collapsed;
                            mw.appDetailsFrame.Visibility = Visibility.Collapsed;
                            mw.appsFrame.Visibility = Visibility.Visible;
                        }
                        break;
                    }
                }
            }
        }

        public void downloadFile(string appName, string version)
        {
            try
            {
                var mw = Application.Current.MainWindow as AppLibaryWindow;
                Grid appTileButton = null;
                foreach (Grid g in appTiles) { if (g.Tag.ToString() == appName) { appTileButton = g; break; } }
                WrapPanel appTileHover = null;
                foreach (WrapPanel wp in wrapPanels) { if (wp.Tag.ToString() == appName) { appTileHover = wp; break; } }
                Label installedText = null;
                foreach (Label l in statusTexts) { if (l.Tag.ToString() == appName.ToLower().Replace(" ", "")) { installedText = l; break; } }

                foreach (ProgressBar pgb in progressBars)
                {
                    if ("pg" + appName == pgb.Tag.ToString())
                    {
                        string aNameSub = string.Empty;
                        string[] sName = appName.Split(' ');
                        foreach (string s in sName) { aNameSub = aNameSub + s; }

                        /*appTileButton.MouseDown += noFunctionBTN;
                        appTileHover.MouseEnter += noFunctionHover;
                        appTileHover.MouseLeave += noFunctionHover;*/
                        appTileButton.MouseDown -= appPage;
                        appTileHover.MouseEnter -= appTile_MouseEnter;
                        appTileHover.MouseLeave -= appTile_MouseLeave;

                        pgb.Visibility = Visibility.Visible;
                        string localStatus = string.Empty;
                        if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll")) { localStatus = "Updating"; }
                        else { localStatus = "Downloading"; }
                        installedText.Content = localStatus;

                        //TMP Close page while downloading, make window stay open in future downloads
                        mw.appDetailsFrame.Content = null;
                        mw.backBTN.Visibility = Visibility.Collapsed;
                        mw.appDetailsFrame.Visibility = Visibility.Collapsed;
                        mw.appsFrame.Visibility = Visibility.Visible;

                        if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}")) { Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}"); }

                        try { File.Delete(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\a{version}.zip"); } catch { }
                        try { File.Delete(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll"); } catch { }

                        string uriSub = string.Empty;
                        string[] uri = appName.ToLower().Split(' ');
                        foreach (string s in uri) { uriSub = uriSub + s; }

                        httpGit.webClient.DownloadFileAsync(new Uri($"https://github.com/kOFReadie/{aNameSub}/releases/download/{version}/{aNameSub}.dll"), AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll");
                        httpGit.webClient.DownloadProgressChanged += client_DownloadProgressChanged;
                        httpGit.webClient.DownloadFileCompleted += downloadCompleted;

                        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
                        {
                            double bytesIn = double.Parse(e.BytesReceived.ToString());
                            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                            double percentage = bytesIn / totalBytes * 100;
                            int percentageComplete = int.Parse(Math.Truncate(percentage).ToString());
                            pgb.Value = percentageComplete;
                            installedText.Content = $"{localStatus} ({percentageComplete}%)";
                        }

                        void downloadCompleted(object sender, AsyncCompletedEventArgs e)
                        {
                            appTileButton.MouseDown += appPage;
                            appTileHover.MouseEnter += appTile_MouseEnter;
                            appTileHover.MouseLeave += appTile_MouseLeave;
                            installedText.Content = "Installed";
                            pgb.Visibility = Visibility.Hidden;
                        }

                        //void noFunctionBTN(object sender, MouseButtonEventArgs e) { }
                        //void noFunctionHover(object sender, MouseEventArgs e) { }

                        break;
                    }
                }
            }
            catch (Exception ex) { LogWriter.CreateLog(ex); msgBox.Show("Failed to download App. Error written to log file.", "App Download Failed"); }
        }

        internal class appData
        {
            public bool show { get; set; }
            public string appName { get; set; }
            public string description { get; set; }
            public string icon { get; set; }
            public string version { get; set; }
        }
    }
}
