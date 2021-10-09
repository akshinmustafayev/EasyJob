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
    /// Interaction logic for ReorderTabsDialog.xaml
    /// </summary>
    public partial class ReorderTabsDialog : Window
    {
        public string configJson = "";
        public Config config;
        ObservableCollection<TabData> TabItems = null;

        public ReorderTabsDialog()
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

                    MainWindowTabsList.ItemsSource = null;
                    TabItems = Helper.LoadConfigs(config);
                    MainWindowTabsList.ItemsSource = TabItems;
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
                    config.tabs = Helper.SaveConfigs(TabItems);

                    string conf = System.Text.Json.JsonSerializer.Serialize(config);
                    File.WriteAllText(path, conf, System.Text.Encoding.UTF8);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                SaveConfig();
            }

            return false;
        }

        private void TabsRedorderDown_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.MainWindowTabsList.SelectedIndex;

            if (selectedIndex + 1 < this.TabItems.Count)
            {
                var itemToMoveDown = this.TabItems[selectedIndex];
                this.TabItems.RemoveAt(selectedIndex);
                this.TabItems.Insert(selectedIndex + 1, itemToMoveDown);
                this.MainWindowTabsList.SelectedIndex = selectedIndex + 1;
            }

            SaveConfig();
        }

        private void TabsReorderUp_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.MainWindowTabsList.SelectedIndex;

            if (selectedIndex > 0)
            {
                var itemToMoveUp = this.TabItems[selectedIndex];
                this.TabItems.RemoveAt(selectedIndex);
                this.TabItems.Insert(selectedIndex - 1, itemToMoveUp);
                this.MainWindowTabsList.SelectedIndex = selectedIndex - 1;
            }

            SaveConfig();
        }

    }
}
