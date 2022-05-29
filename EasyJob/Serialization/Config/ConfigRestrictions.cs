using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyJob.Serialization
{
    public class ConfigRestrictions
    {
        public bool block_tabs_remove { get; set; }
        public bool block_buttons_remove { get; set; }
        public bool block_tabs_add { get; set; }
        public bool block_buttons_add { get; set; }
        public bool block_buttons_reorder { get; set; }
        public bool block_buttons_edit { get; set; }
        public bool block_tabs_rename { get; set; }
        public bool block_buttons_paste { get; set; }
        public bool block_buttons_copy { get; set; }
        public bool hide_menu_item_file_reload_config { get; set; }
        public bool hide_menu_item_file_open_app_folder { get; set; }
        public bool hide_menu_item_file_clear_events_list { get; set; }
        public bool hide_menu_item_settings { get; set; }
        public bool hide_menu_item_settings_workflow { get; set; }
        public bool hide_menu_item_settings_workflow_reorder_tabs { get; set; }
        public bool hide_menu_item_settings_workflow_add_tab { get; set; }
        public bool hide_menu_item_settings_workflow_remove_current_tab { get; set; }
        public bool hide_menu_item_settings_workflow_rename_current_tab { get; set; }
        public bool hide_menu_item_settings_workflow_add_button_to_current_tab { get; set; }
        public bool hide_menu_item_settings_workflow_reorder_buttons_in_current_tab { get; set; }
        public bool hide_menu_item_settings_configuration { get; set; }
        public bool hide_menu_item_help { get; set; }
        public bool hide_menu_item_help_troubleshooting { get; set; }
        public bool hide_menu_item_help_colortags { get; set; }
        public bool hide_menu_item_help_about { get; set; }
    }
}
