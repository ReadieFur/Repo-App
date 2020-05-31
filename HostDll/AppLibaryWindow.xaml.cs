using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net;
using System.IO;
using System.Timers;
using System.Drawing;
using System.Windows.Interop;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using winThread = System.Threading;
using System.Collections.Generic;

namespace kOFRRepo
{
    public partial class AppLibaryWindow : Window
    {
        public static string updateInfoJSON;
        public AppLibaryPage alp = new AppLibaryPage();
        public static List<string> startupArgs;
        bool allowClose = false;
        Timer winAero = new Timer();
        Timer themeChange = new Timer();
        public static System.Windows.Forms.NotifyIcon notifyIcon = null;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (allowClose == false)
            {
                e.Cancel = true;
            }
            else
            {
                notifyIcon.Icon = null;
                notifyIcon.Text = null;
                notifyIcon.Visible = false;
                e.Cancel = false;
            }
        }

        public AppLibaryWindow(string e)
        {
            e = e.Replace(" ", "");
            startupArgs = e.Split('-').ToList();
            InitializeComponent();

            MinWidth = 1025;
            MinHeight = 450;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            windowBorder.Visibility = Visibility.Visible;
            backBTN.Visibility = Visibility.Collapsed;
            updaterRow.Height = new GridLength(0, GridUnitType.Pixel);

            setStyles();

            try
            {
                updateInfoJSON = httpGit.webClient.DownloadString("https://raw.githubusercontent.com/kOFReadie/Repo-App/master/notices.json");
            }
            catch (Exception ex) { LogWriter.CreateLog(ex); }

            mainFrame.Content = new Home();
        }

        private void background_Loaded(object sender, RoutedEventArgs e)
        {
            appsFrame.Visibility = Visibility.Visible;
            appsFrame.Content = alp;
            appsFrame.Visibility = Visibility.Hidden;

            winAero.Interval = 10;
            winAero.Elapsed += checkForAeroFC;
            winAero.Start();

            themeChange.Interval = 500;
            themeChange.Elapsed += (se, ev) =>
            {
                if (Styles.checkForChange())
                {
                    Dispatcher.Invoke(() =>
                    {
                        Styles.getStyles();
                        setStyles();
                        appsFrame.Content = new AppLibaryPage();
                        if (appsFrame.Visibility == Visibility.Visible) { libraryGrid_MouseDown(null, null); }
                    });
                }
            };
            themeChange.Start();

            setupNotifyIcon();

            if (startupArgs.Contains("hide")) { hideWindow(); } else { Activate(); }

            foreach (string appToRun in startupArgs) { if (appToRun.Contains("runapp")) { runApp(appToRun); } }
        }

        private void runApp(string appName)
        {
            winThread.Tasks.Task.Run(() =>
            {
                while (alp.completedStartupTasks == false) { /*Wait*/ } //Need to find a way to wait for updates to complete

                appName = appName.Replace("runapp", "");
                appName = appName.Replace(" ", "");
                string aNameSub = appName;

                //System.Windows.Controls.Label installedText = null;
                //foreach (System.Windows.Controls.Label l in AppLibaryPage.statusTexts) { if (l.Tag.ToString() == aNameSub) { installedText = l; break; } }
                //Dll doesn't launch here for some reason.

                if (!AppLibaryPage.runningPrograms.Contains(aNameSub))
                {
                    try
                    {
                        string args = string.Empty;
                        AppLibaryPage.runningPrograms.Add(aNameSub);
                        winThread.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                AppDomain appDLL = AppDomain.CreateDomain($"{aNameSub}DLL");
                                Assembly appdll = appDLL.Load(aNameSub);
                                //installedText.Content = "Running";
                                Type[] DLL = appdll.GetTypes();
                                int classToLoad = 0;
                                foreach (var type in DLL)
                                {
                                    if (type.ToString().Contains("Startup")) { break; }
                                    else { classToLoad += 1; }
                                }
                                dynamic startup = Activator.CreateInstance(DLL[classToLoad]);
                                //fromUpdaterDLL.tasks(args);

                                winThread.Thread t = new winThread.Thread(() =>
                                {
                                    startup.tasks(args);
                                    AppDomain.Unload(appDLL);
                                    Dispatcher.Invoke(() =>
                                    {
                                        AppLibaryPage.runningPrograms.Remove(aNameSub);
                                        //installedText.Content = "Installed";
                                        showWindow();
                                    });
                                });
                                t.SetApartmentState(winThread.ApartmentState.STA);
                                t.Start();
                            }
                            catch (Exception ex)
                            {
                                LogWriter.CreateLog(ex);
                                msgBox.Show("An error occured while hosting the DLL.", $"Error Hosting {aNameSub}.");
                                try { AppLibaryPage.runningPrograms.Remove(aNameSub); } catch { }
                            }
                        });
                    }
                    catch (Exception ex) { LogWriter.CreateLog(ex); msgBox.Show($"Error Launching {aNameSub}", "Error Launching App"); }
                }
            });
        }

        private void setStyles()
        {
            background.Background = Styles.theme();
            minimisebtn.Background = Styles.theme();
            minimisebtn.Foreground = Styles.text();
            resizebtn.Background = Styles.theme();
            resizebtn.Foreground = Styles.text();
            closebtn.Background = Styles.theme();
            closebtn.Foreground = Styles.text();
            windowBorder.BorderBrush = Styles.accent();
            verticalLine.Stroke = Styles.gBWVertical;
            backBTN.Foreground = Styles.text();
            watermark.Foreground = Styles.text();
            appTitle.Foreground = Styles.text();

            #region Navigation buttons
            string homeBTN;
            string libarayBTN;
            if (Styles.AppsUseLightTheme == "#FFFFFFFF")
            {
                homeBTN = "HomeBlack";
                libarayBTN = "LibarayBlack";
            }
            else
            {
                homeBTN = "HomeWhite";
                libarayBTN = "LibarayWhite";
            }

            //Home button
            homeImage.Source = new BitmapImage(new Uri($"Resources/{homeBTN}.png", UriKind.Relative));
            homeTXT.Foreground = Styles.text();
            homeBorder.BorderBrush = Styles.accent();
            homeGrid.Background = Styles.button();

            //Processes button
            libraryImage.Source = new BitmapImage(new Uri($"Resources/{libarayBTN}.png", UriKind.Relative));
            libraryTXT.Foreground = Styles.text();
            libraryBorder.BorderBrush = Styles.accent();
            libraryGrid.Background = Styles.theme();
            #endregion
        }

        private void topBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Width == SystemParameters.WorkArea.Width && Height == SystemParameters.WorkArea.Height)
                {
                    windowBorder.Visibility = Visibility.Visible;
                    Top = System.Windows.Forms.Control.MousePosition.Y - 15;
                    Left = System.Windows.Forms.Control.MousePosition.X - 400;
                    Width = 800;
                    Height = 450;
                    resizebtn.Content = "\uE922";
                    DragMove();
                }
                else if (e.ClickCount == 2)
                {
                    Top = 0;
                    Left = 0;
                    Width = SystemParameters.WorkArea.Width;
                    Height = SystemParameters.WorkArea.Height;
                    resizebtn.Content = "\uE923";
                    windowBorder.Visibility = Visibility.Hidden;
                }
                else
                {
                    DragMove();
                }
            }
        }

        public void downloadFile(string appName, string version) { alp.downloadFile(appName, version); }

        private void checkForAeroFC(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    Top = 0;
                    Left = 0;
                    Width = SystemParameters.WorkArea.Width;
                    Height = SystemParameters.WorkArea.Height;
                    resizebtn.Content = "\uE923";
                    windowBorder.Visibility = Visibility.Hidden;
                }
                else if (Width != SystemParameters.WorkArea.Width && Height != SystemParameters.WorkArea.Height)
                {
                    resizebtn.Content = "\uE922";
                    windowBorder.Visibility = Visibility.Visible;
                }

                if (Height > SystemParameters.WorkArea.Height)
                {
                    Height = SystemParameters.WorkArea.Height;
                }

                //Extra, check for system theme change | To implement
                /*if (Styles.accentColour != SystemParameters.WindowGlassBrush.ToString())
                {
                    Styles.getStyles();
                    setStyles();
                    alp.
                }*/
            });
        }

        #region Window buttons
        private void closebtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppLibaryPage.runningPrograms.Count >= 1)
            {
                string closeResult = msgBox.Show("Other apps are currently running, closing this app will close all other apps.\nAre you sure you want to close this app?", "Exit App?", options.b.yesNo);
                if (closeResult == "yes")
                {
                    winAero.Stop();
                    allowClose = true;
                    Close();
                }
            }
            else
            {
                winAero.Stop();
                allowClose = true;
                Close();
            }
        }

        private void resizebtn_Click(object sender, RoutedEventArgs e)
        {
            if (Height != SystemParameters.WorkArea.Height && Width != SystemParameters.WorkArea.Width)
            {
                Top = 0;
                Left = 0;
                Height = SystemParameters.WorkArea.Height;
                Width = SystemParameters.WorkArea.Width;
                windowBorder.Visibility = Visibility.Hidden;
                resizebtn.Content = "\uE923";
            }
            else
            {
                WindowState = WindowState.Normal;
                Height = 450;
                Width = 800;
                Top = SystemParameters.WorkArea.Height / 4;
                Left = SystemParameters.WorkArea.Width / 4;
                windowBorder.Visibility = Visibility.Visible;
                resizebtn.Content = "\uE922";
            }
        }

        private void minimisebtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        #region NotifyIcon
        private void setupNotifyIcon()
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/kOFRRepo;component/Resources/Icon.ico")).Stream;
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Click += new EventHandler(notifyIcon_Click);
            notifyIcon.Icon = new Icon(iconStream);
            notifyIcon.Text = "Show kOFR Repo";
            notifyIcon.Visible = true;
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            if (ShowInTaskbar == false) { showWindow(); }
            else { hideWindow(); }
        }

        public void showWindow()
        {
            if (ShowInTaskbar == false)
            {
                ShowInTaskbar = true;
                WindowState = WindowState.Normal;
                changeWindowState();
                Activate();
                notifyIcon.Text = "Hide kOFR Repo";
            }
        }

        public void hideWindow()
        {
            if (ShowInTaskbar == true)
            {
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
                changeWindowState();
                notifBox.show("kOFR Repo has been minimised to the system tray", "Minimised to tray", 2);
                notifyIcon.Text = "Show kOFR Repo";
            }
        }

        void changeWindowState()
        {
            try
            {
                WindowInteropHelper wndHelper = new WindowInteropHelper(this);
                int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
                exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            }
            catch (Exception ex) { LogWriter.CreateLog(ex); Environment.Exit(0); }
        }
        #endregion

        #region Navigation buttons
        private void homeGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            homeBorder.Visibility = Visibility.Visible;
            appDetailsFrame.Visibility = Visibility.Visible;
            homeGrid.Background = Styles.button();
            mainFrame.Visibility = Visibility.Visible;
            mainFrame.Content = new Home();
            backBTN.Visibility = Visibility.Hidden;

            hideLibray();
        }

        private void libraryGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            libraryBorder.Visibility = Visibility.Visible;
            libraryGrid.Background = Styles.button();
            mainFrame.Content = null;
            appsFrame.Visibility = Visibility.Visible;

            mainFrame.Visibility = Visibility.Hidden;
            homeGrid.Background = Styles.theme();
            homeBorder.Visibility = Visibility.Hidden;
        }

        private void hideLibray()
        {
            appsFrame.Visibility = Visibility.Collapsed;
            libraryGrid.Background = Styles.theme();
            libraryBorder.Visibility = Visibility.Hidden;
            appDetailsFrame.Visibility = Visibility.Collapsed;
            appDetailsFrame.Content = null;
        }
        #endregion

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            WS_EX_TOOLWINDOW = 0x00000080
        }

        public enum GetWindowLongFields
        {
            GWL_EXSTYLE = (-20)
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion
    }
}
