using EasyJob.Serialization;
using EasyJob.Serialization.AnswerDialog;
using EasyJob.TabItems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EasyJob.Utils
{
    public class ConfigUtils
    {
        public static string ConfigJsonPath = AppDomain.CurrentDomain.BaseDirectory + "config.json";


        public static ObservableCollection<TabData> ConvertTabsFromConfigToUI(Config config)
        {
            ObservableCollection<TabData> tabs = new ObservableCollection<TabData>();

            foreach (ConfigTab configTab in config.tabs)
            {
                List<ActionButton> actionButtons = new List<ActionButton>();

                foreach (ConfigButton configButton in configTab.Buttons)
                {
                    List<Answer> configArguments = new List<Answer>();
                    foreach (ConfigArgument configArgument in configButton.Arguments)
                    {
                        configArguments.Add(new Answer { AnswerQuestion = configArgument.ArgumentQuestion, AnswerResult = configArgument.ArgumentAnswer });
                    }

                    actionButtons.Add(new ActionButton { ID = configButton.Id, ButtonText = configButton.Text, ButtonDescription = configButton.Description, ButtonScript = configButton.Script, ButtonScriptPathType = configButton.ScriptPathType, ButtonScriptType = configButton.ScriptType, ButtonArguments = configArguments });
                }

                tabs.Add(new TabData { ID = configTab.ID, TabHeader = configTab.Header, ConsoleBackground = config.console_background, ConsoleForeground = config.console_foreground, TabActionButtons = actionButtons, TabTextBoxText = "" });
            }

            return tabs;
        }

        /// <summary>
        /// Saves the configs.
        /// </summary>
        /// <param name="tabs">The tabs.</param>
        /// <returns></returns>
        public static List<ConfigTab> ConvertTabsFromUIToConfig(IEnumerable<TabData> tabs)
        {
            List<ConfigTab> configTabs = new List<ConfigTab>();
            List<ConfigButton> buttons = null;
            List<ConfigArgument> configArguments = null;

            foreach (TabData tab in tabs)
            {
                buttons = new List<ConfigButton>();
                foreach (ActionButton button in tab.TabActionButtons)
                {
                    configArguments = new List<ConfigArgument>();
                    foreach (Answer answer in button.ButtonArguments)
                    {
                        configArguments.Add(new ConfigArgument(answer.AnswerQuestion, answer.AnswerResult));
                    }

                    buttons.Add(new ConfigButton(button.ID, button.ButtonText, button.ButtonDescription, button.ButtonScript, button.ButtonScriptPathType, button.ButtonScriptType, configArguments));
                }

                configTabs.Add(new ConfigTab(tab.ID, tab.TabHeader, buttons));
            }

            return configTabs;
        }

        public static bool SaveFromConfigToFile(Config config)
        {
            try
            {
                string confifJson = JsonConvert.SerializeObject(config);
                File.WriteAllText(ConfigJsonPath, confifJson, Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
    }
}
