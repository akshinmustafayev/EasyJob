using EasyJob.Serialization;
using EasyJob.Serialization.AnswerDialog;
using EasyJob.TabItems;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EasyJob.Helpers
{
    public static class Utils
    {
        /// <summary>
        /// Loads the configs.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        public static ObservableCollection<TabData> LoadConfigs(Config config)
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

                    actionButtons.Add(new ActionButton { ButtonText = configButton.Text, ButtonDescription = configButton.Description, ButtonScript = configButton.Script, ButtonScriptPathType = configButton.ScriptPathType, ButtonArguments = configArguments });
                }

                tabs.Add(new TabData { TabHeader = configTab.Header, ConsoleBackground = config.console_background, ConsoleForeground = config.console_foreground, TabActionButtons = actionButtons, TabTextBoxText = "" });
            }

            return tabs;
        }

        /// <summary>
        /// Saves the configs.
        /// </summary>
        /// <param name="tabs">The tabs.</param>
        /// <returns></returns>
        public static List<ConfigTab> SaveConfigs(IEnumerable<TabData> tabs)
        {
            List<ConfigTab> configTabs = new List<ConfigTab>();
            List<ConfigButton> buttons = new List<ConfigButton>();
            List<ConfigArgument> configArguments = new List<ConfigArgument>();

            foreach (TabData tab in tabs)
            {
                foreach (ActionButton button in tab.TabActionButtons)
                {
                    foreach (Answer answer in button.ButtonArguments)
                    {
                        configArguments.Add(new ConfigArgument(answer.AnswerQuestion, answer.AnswerResult));
                    }

                    buttons.Add(new ConfigButton(button.ButtonText, button.ButtonDescription, button.ButtonScript, button.ButtonScriptPathType, configArguments));
                }

                configTabs.Add(new ConfigTab(tab.TabHeader, buttons));
            }

            return configTabs;
        }
    }
}
