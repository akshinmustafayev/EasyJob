using EasyJob.Serialization;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for ExportDialog.xaml
    /// </summary>
    public partial class ExportDialog : Window
    {
        #region Variables

        public string configPath = "";
        public Config config;

        string selectedFolder = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDialog"/> class.
        /// </summary>
        public ExportDialog()
        {
            InitializeComponent();            
        }

        #endregion

        #region Methods

        private void copyToOutputStream(StreamReader inputStream, StreamWriter outputStream)
        {
            string line = null;
            while ((line = inputStream.ReadLine()) != null)
            {
                outputStream.WriteLine(line);
            }
            outputStream.Write(inputStream.ReadToEnd());
        }

        /// <summary>
        /// Exports the configuration.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <returns></returns>
        public bool ExportConfig(string folder)
        {
            if (Directory.Exists(folder))
            {
                try
                {
                    string settingsPath = Path.Combine(folder, "settings");
                    if (!Directory.Exists(settingsPath))
                        Directory.CreateDirectory(settingsPath);

                    configPath = AppDomain.CurrentDomain.BaseDirectory + "config.json";
                    LoadConfig();
                    string zipPath = Path.Combine(folder, "settings");

                    using (var settingsInputStream = new StreamReader(configPath))
                    {
                        using (var settingsOutputStream = new StreamWriter(Path.Combine(zipPath, "easyjob_settings.json")))
                        {
                            copyToOutputStream(settingsInputStream, settingsOutputStream);

                            if ((bool)ckIncludeScripts.IsChecked)
                            {
                                string scriptsPath = Path.Combine(zipPath, "scripts");

                                if (!Directory.Exists(scriptsPath))
                                    Directory.CreateDirectory(scriptsPath);

                                foreach (ConfigTab tabs in config.tabs)
                                {
                                    foreach (ConfigButton button in tabs.Buttons)
                                    {
                                        if (File.Exists(button.Script))
                                        {
                                            using (var scriptsInputStream = new StreamReader(button.Script))
                                            {
                                                using (var scriptsOutputStream = new StreamWriter(Path.Combine(scriptsPath, Path.GetFileName(button.Script))))
                                                {
                                                    copyToOutputStream(scriptsInputStream, scriptsOutputStream);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string zipFolder = Path.Combine(folder, "settings.zip");

                    ZipFile.CreateFromDirectory(settingsPath, zipFolder);

                    Directory.Delete(settingsPath, true);

                    return true;
                }
                catch(Exception ex)
                {
                    return false;
                }
            }

            return false;
        }

        public void LoadConfig()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                try
                {
                    string configJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                    config = JsonConvert.DeserializeObject<Config>(configJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("File " + AppDomain.CurrentDomain.BaseDirectory + "config.json does not exist.");
            }
        }

        #endregion

        #region Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            dialog.RootFolder = Environment.SpecialFolder.Desktop;

            if ((bool)dialog.ShowDialog(this))
            {
                txtBrowserFolder.Text = dialog.SelectedPath;
                selectedFolder = txtBrowserFolder.Text;
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ExportConfig(txtBrowserFolder.Text))
            {
                MessageBox.Show("Configurations exported successfully.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Error tring to export configurations.");
            }
        }

        #endregion
    }
}
