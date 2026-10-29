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
