﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.ComponentModel;
using SharpSvn;
using System.Windows.Media.Effects;

namespace FMM2
{
    public partial class MainWindow : Window
    {
        private void infobarDLDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!workerDownloadMods.IsBusy)
            {
                workerDownloadMods.RunWorkerAsync();
            }
        }

        string modsdownloaded = "Mods downloaded.";
        string moddlsuccess = "Mod downloaded successfully.";
        string moddlfailed = "Mod failed to download.";
        string downloadingmods = "Downloading mods";

        private void dlModWorker(object sender, DoWorkEventArgs e)
        {
            List<Mod> checkedMods = new List<Mod>();

            foreach (Mod listedMod in downloadableModsList.Items)
            {
                if (listedMod.IsChecked == true)
                {
                    checkedMods.Add(listedMod);
                }
            }
            if (checkedMods.Count == 0)
            {
                return;
            }

            Dispatcher.Invoke(new Action(() =>
            {
                modsTabs.IsEnabled = false;
                menu.IsEnabled = false;
                everythingGrid.Effect = new BlurEffect { Radius = 10 };
                installLogGrid.Visibility = Visibility.Visible;
                closeLogButton.Visibility = Visibility.Collapsed;
                closeLogButton.Focus();
                installLogBox.Text = "";
                installLogBox.Text += "-- " + downloadingmods.ToUpper() + " --" + Environment.NewLine + Environment.NewLine;
            }));

            int i = 0;

            // Create Octokit client to use
            try
            {
                var octo = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("FMMv2"));
            }
            catch ( Exception ex )
            {
                MessageBox.Show(ex.ToString());
            }

            foreach (Mod checkedMod in checkedMods)
            {
                i++;
                Dispatcher.Invoke(new Action(() =>
                {
                    installLogBox.Text += downloadingmods + " (" + i + "/" + checkedMods.Count + ") : " + checkedMod.Name + " " + checkedMod.Version + Environment.NewLine;
                }));

                SvnClient svnClient = new SvnClient();
                svnClient.Progress += new EventHandler<SvnProgressEventArgs>(svnProgress);
                string remoteLocation = repository + Path.GetDirectoryName(checkedMod.Location);
                string localLocation = Path.GetDirectoryName(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "mods", "tagmods", checkedMod.Location.Replace("/", "\\")));
                
                try
                {
                    deleteDirectory(localLocation);
                } catch
                {
                    // mod doesn't already exist - all fine
                }

                if (!Directory.Exists(localLocation))
                {
                    Directory.CreateDirectory(localLocation);
                }

                if (Directory.Exists(Path.Combine(localLocation, ".svn")))
                {
                    svnClient.CleanUp(localLocation);
                }

                try
                {
                    svnClient.CheckOut(new Uri(remoteLocation), localLocation);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        installLogBox.Text += "| " + moddlsuccess + Environment.NewLine;
                    }));
                }
                catch
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        installLogBox.Text += "| " + moddlfailed + Environment.NewLine;
                    }));
                }
            }

            Dispatcher.Invoke(new Action(() =>
            {
                MessageBox.Show(System.Windows.Application.Current.MainWindow, modsdownloaded, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                closeLogButton.Visibility = Visibility.Visible;

                foreach (Mod listedMod in downloadableModsList.Items)
                {
                    if (listedMod.IsChecked == true)
                    {
                        listedMod.IsChecked = false;
                    }
                }

                mMods.Clear();
                infobarScroll.Visibility = Visibility.Collapsed;
                workerPopulateMyMods.RunWorkerAsync();
            }));
        }

        private void svnProgress(object sender, SvnProgressEventArgs e)
        {
            
        }

        private void deleteDirectory(string path)
        {
            var directory = new DirectoryInfo(path)
            { Attributes = FileAttributes.Normal };
            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }
            directory.Delete(true);
        }
    }
}
