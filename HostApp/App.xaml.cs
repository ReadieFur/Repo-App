using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;

namespace kOFR_Repo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void App_Startup(object sender, StartupEventArgs e)
        {
            string[] startupArgs = e.Args;
            string args = string.Empty;
            foreach (var arg in startupArgs) { args = args + arg; }
            args = args.ToLower();

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            string thisDirectory = AppDomain.CurrentDomain.BaseDirectory;

            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "kOFReadie-RepoApp");

            string kOFRRepoVersion = string.Empty;
            string hostVersion = string.Empty;

            if (!File.Exists(thisDirectory + "\\kOFRRepo.dll"))
            {
                new Installer().ShowDialog();
            }
            else
            {
                if (!args.Contains("noupdate"))
                {
                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not contact server for updates.");
                        LogWriter.CreateLog(ex);
                    }

                    if (kOFRRepoVersion != string.Empty && FileVersionInfo.GetVersionInfo(thisDirectory + "\\kOFRRepo.dll").FileVersion
                        != kOFRRepoVersion.Substring(4) + ".0")
                    {
                        downloadFile($"https://github.com/kOFReadie/Repo-App/releases/download/{kOFRRepoVersion}/", $"kOFRRepo.dll", $"{thisDirectory}",
                            new List<string>() { "kOFRRepo.dll" }, "Downloading kOFRRepo.dll");
                    }
                }
            }

            loadMainDLL(args);

            Environment.Exit(0);
        }

        private void loadMainDLL(string e)
        {
            AppDomain kOFRRepoDLL = AppDomain.CreateDomain("kOFRRepoDLL");
            Assembly kOFRRepodll = kOFRRepoDLL.Load("kOFRRepo");
            Type[] DLL = kOFRRepodll.GetTypes();
            int uClassToLoad = 0;
            foreach (var type in DLL)
            {
                if (type.ToString().Contains("Startup")) { break; }
                else { uClassToLoad += 1; }
            }
            dynamic fromUpdaterDLL = Activator.CreateInstance(DLL[uClassToLoad]);
            fromUpdaterDLL.tasks(e);
            AppDomain.Unload(kOFRRepoDLL);
        }

        private void downloadFile(string fileUrl, string fileName, string downloadPath, List<string> filesToDelete, string body)
        {
            new FileDownloader(fileUrl, fileName, downloadPath, filesToDelete, body).ShowDialog();
        }        
    }
}
