using EasyJob.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for TroubleshootingWindow.xaml
    /// </summary>
    public partial class TroubleshootingWindow : Window
    {
        Config config = null;

        public TroubleshootingWindow(Config _config)
        {
            InitializeComponent();
            config = _config;
            FillKnownData();
        }

        private void FillKnownData()
        {
            OSDescription.Text = RuntimeInformation.OSDescription;
            FrameworkDescription.Text = RuntimeInformation.FrameworkDescription;
            OsD.Text = RuntimeInformation.OSArchitecture.ToString();

            PowerShellPath.Text = config.default_powershell_path;

            if(config.powershell_arguments == "")
            {
                PowerShellArguments.Text = "empty";
                PowerShellArguments.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            }
            else 
            { 
                PowerShellArguments.Text = config.powershell_arguments;
            }

            if(File.Exists(config.default_powershell_path))
            {
                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(config.default_powershell_path);
                    PowerShellVersion.Text = versionInfo.FileVersion;
                }
                catch
                {
                    PowerShellVersion.Text = "unable to get version";
                    PowerShellVersion.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                }
            }

            if(Directory.Exists(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Program Files\WindowsPowerShell\Modules"))
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Program Files\WindowsPowerShell\Modules", "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in dirs)
                    {
                        PowerShellModules.Text = PowerShellModules.Text + dir + Environment.NewLine;
                    }
                }
                catch { }
            }
            if (Directory.Exists(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Windows\System32\WindowsPowerShell\v1.0\Modules"))
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Windows\System32\WindowsPowerShell\v1.0\Modules", "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in dirs)
                    {
                        PowerShellModules.Text = PowerShellModules.Text + dir + Environment.NewLine;
                    }
                }
                catch { }
            }
            if (Directory.Exists(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Program Files (x86)\WindowsPowerShell\Modules"))
            {
                try
                {
                    string[] dirs = Directory.GetDirectories(System.IO.Path.GetPathRoot(Environment.SystemDirectory) + @"Program Files (x86)\WindowsPowerShell\Modules", "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in dirs)
                    {
                        PowerShellModules.Text = PowerShellModules.Text + dir + Environment.NewLine;
                    }
                }
                catch { }
            }

        }

        private void TroubleshootingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    /*
                    string URL = "https://google.com";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                    request.ContentType = "text/html";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        MessageBox.Show(reader.ReadToEnd());
                    }
                    */

                }).ContinueWith((task) =>
                {
                    // do this on the UI thread once the task has finished..
                }, System.Threading.CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
