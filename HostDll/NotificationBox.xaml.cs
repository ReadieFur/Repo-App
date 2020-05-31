using System;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace kOFRRepo
{
    /// <summary>
    /// Interaction logic for NotificationBox.xaml
    /// </summary>

    public class notifBox
    {
        public static void show(string text, string title, int time)
        {
            new NotificationBox(text, title, time).Show();
        }
    }

    public partial class NotificationBox : Window
    {
        Timer timeTillClose = new Timer();
        int timeTick = 0;
        int timeOpen = 0;

        public NotificationBox(string text, string title, int time)
        {
            InitializeComponent();
            windowBorder.Visibility = Visibility.Visible;
            background.Background = Styles.theme();
            nTitle.Foreground = Styles.text();
            nBody.Foreground = Styles.text();
            backBTN.Foreground = Styles.text();
            windowBorder.BorderBrush = Styles.accent();
            Left = SystemParameters.WorkArea.Width - Width - 10;
            Top = SystemParameters.WorkArea.Height - Height - 10;
            nTitle.Content = title;
            nBody.Text = text;
            timeOpen = time;
            timeTillClose.Interval = 1000;
            timeTillClose.Tick += TimeTillClose_Tick;
            timeTillClose.Start();
            Visibility = Visibility.Visible;
            Activate();
        }

        private void TimeTillClose_Tick(object sender, EventArgs e)
        {
            if (timeTick > timeOpen) { timeTillClose.Stop(); Close(); }
            else { timeTick += 1; }
        }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void backBTN_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
