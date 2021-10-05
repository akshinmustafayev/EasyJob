using EasyJob.Serialization;
using EasyJob.TabItems;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for TabsDialog.xaml
    /// </summary>
    public partial class TabsDialog : Window
    {
        #region Variables

        public string configJson = "";
        public Config config;
        ObservableCollection<TabData> TabItems = null;

        #endregion

        #region Constructors

        public TabsDialog()
        {
            InitializeComponent();

            TabItems = new ObservableCollection<TabData>();
            DataContext = this;
            LoadConfig();
        }

        #endregion

        #region Methods

        public void LoadConfig()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                try
                {
                    configJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                    config = JsonConvert.DeserializeObject<Config>(configJson);

                    lstTabs.ItemsSource = null;
                    TabItems = Helpers.Utils.LoadConfigs(config);                    
                    lstTabs.ItemsSource = TabItems;
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
                    config.tabs = Helpers.Utils.SaveConfigs(TabItems);

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

        #endregion

        #region Events

        private void btnAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtNewTab.Text))
            {
                TabData tabData = new TabData(txtNewTab.Text);
                TabItems.Add(tabData);

                if (SaveConfig())
                {
                    LoadConfig();

                    txtNewTab.Text = string.Empty;
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

        private void btnTabDown_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.lstTabs.SelectedIndex;

            if (selectedIndex + 1 < this.TabItems.Count)
            {
                var itemToMoveDown = this.TabItems[selectedIndex];
                this.TabItems.RemoveAt(selectedIndex);
                this.TabItems.Insert(selectedIndex + 1, itemToMoveDown);
                this.lstTabs.SelectedIndex = selectedIndex + 1;
            }
        }

        private void btnTabUp_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.lstTabs.SelectedIndex;
            
            if (selectedIndex > 0)
            {
                var itemToMoveUp = this.TabItems[selectedIndex];
                this.TabItems.RemoveAt(selectedIndex);
                this.TabItems.Insert(selectedIndex - 1, itemToMoveUp);
                this.lstTabs.SelectedIndex = selectedIndex - 1;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        #endregion
    }
}
