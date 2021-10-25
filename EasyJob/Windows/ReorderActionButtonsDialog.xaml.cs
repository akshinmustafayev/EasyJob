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
    /// Interaction logic for ReorderActionButtonsDialog.xaml
    /// </summary>
    public partial class ReorderActionButtonsDialog : Window
    {
        public Config config;
        public int currentTabIndex = 0;
        public bool changesOccured = false;
        ObservableCollection<TabData> TabItems = null;
        ObservableCollection<ActionButton> ActionButtons = null;

        public ReorderActionButtonsDialog(int _currentTabIndex, Config _config)
        {
            InitializeComponent();
            config = _config;
            currentTabIndex = _currentTabIndex;
            LoadConfig();
        }

        public void LoadConfig()
        {
            try
            {
                MainWindowActionButtonsList.ItemsSource = null;

                TabItems = ConfigUtils.ConvertTabsFromConfigToUI(config);

                List<ActionButton> list = TabItems[currentTabIndex].TabActionButtons;
                ObservableCollection<ActionButton> collection = new ObservableCollection<ActionButton>(list);

                ActionButtons = collection;
                
                MainWindowActionButtonsList.ItemsSource = ActionButtons;
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

        private void ActionButtonsRedorderDown_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowActionButtonsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to reorder");
                return;
            }

            var selectedIndex = MainWindowActionButtonsList.SelectedIndex;
            
            if (selectedIndex + 1 < ActionButtons.Count)
            {
                var itemToMoveDown = ActionButtons[selectedIndex];
                ActionButtons.RemoveAt(selectedIndex);
                ActionButtons.Insert(selectedIndex + 1, itemToMoveDown);
                MainWindowActionButtonsList.SelectedIndex = selectedIndex + 1;

                List<ActionButton> myList = new List<ActionButton>(ActionButtons);
                TabItems[currentTabIndex].TabActionButtons = myList;
            }

            changesOccured = true;

            SaveConfig();
        }

        private void ActionButtonsReorderUp_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowActionButtonsList.SelectedIndex == -1)
            {
                MessageBox.Show("Please select item to reorder");
                return;
            }

            var selectedIndex = MainWindowActionButtonsList.SelectedIndex;

            if (selectedIndex > 0)
            {
                var itemToMoveUp = ActionButtons[selectedIndex];
                ActionButtons.RemoveAt(selectedIndex);
                ActionButtons.Insert(selectedIndex - 1, itemToMoveUp);
                MainWindowActionButtonsList.SelectedIndex = selectedIndex - 1;
                List<ActionButton> myList = new List<ActionButton>(ActionButtons);
                TabItems[currentTabIndex].TabActionButtons = myList;
            }

            changesOccured = true;

            SaveConfig();
        }
    }
}
