using System;
using System.IO;

namespace kOFR_Repo
{
    class LogWriter
    {
        public static void CreateLog(Exception logToWrite)
        {
            try
            {
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Logs.txt"))
                {
                    using (StreamWriter sw = File.CreateText(AppDomain.CurrentDomain.BaseDirectory + "Logs.txt"))
                    {
                        sw.Write("Logs for kOFRRepo.exe, kOFRRepo.dll and Host Updater.exe.\n" +
                            "Events are sorted from oldest to newest.\n" +
                            "===========================================================");
                    }
                }

                using (StreamWriter sw = File.AppendText(AppDomain.CurrentDomain.BaseDirectory + "\\Logs.txt"))
                {
                    sw.WriteLine($"\n\nLog Entry: {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
                    //sw.WriteLine($"Error Type: {}");
                    sw.WriteLine($"Log Details: {logToWrite}");
                    sw.Write("-------------------------------");
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Failed to create log.", "Log Error");
                Environment.Exit(0);
            }
        }
    }
}
