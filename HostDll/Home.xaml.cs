using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kOFRRepo
{
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
            try
            {
                //background.Background = Styles.theme();
                appName.Foreground = Styles.text();
                appVersion.Foreground = Styles.text();
                appNameLine.Stroke = Styles.gBWHorizontal;
                string[] appVersionSS = FileVersionInfo.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory + "kOFRRepo.dll").FileVersion.Split('.');
                appVersion.Text = $"v{appVersionSS[0]}.{appVersionSS[1]}.{appVersionSS[2]}";

                List<notices> noticesInfo = JsonConvert.DeserializeObject<List<notices>>(AppLibaryWindow.updateInfoJSON);
                foreach (var noticeData in noticesInfo)
                {
                    if (noticeData.show == true || AppLibaryWindow.startupArgs.Contains("showall"))
                    {
                        Grid grid = new Grid();
                        grid.Margin = new Thickness(15, 15, 0, 0);
                        grid.Width = 175;
                        grid.Height = 210;

                        Rectangle rectangle = new Rectangle();
                        rectangle.Fill = Styles.button();
                        rectangle.RadiusX = 10;
                        rectangle.RadiusY = 10;

                        WrapPanel wrappanel = new WrapPanel();
                        wrappanel.Width = 175;
                        wrappanel.Orientation = Orientation.Vertical;
                        wrappanel.HorizontalAlignment = HorizontalAlignment.Left;
                        wrappanel.VerticalAlignment = VerticalAlignment.Top;

                        TextBlock Title = new TextBlock();
                        Title.Margin = new Thickness(10, 10, 0, 0);
                        Title.Width = 155;
                        Title.TextWrapping = TextWrapping.Wrap;
                        Title.Text = noticeData.title;
                        Title.FontFamily = new FontFamily("Century Gothic");
                        Title.FontSize = 18;
                        Title.Foreground = Styles.text();

                        TextBlock body = new TextBlock();
                        body.Margin = new Thickness(10, 5, 0, 0);
                        body.Width = 155;
                        body.Height = double.NaN;
                        body.TextWrapping = TextWrapping.Wrap;
                        body.Text = noticeData.body;
                        body.FontFamily = new FontFamily("Century Gothic");
                        body.FontSize = 12;
                        body.Foreground = Styles.text();

                        wrappanel.Children.Add(Title);
                        wrappanel.Children.Add(body);
                        grid.Children.Add(rectangle);
                        grid.Children.Add(wrappanel);
                        updateWrapPanel.Children.Insert(0, grid);
                    }
                }
            }
            catch (Exception ex) { LogWriter.CreateLog(ex); msgBox.Show("Failed to get alerts from server."); }
        }

        class notices
        {
            public bool show { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }
    }
}
