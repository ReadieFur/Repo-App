using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace kOFRRepo
{
    public class Startup
    {
        public void tasks(string args)
        {
            if (!args.Contains("overrideinstances"))
            {
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
                {
                    msgBox.Show("An instance of kOFRRepoApp.exe is already running!", "App Already Running");
                    Environment.Exit(0);
                }
            }

            httpGit.setupClients();
            Styles.getStyles();
            new AppLibaryWindow(args).ShowDialog();
        }
    }
}
