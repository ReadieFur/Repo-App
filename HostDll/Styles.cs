using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;

namespace kOFRRepo
{
    public static class Styles
    {
        public static LinearGradientBrush gBWHorizontal = new LinearGradientBrush();
        public static LinearGradientBrush gBWVertical = new LinearGradientBrush();
        public static LinearGradientBrush gVertical = new LinearGradientBrush();
        public static LinearGradientBrush gHorizontal = new LinearGradientBrush();
        public static string AppsUseLightTheme = "#FFFFFFFF";
        static string textColour = "#FF000000";
        static string buttonColour = "#FFDDDDDD";
        public static string accentColour = "#FF0078D7";
        public static Brush theme() { return b(AppsUseLightTheme); }
        public static Brush text() { return b(textColour); }
        public static Brush button() { return b(buttonColour); }
        public static Brush accent() { return b(accentColour); }

        public static Brush b(string col)
        {
            var bc = new BrushConverter();
            return (Brush)bc.ConvertFrom(col);
        }

        public static void getStyles()
        {
            try
            {
                try
                {
                    accentColour = SystemParameters.WindowGlassBrush.ToString();
                    if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").GetValue("AppsUseLightTheme").ToString() == "0") //Dark theme
                    {
                        AppsUseLightTheme = "#FF101011";
                        textColour = "#FFFFFF";
                        buttonColour = "#FF383838";
                    }
                }
                catch { }

                gradientBWHorizontal();
                gradientBWVertical();
                gradientHorizontal();
                gradientVertical();
            }
            catch (Exception ex)
            {
                LogWriter.CreateLog(ex);
                MessageBox.Show("Failed to get system styles.", "Peronalisation Error");
            }
        }

        public static bool checkForChange()
        {
            try
            {
                if (SystemParameters.WindowGlassBrush.ToString() != accentColour) { return true; }

                string tmpTheme = "#FFFFFFFF";
                if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").GetValue("AppsUseLightTheme").ToString() == "0") { tmpTheme = "#FF101011"; }
                if (tmpTheme != AppsUseLightTheme) { return true; }

                return false;
            }
            catch (Exception ex) { LogWriter.CreateLog(ex); }
            return false;
        }

        private static void gradientBWHorizontal()
        {
            gBWHorizontal = new LinearGradientBrush();
            gBWHorizontal.StartPoint = new Point(0, 0.5);
            gBWHorizontal.EndPoint = new Point(1, 0.5);
            gBWHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(AppsUseLightTheme), Offset = 0 });
            gBWHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(textColour), Offset = 0.3 });
            gBWHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(textColour), Offset = 0.7 });
            gBWHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(AppsUseLightTheme), Offset = 1 });
        }

        private static void gradientBWVertical()
        {
            gBWVertical = new LinearGradientBrush();
            gBWVertical.StartPoint = new Point(0.5, 0);
            gBWVertical.EndPoint = new Point(0.5, 1);
            gBWVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(AppsUseLightTheme), Offset = 0 });
            gBWVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(textColour), Offset = 0.3 });
            gBWVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(textColour), Offset = 0.7 });
            gBWVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(AppsUseLightTheme), Offset = 1 });
        }

        private static void gradientVertical()
        {
            gVertical = new LinearGradientBrush();
            gVertical.StartPoint = new Point(0.5, 0);
            gVertical.EndPoint = new Point(0.5, 1);
            gVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(Styles.accentColour), Offset = 0.1 });
            gVertical.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(Styles.AppsUseLightTheme), Offset = 0.9 });
        }

        private static void gradientHorizontal()
        {
            gHorizontal = new LinearGradientBrush();
            gHorizontal.StartPoint = new Point(0, 0.5);
            gHorizontal.EndPoint = new Point(1, 0.5);
            gHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(Styles.accentColour), Offset = 0.1 });
            gHorizontal.GradientStops.Add(new GradientStop() { Color = (Color)ColorConverter.ConvertFromString(Styles.AppsUseLightTheme), Offset = 0.9 });
        }
    }
}
