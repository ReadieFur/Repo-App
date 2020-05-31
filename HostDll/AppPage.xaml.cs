using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Threading;

namespace kOFRRepo
{
    public partial class AppPage : Page
    {
        string aAppName;
        string aNameSub = string.Empty;
        string aDesc;
        string aImage;
        string aVersion;
        bool mainOption = true;

        public AppPage(string appName, string description, string icon, string version)
        {
            aAppName = appName;
            aDesc = description;
            aImage = icon;
            aVersion = version;
            string[] sName = appName.Split(' ');
            foreach (string s in sName) { aNameSub = aNameSub + s; }
            aNameSub = aNameSub.ToLower();

            InitializeComponent();
            //moreOptionsBTN.Visibility = Visibility.Collapsed;
            extras.Visibility = Visibility.Collapsed;
            setStyles();
        }

        private void setStyles()
        {
            background.Background = Styles.theme();
            aTitle.Foreground = Styles.text();
            aDescription.Foreground = Styles.text();
            appFunctionTXT.Foreground = Styles.text();
            appFunction.Background = Styles.accent();
            //moreOptionsTXT.Foreground = Styles.text();
        }

        private void moreOptionsBTN_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Show/expand extra options (launch options/uninstall)
        }

        private void appFunction_Click(object sender, MouseButtonEventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll"))
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    Label installedText = null;
                    foreach (Label l in AppLibaryPage.statusTexts) { if (l.Tag.ToString() == aNameSub) { installedText = l; break; } }

                    if (mainOption)
                    {
                        if (!AppLibaryPage.runningPrograms.Contains(aNameSub))
                        {
                            try
                            {
                                var mw = Application.Current.MainWindow as AppLibaryWindow;
                                string args = string.Empty;
                                AppLibaryPage.runningPrograms.Add(aNameSub);
                                appFunctionTXT.Content = "Running";
                                installedText.Content = "Running";
                                appFunction.Background = Styles.button();
                                mw.hideWindow();
                                //appFunction.Click += (s, ea) => { };
                                appFunction.MouseDown -= appFunction_Click;
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        AppDomain appDLL = AppDomain.CreateDomain($"{aNameSub}DLL");
                                        Assembly appdll = appDLL.Load(aNameSub);
                                        Type[] DLL = appdll.GetTypes();
                                        int classToLoad = 0;
                                        foreach (var type in DLL)
                                        {
                                            if (type.ToString().Contains("Startup")) { break; }
                                            else { classToLoad += 1; }
                                        }
                                        dynamic startup = Activator.CreateInstance(DLL[classToLoad]);
                                        //fromUpdaterDLL.tasks(args);

                                        Thread t = new Thread(() =>
                                        {
                                            startup.tasks(args);
                                            AppDomain.Unload(appDLL);
                                            Dispatcher.Invoke(() =>
                                            {
                                                AppLibaryPage.runningPrograms.Remove(aNameSub);
                                                appFunctionTXT.Content = "Launch";
                                                installedText.Content = "Installed";
                                                appFunction.Background = Styles.accent();
                                                appFunction.MouseDown += appFunction_Click;
                                                mw.showWindow();
                                            });
                                        });
                                        t.SetApartmentState(ApartmentState.STA);
                                        t.Start();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogWriter.CreateLog(ex);
                                        msgBox.Show("An error occured while hosting the DLL.", $"Error Hosting {aAppName}.");
                                        try { AppLibaryPage.runningPrograms.Remove(aNameSub); } catch { }
                                    }
                                });
                            }
                            catch (Exception ex) { LogWriter.CreateLog(ex); msgBox.Show($"Error Launching {aAppName}", "Error Launching App"); }
                        }
                        else { appFunction.MouseDown -= appFunction_Click; }
                    }
                    else
                    {
                        Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}", true);
                        appFunction.Background = Styles.accent();
                        appFunctionTXT.Content = "Install";
                        installedText.Content = "Install";
                    }
                }
                if (e.ChangedButton == MouseButton.Right)
                {
                    if (mainOption)
                    {
                        appFunction.Background = Styles.b("#FFFF0000");
                        appFunctionTXT.Content = "Uninstall";
                        mainOption = false;
                    }
                    else
                    {
                        appFunction.Background = Styles.accent();
                        appFunctionTXT.Content = "Launch";
                        mainOption = true;
                    }
                }
            }
            else
            {
                var mw = Application.Current.MainWindow as AppLibaryWindow;
                mw.downloadFile(aAppName, aVersion);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BitmapImage appIconImage = new BitmapImage();
            appIconImage.BeginInit();
            appIconImage.StreamSource = new MemoryStream(Convert.FromBase64String(aImage));
            appIconImage.EndInit();

            aIcon.Source = appIconImage;
            aTitle.Content = aAppName;
            aDescription.Text = aDesc;

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"\\{aNameSub}\\{aNameSub}.dll")) { appFunctionTXT.Content = "Launch"; }
            else { appFunctionTXT.Content = "Install"; }

            if (AppLibaryPage.runningPrograms.Contains(aNameSub))
            {
                appFunctionTXT.Content = "Running";
                appFunction.Background = Styles.button();
                appFunction.MouseDown -= appFunction_Click;
                System.Timers.Timer waitForExit = new System.Timers.Timer();
                waitForExit.Elapsed += WaitForExit_Elapsed;
                waitForExit.Interval = 100;
                waitForExit.Start();

                void WaitForExit_Elapsed(object s, System.Timers.ElapsedEventArgs ea)
                {
                    if (!AppLibaryPage.runningPrograms.Contains(aNameSub))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            appFunctionTXT.Content = "Launch";
                            appFunction.Background = Styles.accent();
                            appFunction.MouseDown += appFunction_Click;
                        });
                    }
                }
            }
        }
    }
}
