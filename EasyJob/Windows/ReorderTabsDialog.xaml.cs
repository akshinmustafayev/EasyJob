using EasyJob.Serialization;
using EasyJob.TabItems;
using EasyJob.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

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
                SaveConfig();
            }

            return false;
        }

        private void TabsRedorderDown_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindowTabsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to reorder");
                return;
            }

            var selectedIndex = MainWindowTabsList.SelectedIndex;

            if (selectedIndex + 1 < TabItems.Count)
            {
                var itemToMoveDown = TabItems[selectedIndex];
                TabItems.RemoveAt(selectedIndex);
                TabItems.Insert(selectedIndex + 1, itemToMoveDown);
                MainWindowTabsList.SelectedIndex = selectedIndex + 1;
            }

            SaveConfig();
        }

        private void TabsReorderUp_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowTabsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to reorder");
                return;
            }

            var selectedIndex = MainWindowTabsList.SelectedIndex;

            if (selectedIndex > 0)
            {
                var itemToMoveUp = TabItems[selectedIndex];
                TabItems.RemoveAt(selectedIndex);
                TabItems.Insert(selectedIndex - 1, itemToMoveUp);
                MainWindowTabsList.SelectedIndex = selectedIndex - 1;
            }

            SaveConfig();
        }

    }
}
