using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using Microsoft.Win32;
using System.IO.Compression;
using System.IO;

namespace kOFR_Repo
{
    /// <summary>
    /// Interaction logic for FileDownloader.xaml
    /// </summary>
    public partial class FileDownloader : Window
    {
        string serverPath;
        string fileName;
        string savePath;
        List<string> filesToDelete;
        string bodyText;

        public FileDownloader(string fileUrl, string fileToGet, string downloadPath, List<string> ftd, string bodyT)
        {
            serverPath = fileUrl;
            fileName = fileToGet;
            savePath = downloadPath;
            filesToDelete = ftd;
            bodyText = bodyT;

            InitializeComponent();
            windowBorder.Visibility = Visibility.Visible;
            setStyles();

            body.Text = bodyText;

            downloadFile();
        }

        private void setStyles()
        {
            string accentColour = SystemParameters.WindowGlassBrush.ToString();
            string AppsUseLightTheme = "#FFFFFFFF";
            string textColour = "#FF000000";
            string buttonColour = "#FFDDDDDD";
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
            titleBar.Background = b(accentColour);
            applicationTitle.Foreground = b(textColour);
            closebtn.Foreground = b(textColour);
            closebtn.Background = b(accentColour);
            body.Foreground = b(textColour);
            logs.Foreground = b(textColour);
            downloadprogress.Background = b(buttonColour);
            downloadprogress.Foreground = b(accentColour);
            windowBorder.BorderBrush = b(accentColour);
        }

        private void downloadFile()
        {
            try { Directory.CreateDirectory(savePath); } catch { }
            foreach (string s in filesToDelete) { try { File.Delete(savePath + s); } catch { } }

            logs.Text = "Downloading";
            WebClient webclient = new WebClient();
            webclient.Headers.Add("user-agent", "kOFReadie-RepoApp");
            webclient.DownloadFileAsync(new Uri(serverPath + fileName), savePath + fileName);
            webclient.DownloadProgressChanged += Webclient_DownloadProgressChanged;
            webclient.DownloadFileCompleted += Webclient_DownloadFileCompleted;
        }

        private void Webclient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //logs.Text = "Extracting...";
            //string[] a = fileName.Split('.');
            //ZipFile.ExtractToDirectory(savePath + fileName, savePath);
            Close();
        }

        private void Webclient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            int downloadPercentage = int.Parse(Math.Truncate(percentage).ToString());
            downloadprogress.Value = downloadPercentage;
            logs.Text = $"Downloading ({downloadPercentage}%)";
        }

        #region Window settings
        private void titleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void closebtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
    }
}
