namespace EasyJob.Models;

/// <summary>On-disk configuration. Property names are kept identical to the 1.x config.json format.</summary>
public class AppConfig
{
    public string default_powershell_path { get; set; } = "";
    public string default_cmd_path { get; set; } = "";
    public string default_shell_path { get; set; } = "";
    public string default_python_path { get; set; } = "";
    public string powershell_arguments { get; set; } = "";
    /// <summary>Show PowerShell's own colors (errors, tables, Write-Host -ForegroundColor) in the output.</summary>
    public bool powershell_native_colors { get; set; } = true;
    /// <summary>Open the PowerShell commands panel automatically when the script editor opens.</summary>
    public bool editor_open_commands_panel { get; set; } = true;
    public string console_background { get; set; } = "#0C0C0C";
    public string console_foreground { get; set; } = "#E6E6E6";
    public bool clear_events_when_reload { get; set; } = true;
    /// <summary>"system", "light" or "dark".</summary>
    public string theme { get; set; } = "system";
    /// <summary>Script button size: auto (compact once a tab has many buttons), cards or compact.</summary>
    /// <summary>Look for a newer EasyJob release on GitHub when the app starts.</summary>
    public bool check_updates_on_startup { get; set; } = true;
    public string button_density { get; set; } = "auto";
    public ConfigRestrictions restrictions { get; set; } = new();

    /// <summary>The restrictions as the user's own config.json has them (without the administrator's policy), so saving keeps them apart.</summary>
    [System.Text.Json.Serialization.JsonIgnore] public ConfigRestrictions? UserRestrictions { get; set; }

    /// <summary>A copy for writing to disk: the user's own restrictions, not the policy's.</summary>
    public AppConfig ForSave()
    {
        var copy = (AppConfig)MemberwiseClone();
        copy.restrictions = UserRestrictions ?? restrictions;
        return copy;
    }

    public List<ConfigTab> tabs { get; set; } = new();
}

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
    public bool hide_menu_item_help_about { get; set; }

    // ---- administration (see PolicyService; set them in policy.json to keep users from changing them) ----

    /// <summary>Nothing can be changed or saved: tabs, buttons and settings stay as shipped.</summary>
    public bool config_readonly { get; set; }
    /// <summary>The script editor opens files for reading and running only.</summary>
    public bool script_editor_readonly { get; set; }
    /// <summary>Ask "Run this script?" before every run.</summary>
    public bool confirm_before_run { get; set; }
    public bool block_script_editor { get; set; }
    public bool block_script_editor_run { get; set; }
    public bool block_script_editor_open_files { get; set; }
    public bool block_powershell_commands { get; set; }
    /// <summary>No interactive terminal under the script editor.</summary>
    public bool block_editor_terminal { get; set; }
    public bool block_open_script_externally { get; set; }
    public bool block_show_in_folder { get; set; }
    public bool block_updates { get; set; }
    public bool block_view_settings { get; set; }
    public bool hide_menu_item_tools { get; set; }
    public bool hide_menu_item_view_event_log { get; set; }

    /// <summary>When not empty, only these script types may run: powershell, python, shell, cmd.</summary>
    public List<string> allowed_script_types { get; set; } = new();
    /// <summary>When not empty, scripts may only be run, added or edited inside these folders. {config} and {home} are replaced.</summary>
    public List<string> allowed_script_folders { get; set; } = new();
    /// <summary>0 = no limit.</summary>
    public int max_concurrent_runs { get; set; }
}

public class ConfigTab
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string Header { get; set; } = "";
    public List<ConfigButton> Buttons { get; set; } = new();
}

public class ConfigButton
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public string Description { get; set; } = "";
    public string Script { get; set; } = "";
    public string ScriptPathType { get; set; } = "relative";
    public string ScriptType { get; set; } = "powershell";
    public List<ConfigArgument> Arguments { get; set; } = new();

    public ConfigButton Clone() => new()
    {
        Id = Guid.NewGuid(),
        Text = Text,
        Description = Description,
        Script = Script,
        ScriptPathType = ScriptPathType,
        ScriptType = ScriptType,
        Arguments = Arguments.Select(a => new ConfigArgument { ArgumentQuestion = a.ArgumentQuestion, ArgumentAnswer = a.ArgumentAnswer }).ToList()
    };
}

public class ConfigArgument
{
    public string ArgumentQuestion { get; set; } = "";
    public string ArgumentAnswer { get; set; } = "";

    // Configs written by EasyJob 1.x use snake_case keys; read them too (they are saved back in the new form).
    [System.Text.Json.Serialization.JsonPropertyName("argument_question"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyQuestion { get => null; set { if (!string.IsNullOrEmpty(value)) ArgumentQuestion = value; } }

    [System.Text.Json.Serialization.JsonPropertyName("argument_answer"), System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyAnswer { get => null; set { if (!string.IsNullOrEmpty(value)) ArgumentAnswer = value; } }
}
