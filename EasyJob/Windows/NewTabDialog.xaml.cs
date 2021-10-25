using EasyJob.Serialization;
using EasyJob.TabItems;
using EasyJob.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for NewTabDialog.xaml
    /// </summary>
    public partial class NewTabDialog : Window
    {
        public string configJson = "";
        public Config config;
        ObservableCollection<TabData> TabItems = null;

        public NewTabDialog()
        {
            InitializeComponent();
            LoadConfig();
        }

        public void LoadConfig()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                try
                {
                    configJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                    config = JsonConvert.DeserializeObject<Config>(configJson);

                    TabItems = ConfigUtils.ConvertTabsFromConfigToUI(config);
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

        public bool SaveConfig()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "config.json";
            if (File.Exists(path))
            {
                try
                {
                    config.tabs.Clear();
                    config.tabs = ConfigUtils.ConvertTabsFromUIToConfig(TabItems);

                    if (ConfigUtils.SaveFromConfigToFile(config) == true)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                //SaveConfig();
            }

            return false;
        }

        private void CreateNewTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CreateNewTabTextBox.Text))
            {
                TabData tabData = new TabData(CreateNewTabTextBox.Text);
                TabItems.Add(tabData);

                if (SaveConfig())
                {
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Error trying to save added Tab.");
                }
            }
            else
            {
                MessageBox.Show("Tab header name should not be empty.");
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
