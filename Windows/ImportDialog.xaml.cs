using EasyJob.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for ImportDialog.xaml
    /// </summary>
    public partial class ImportDialog : Window
    {
        #region Variables

        public string configPath = "";
        public Config config;

        string selectedFile = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportDialog"/> class.
        /// </summary>
        public ImportDialog()
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
        /// Imports the configuration.
        /// </summary>
        /// <param name="zipFile">The file.</param>
        /// <returns></returns>
        public bool ImportConfig(string zipFile)
        {
            if (File.Exists(zipFile))
            {
                string settingsFolder = Path.GetDirectoryName(zipFile);

                try
                {
                    ZipFile.ExtractToDirectory(zipFile, settingsFolder);

                    using (var settingsInputStream = new StreamReader(Path.Combine(settingsFolder, "easyjob_settings.json")))
                    {
                        using (var settingsOutputStream = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")))
                        {
                            copyToOutputStream(settingsInputStream, settingsOutputStream);
                        }
                    }

                    if ((bool)ckIncludeScripts.IsChecked)
                    {
                        LoadConfig();

                        string scriptsPath = Path.Combine(settingsFolder, "scripts");

                        foreach (ConfigTab tabs in config.tabs)
                        {
                            foreach (ConfigButton button in tabs.Buttons)
                            {
                                string[] scriptsContent = Directory.GetFiles(scriptsPath);

                                foreach (string script in scriptsContent)
                                {
                                    if (Path.GetFileName(button.Script).Equals(Path.GetFileName(script)))
                                    {
                                        using (var scriptsInputStream = new StreamReader(script))
                                        {
                                            using (var scriptsOutputStream = new StreamWriter(button.Script))
                                            {
                                                copyToOutputStream(scriptsInputStream, scriptsOutputStream);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    System.IO.DirectoryInfo di = new DirectoryInfo(settingsFolder);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        if (!file.Extension.Equals(".zip"))
                        {
                            file.Delete();
                        }
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
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

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (ImportConfig(txtBrowserFile.Text))
            {
                MessageBox.Show("Configurations imported successfully.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Error tring to import configurations.");
            }
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "zip|*.zip";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtBrowserFile.Text = openFileDialog.FileName;
                    selectedFile = txtBrowserFile.Text;
                }
            }
        }

        #endregion
    }
}
