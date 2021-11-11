using EasyJob.Serialization;
using EasyJob.Utils;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class ConfigurationDialog : Window
    {
        private Config config;

        public ConfigurationDialog(Config _config)
        {
            InitializeComponent();
            config = _config;
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // Common parameters
            DefaultPowerShellPath.Text = config.default_powershell_path;
            DefaultCMDPath.Text = config.default_cmd_path;
            PowerShellArguments.Text = config.powershell_arguments;
            ConsoleBackground.Text = config.console_background;
            ConsoleForeground.Text = config.console_foreground;
            SetComboBoxFromValue(ConsoleIgnoreColorTags, config.console_ignore_color_tags);
            SetComboBoxFromValue(ClearEventsWhenReload, config.clear_events_when_reload);

            // Restrictions
            SetComboBoxFromValue(BlockTabsRemove, config.restrictions.block_tabs_remove);
            SetComboBoxFromValue(BlockButtonsRemove, config.restrictions.block_buttons_remove);
            SetComboBoxFromValue(BlockTabsAdd, config.restrictions.block_tabs_add);
            SetComboBoxFromValue(BlockButtonsAdd, config.restrictions.block_buttons_add);
            SetComboBoxFromValue(BlockButtonsReorder, config.restrictions.block_buttons_reorder);
            SetComboBoxFromValue(BlockButtonsEdit, config.restrictions.block_buttons_edit);
            SetComboBoxFromValue(BlockTabsRename, config.restrictions.block_tabs_rename);
            SetComboBoxFromValue(HideFileReloadConfigMenuItem, config.restrictions.hide_menu_item_file_reload_config);
            SetComboBoxFromValue(HideFileOpenAppFolderMenuItem, config.restrictions.hide_menu_item_file_open_app_folder);
            SetComboBoxFromValue(HideFileClearEventsListMenuItem, config.restrictions.hide_menu_item_file_clear_events_list);
            SetComboBoxFromValue(HideSettingsMenuItem, config.restrictions.hide_menu_item_settings);
            SetComboBoxFromValue(HideSettingsWorkflowMenuItem, config.restrictions.hide_menu_item_settings_workflow);
            SetComboBoxFromValue(HideSettingsWorkflowReorderTabsMenuItem, config.restrictions.hide_menu_item_settings_workflow_reorder_tabs);
            SetComboBoxFromValue(HideSettingsWorkflowAddTabMenuItem, config.restrictions.hide_menu_item_settings_workflow_add_tab);
            SetComboBoxFromValue(HideSettingsWorkflowRemoveCurrentTabMenuItem, config.restrictions.hide_menu_item_settings_workflow_remove_current_tab);
            SetComboBoxFromValue(HideSettingsWorkflowRenameCurrentTabMenuItem, config.restrictions.hide_menu_item_settings_workflow_rename_current_tab);
            SetComboBoxFromValue(HideSettingsWorkflowAddButtonToCurrentTabMenuItem, config.restrictions.hide_menu_item_settings_workflow_add_button_to_current_tab);
            SetComboBoxFromValue(HideSettingsWorkflowReorderButtonsInCurrentTabMenuItem, config.restrictions.hide_menu_item_settings_workflow_reorder_buttons_in_current_tab);
            SetComboBoxFromValue(HideSettingsConfigurationMenuItem, config.restrictions.hide_menu_item_settings_configuration);
            SetComboBoxFromValue(HideHelpMenuItem, config.restrictions.hide_menu_item_help);
            SetComboBoxFromValue(HideHelpTroubleshootingMenuItem, config.restrictions.hide_menu_item_help_troubleshooting);
            SetComboBoxFromValue(HideHelpColorTagsMenuItem, config.restrictions.hide_menu_item_help_colortags);
            SetComboBoxFromValue(HideHelpAboutMenuItem, config.restrictions.hide_menu_item_help_about);
        }

        private void SetComboBoxFromValue(ComboBox comboBox, bool value)
        {
            if(value == true)
            {
                comboBox.SelectedIndex = 0;
            }
            else
            {
                comboBox.SelectedIndex = 1;
            }
        }

        private bool GetComboBoxValue(ComboBox comboBox)
        {
            if (comboBox.SelectedIndex == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            config.default_powershell_path = DefaultPowerShellPath.Text;
            config.default_cmd_path = DefaultCMDPath.Text;
            config.powershell_arguments = PowerShellArguments.Text;
            config.console_background = ConsoleBackground.Text;
            config.console_foreground = ConsoleForeground.Text;
            config.console_ignore_color_tags = GetComboBoxValue(ConsoleIgnoreColorTags);
            config.clear_events_when_reload = GetComboBoxValue(ClearEventsWhenReload);

            config.restrictions.block_tabs_remove = GetComboBoxValue(BlockTabsRemove);
            config.restrictions.block_buttons_remove = GetComboBoxValue(BlockButtonsRemove);
            config.restrictions.block_tabs_add = GetComboBoxValue(BlockTabsAdd);
            config.restrictions.block_buttons_add = GetComboBoxValue(BlockButtonsAdd);
            config.restrictions.block_buttons_reorder = GetComboBoxValue(BlockButtonsReorder);
            config.restrictions.block_buttons_edit = GetComboBoxValue(BlockButtonsEdit);
            config.restrictions.block_tabs_rename = GetComboBoxValue(BlockTabsRename);
            config.restrictions.hide_menu_item_file_reload_config = GetComboBoxValue(HideFileReloadConfigMenuItem);
            config.restrictions.hide_menu_item_file_open_app_folder = GetComboBoxValue(HideFileOpenAppFolderMenuItem);
            config.restrictions.hide_menu_item_file_clear_events_list = GetComboBoxValue(HideFileClearEventsListMenuItem);
            config.restrictions.hide_menu_item_settings = GetComboBoxValue(HideSettingsMenuItem);
            config.restrictions.hide_menu_item_settings_workflow = GetComboBoxValue(HideSettingsWorkflowMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_reorder_tabs = GetComboBoxValue(HideSettingsWorkflowReorderTabsMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_add_tab = GetComboBoxValue(HideSettingsWorkflowAddTabMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_remove_current_tab = GetComboBoxValue(HideSettingsWorkflowRemoveCurrentTabMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_rename_current_tab = GetComboBoxValue(HideSettingsWorkflowRenameCurrentTabMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_add_button_to_current_tab = GetComboBoxValue(HideSettingsWorkflowAddButtonToCurrentTabMenuItem);
            config.restrictions.hide_menu_item_settings_workflow_reorder_buttons_in_current_tab = GetComboBoxValue(HideSettingsWorkflowReorderButtonsInCurrentTabMenuItem);
            config.restrictions.hide_menu_item_settings_configuration = GetComboBoxValue(HideSettingsConfigurationMenuItem);
            config.restrictions.hide_menu_item_help = GetComboBoxValue(HideHelpMenuItem);
            config.restrictions.hide_menu_item_help_troubleshooting = GetComboBoxValue(HideHelpTroubleshootingMenuItem);
            config.restrictions.hide_menu_item_help_colortags = GetComboBoxValue(HideHelpColorTagsMenuItem);
            config.restrictions.hide_menu_item_help_about = GetComboBoxValue(HideHelpAboutMenuItem);

            if(ConfigUtils.SaveFromConfigToFile(config) == true)
            {
                MessageBox.Show("Settings saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HelpDialog hd = new HelpDialog(button.Name);
            hd.ShowDialog();
        }
    }
}
