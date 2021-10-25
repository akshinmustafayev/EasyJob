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
        public Config config;
        ObservableCollection<TabData> TabItems = null;
        public bool changesOccured = false;

        public ReorderTabsDialog(Config _config)
        {
            InitializeComponent();
            config = _config;
            LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                MainWindowTabsList.ItemsSource = null;
                TabItems = ConfigUtils.ConvertTabsFromConfigToUI(config);
                MainWindowTabsList.ItemsSource = TabItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public bool SaveConfig()
        {
            if (File.Exists(ConfigUtils.ConfigJsonPath))
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

            changesOccured = true;

            SaveConfig();
        }

        private void TabsReorderUp_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowTabsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to reorder");
                return;
            }

            changesOccured = true;

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
