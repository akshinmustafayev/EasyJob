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
        public string configJson = "";
        public Config config;
        public int currentTabIndex = 0;
        ObservableCollection<TabData> TabItems = null;
        ObservableCollection<ActionButton> ActionButtons = null;

        public ReorderActionButtonsDialog(int _currentTabIndex)
        {
            InitializeComponent();
            currentTabIndex = _currentTabIndex;
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

                    MainWindowActionButtonsList.ItemsSource = null;

                    TabItems = Helper.LoadConfigs(config);

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
                    File.WriteAllText(path, conf, Encoding.UTF8);

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

                //List<ActionButton> myList = new List<ActionButton>(MainWindowActionButtonsList.DataContext);
                List<ActionButton> myList = new List<ActionButton>(ActionButtons);
                TabItems[currentTabIndex].TabActionButtons = myList;
            }

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

            SaveConfig();
        }
    }
}
