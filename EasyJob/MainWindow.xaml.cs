using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EasyJob.Serialization;
using EasyJob.Serialization.AnswerDialog;
using EasyJob.Serialization.TasksList;
using EasyJob.TabItems;
using EasyJob.Utils;
using EasyJob.Windows;
using Newtonsoft.Json;
using WpfRichText;

namespace EasyJob
{
    public partial class MainWindow : Window
    {
        ActionButton actionButtonForCopy = null;
        TabData selectedTabItem = null;
        ActionButton selectedActionButton = null;
        ObservableCollection<TaskListTask> tasksList = new ObservableCollection<TaskListTask>();
        public Config config;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            MainMenuItemsVisibility();
        }

        public void LoadConfig()
        {
            if (File.Exists(ConfigUtils.ConfigJsonPath))
            {
                try
                {
                    string configJson = File.ReadAllText(ConfigUtils.ConfigJsonPath);
                    config = JsonConvert.DeserializeObject<Config>(configJson);

                    MainTab.ItemsSource = ConfigUtils.ConvertTabsFromConfigToUI(config);

                    AddTextToEventsList("Config loaded from file: " + ConfigUtils.ConfigJsonPath, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddTextToEventsList("Error occured while loading file: " + ex.Message, false);
                }
            }
            else
            {
                MessageBox.Show("File " + ConfigUtils.ConfigJsonPath + " does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool SaveConfig()
        {
            if (File.Exists(ConfigUtils.ConfigJsonPath))
            {
                try
                {
                    IEnumerable<TabData> tabs = (IEnumerable<TabData>)MainTab.ItemsSource;

                    config.tabs.Clear();

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

                    config.tabs = configTabs;

                    if (ConfigUtils.SaveFromConfigToFile(config) == true)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            else
            {
                SaveConfig();
            }

            return false;
        }

        private void MainMenuItemsVisibility()
        {
            // File
            if (config.restrictions.hide_menu_item_file_reload_config == true) { FileReloadConfigMenuItem.Visibility = Visibility.Collapsed; } else { FileReloadConfigMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_file_open_app_folder == true) { FileOpenAppFolderMenuItem.Visibility = Visibility.Collapsed; } else { FileOpenAppFolderMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_file_clear_events_list == true) { FileClearEventsLogListMenuItem.Visibility = Visibility.Collapsed; } else { FileClearEventsLogListMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_file_reload_config == true && config.restrictions.hide_menu_item_file_open_app_folder == true && config.restrictions.hide_menu_item_file_clear_events_list == true) { FileSeparator1.Visibility = Visibility.Collapsed; } else { FileSeparator1.Visibility = Visibility.Visible; }

            // Settings
            if (config.restrictions.hide_menu_item_settings == true) { SettingsMenuItem.Visibility = Visibility.Collapsed; } else { SettingsMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow == true) { SettingsWorkflowMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_reorder_tabs == true) { SettingsWorkflowReorderTabsMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowReorderTabsMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_add_tab == true) { SettingsWorkflowAddTabMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowAddTabMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_remove_current_tab == true) { SettingsWorkflowRemoveCurrentTabMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowRemoveCurrentTabMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_rename_current_tab == true) { SettingsWorkflowRenameCurrentTabMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowRenameCurrentTabMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_add_button_to_current_tab == true) { SettingsWorkflowAddButtonToCurrentTabMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowAddButtonToCurrentTabMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_workflow_reorder_buttons_in_current_tab == true) { SettingsWorkflowReorderButtonsInCurrentTabMenuItem.Visibility = Visibility.Collapsed; } else { SettingsWorkflowReorderButtonsInCurrentTabMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_settings_configuration == true) { SettingsConfigurationMenuItem.Visibility = Visibility.Collapsed; } else { SettingsConfigurationMenuItem.Visibility = Visibility.Visible; }

            // Help
            if (config.restrictions.hide_menu_item_help == true) { HelpMenuItem.Visibility = Visibility.Collapsed; } else { HelpMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_help_troubleshooting == true) { HelpTroubleshootingMenuItem.Visibility = Visibility.Collapsed; } else { HelpTroubleshootingMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_help_colortags == true) { HelpColorTagsMenuItem.Visibility = Visibility.Collapsed; } else { HelpColorTagsMenuItem.Visibility = Visibility.Visible; }
            if (config.restrictions.hide_menu_item_help_about == true) { HelpAboutMenuItem.Visibility = Visibility.Collapsed; } else { HelpAboutMenuItem.Visibility = Visibility.Visible; }
        }

        private void AddTextToConsole (string Text, int OwnerTab)
        {
            if(Text == "" || Text == null || Text == "Error: ")
            {
                return;
            }

            if (config.console_ignore_color_tags == false)
            {
                Text = CommonUtils.ApplyConsoleColorTagsToText(Text);
            }

            //this.Dispatcher.Invoke(() =>
            //{
                TabData td = (TabData)MainTab.Items[OwnerTab];
                td.TabTextBoxText = td.TabTextBoxText + "<br>" + Text;
            //});
        }

        public void AddTextToEventsList(string Text, bool IsAsync)
        {
            if (Text == "" || Text == null || Text == "Error: ")
            {
                return;
            }

            if (IsAsync == true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    EventsList.Items.Add(Text);
                    EventsList.ScrollIntoView(EventsList.Items.Count - 1);
                });
            }
            else
            {
                EventsList.Items.Add(Text);
                EventsList.ScrollIntoView(EventsList.Items.Count - 1);
            }
        }

        private void ScrollToBottomListBox(ListBox listBox, bool IsAsync)
        {
            if(listBox == null && listBox.Items.Count == 0)
            {
                return;
            }

            if (IsAsync == true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (listBox != null)
                    {
                        var border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                        var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                        scrollViewer.ScrollToBottom();
                    }
                });
            }
            else
            {
                if (listBox != null)
                {
                    var border = (Border)VisualTreeHelper.GetChild(listBox, 0);
                    var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }
        }

        private void RemoveTaskFromTasksList(int ProcessID, bool IsAsync)
        {
            if (IsAsync == true)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ObservableCollection<TaskListTask> taskListsNew = new ObservableCollection<TaskListTask>();
                    foreach (TaskListTask tlt in tasksList)
                    {
                        if (tlt.TaskID == ProcessID)
                        {
                            continue;
                        }
                        taskListsNew.Add(new TaskListTask { TaskID = tlt.TaskID });
                    }
                    tasksList = taskListsNew;
                    TasksList.ItemsSource = taskListsNew;
                });
            }
            else
            {
                ObservableCollection<TaskListTask> taskListsNew = new ObservableCollection<TaskListTask>();
                foreach (TaskListTask tlt in tasksList)
                {
                    if (tlt.TaskID == ProcessID)
                    {
                        continue;
                    }
                    taskListsNew.Add(new TaskListTask { TaskID = tlt.TaskID });
                }
                tasksList = taskListsNew;
                TasksList.ItemsSource = taskListsNew;
            }
        }

        private void AddTaskToTasksList(int ProcessID, string ProcessFileName)
        {
            tasksList.Add(new TaskListTask { TaskID = ProcessID, TaskFile = ProcessFileName });
            TasksList.ItemsSource = tasksList;
        }

        private string GetScriptPath(string ButtonScript, string ButtonScriptPathType)
        {
            string scriptPath = "";
            if (ButtonScriptPathType.ToLower() == "absolute")
            {
                scriptPath = ButtonScript;
            }
            else if (ButtonScriptPathType.ToLower() == "relative")
            {
                scriptPath = AppDomain.CurrentDomain.BaseDirectory + ButtonScript;
            }
            else
            {
                scriptPath = ButtonScript;
            }
            return scriptPath;
        }

        private string GetPowerShellArguments(string Arguments)
        {
            string arguments = "";
            
            if(Arguments.Length > 0)
            {
                arguments = " " + Arguments + " ";
            }

            return arguments;
        }

        private void ShowAddButtonDialog()
        {
            AddActionButtonDialog aabd = new AddActionButtonDialog();
            if (aabd.ShowDialog() == true)
            {
                if (MainTab.Items[MainTab.SelectedIndex] is TabData button)
                {
                    List<Answer> answers = new List<Answer>();
                    foreach (ConfigArgument ca in aabd.configButton.Arguments)
                    {
                        answers.Add(new Answer { AnswerQuestion = ca.ArgumentQuestion, AnswerResult = ca.ArgumentAnswer });
                    }
                    button.TabActionButtons.Add(new ActionButton { ButtonText = aabd.configButton.Text, ButtonDescription = aabd.configButton.Description, ButtonScript = aabd.configButton.Script, ButtonScriptPathType = aabd.configButton.ScriptPathType, ButtonScriptType = aabd.configButton.ScriptType, ButtonArguments = answers });
                }

                if (SaveConfig())
                {
                    MainTab.Items.Refresh();
                    this.UpdateLayout();
                    AddTextToEventsList("Button '" + aabd.configButton.Text + "' has been successfully added", false);
                }
            }
            else
            {
                AddTextToEventsList("Adding button cancelled by user", false);
            }
        }

        private void ShowRenameTabDialog(int SelectedTab, string SelectedTabHeader)
        {
            RenameTabDialog rtd = new RenameTabDialog(SelectedTabHeader);
            if (rtd.ShowDialog() == true)
            {
                List<TabData> newSourceTabs = new List<TabData>();
                foreach (TabData tab in MainTab.Items)
                {
                    TabData currentTab = MainTab.Items[SelectedTab] as TabData;
                    if (tab == currentTab)
                    {
                        tab.TabHeader = rtd.NewTabName;
                    }
                    newSourceTabs.Add(tab);
                }

                MainTab.ItemsSource = null;
                MainTab.ItemsSource = newSourceTabs;

                if (SaveConfig())
                {
                    MainTab.Items.Refresh();
                    this.UpdateLayout();
                }
            }
            else
            {
                AddTextToEventsList("Current tab rename cancelled by user", false);
            }
        }

        private void ShowAddNewTabDialog()
        {
            NewTabDialog ntd = new NewTabDialog();
            if(ntd.ShowDialog() == true)
            {
                LoadConfig();

                MainTab.Items.Refresh();
                this.UpdateLayout();
            }
            else
            {
                AddTextToEventsList("Adding new Tab cancelled by user", false);
            }
        }

        private void ShowReorderActionButtonsDialog()
        {
            int currentTab = MainTab.SelectedIndex;

            ReorderActionButtonsDialog rabd = new ReorderActionButtonsDialog(MainTab.SelectedIndex, config);
            rabd.ShowDialog();

            if (rabd.changesOccured)
            {
                LoadConfig();

                MainTab.Items.Refresh();
                this.UpdateLayout();
                AddTextToEventsList("Reorder action buttons dialog ended!", false);
                MainTab.SelectedIndex = currentTab;
            }
            else
            {
                AddTextToEventsList("Reorder action buttons dialog ended. No changes occured!", false);
            }
        }

        public void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            TabData td = (TabData)MainTab.SelectedItem;
            td.TabTextBoxText = "";
            AddTextToEventsList("Output has been cleared!", false);
        }

        public AnswerData ConvertArgumentsToAnswers(List<Answer> Answers)
        {
            AnswerData answerData = new AnswerData();
            answerData.Answers = Answers;
            return answerData;
        }

        public string ConvertArgumentsToPowerShell(List<Answer> Answers)
        {
            string powerShellArguments = "";
            foreach (Answer answer in Answers)
            {
                powerShellArguments = powerShellArguments + answer.AnswerResult + " ";
            }

            if (powerShellArguments.EndsWith(" "))
            {
                powerShellArguments = powerShellArguments.Remove(powerShellArguments.Length - 1);
            }

            return powerShellArguments;
        }

        public string ConvertArgumentsToCMD(List<Answer> Answers)
        {
            string powerShellArguments = "";
            foreach (Answer answer in Answers)
            {
                powerShellArguments = powerShellArguments + answer.AnswerResult + " ";
            }

            if (powerShellArguments.EndsWith(" "))
            {
                powerShellArguments = powerShellArguments.Remove(powerShellArguments.Length - 1);
            }

            return powerShellArguments;
        }

        public string RemoveScriptFileFromPath(string ScriptPath)
        {
            string[] scriptPathSplits = ScriptPath.Split("\\");
            string newScriptPath = "";

            if(scriptPathSplits.Length > 0)
            {
                for (int i = 0; i < scriptPathSplits.Length - 1; i++)
                {
                    newScriptPath += scriptPathSplits[i] + "\\";
                }
            }

            return newScriptPath;
        }

        public async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Button actionButton = sender as Button;
            string buttonScript = ((ActionButton)actionButton.DataContext).ButtonScript;
            string buttonScriptPathType = ((ActionButton)actionButton.DataContext).ButtonScriptPathType;
            string buttonScriptType = ((ActionButton)actionButton.DataContext).ButtonScriptType;
            string scriptPath = GetScriptPath(buttonScript, buttonScriptPathType);
            string powershellArguments = GetPowerShellArguments(config.powershell_arguments);
            int ownerTab = MainTab.SelectedIndex;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                try
                {
                    Process.Start("explorer.exe", GetScriptPath(RemoveScriptFileFromPath(buttonScript), buttonScriptPathType));
                    AddTextToEventsList("Opened script location folder: " + GetScriptPath(RemoveScriptFileFromPath(buttonScript), buttonScriptPathType), false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    AddTextToEventsList("Could not open script location folder: " + ex.Message, false);
                }
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                try
                {
                    Process.Start("explorer.exe", scriptPath);
                    AddTextToEventsList("Opened script : " + scriptPath, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    AddTextToEventsList("Could not open script: " + ex.Message, false);
                }
                return;
            }

            List<Answer> buttonArguments = ((ActionButton)actionButton.DataContext).ButtonArguments;
            AddTextToConsole("Start script: " + scriptPath + "<br><span style=\"color:yellow;\">=====================================================================</span><br>", ownerTab);
            AddTextToEventsList("Execution of " + scriptPath + " has been started.", false);

            if (buttonArguments.Count == 0)
            {
                AddTextToEventsList("Starting script " + scriptPath, false);
                if (buttonScriptType.ToLower() == "powershell")
                {
                    await RunProcessAsync(config.default_powershell_path, powershellArguments + "-File \"" + scriptPath + "\"", ownerTab, buttonScript);
                }
                else
                {
                    await RunProcessAsync(config.default_cmd_path, "/c \"" + scriptPath + "\"", ownerTab, buttonScript);
                }
            }
            else
            {
                AnswerDialog dialog = new AnswerDialog(ConvertArgumentsToAnswers(buttonArguments));
                if (dialog.ShowDialog() == true)
                {
                    AddTextToEventsList("Starting script" + scriptPath, false);
                    if (buttonScriptType.ToLower() == "powershell")
                    {
                        await RunProcessAsync(config.default_powershell_path, powershellArguments + "-File \"" + scriptPath + "\" " + ConvertArgumentsToPowerShell(dialog.answerData.Answers), ownerTab, buttonScript);
                    }
                    else
                    {
                        await RunProcessAsync(config.default_cmd_path, powershellArguments + "/c \"" + scriptPath + "\" " + ConvertArgumentsToCMD(dialog.answerData.Answers), ownerTab, buttonScript);
                    }
                }
                else
                {
                    AddTextToEventsList("Task cancelled by user", false);
                    AddTextToConsole("Task cancelled by user!", ownerTab);
                }
            }
        }

        public async Task<int> RunProcessAsync(string FileName, string Args, int OwnerTab, string ScriptName)
        {
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = FileName, Arguments = Args,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                },
                EnableRaisingEvents = true
            })
            {
                return await RunProcessAsync(process, OwnerTab, ScriptName).ConfigureAwait(false);
            }
        }

        private Task<int> RunProcessAsync(Process process, int OwnerTab, string ScriptName)
        {
            var tcs = new TaskCompletionSource<int>();

            // Process Exited
            process.Exited += (s, ea) =>
            {
                RemoveTaskFromTasksList(process.Id, true);
                tcs.SetResult(process.ExitCode);
                AddTextToConsole("<br><span style=\"color:yellow;\">Task finished!</span><br>", OwnerTab);
                AddTextToEventsList("Task " + process.StartInfo.Arguments.Replace("-File ", "") + " finished", true);
                ScrollToBottomListBox(EventsList, true);
            };

            // Process Output data received
            process.OutputDataReceived += (s, ea) => {
                AddTextToConsole(ea.Data, OwnerTab); 
            };

            // Process Error output received
            process.ErrorDataReceived += (s, ea) => {

                if (ea.Data != null)
                {
                    if (ea.Data != "")
                    {
                        AddTextToConsole("<span style=\"color:red;\">Error: " + ea.Data + "</span>", OwnerTab);
                        AddTextToEventsList("Task " + process.StartInfo.Arguments.Replace("-File ", "") + " failed", true);
                    }
                }
            };

            // Start the process
            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Could not start process: " + process);
            }

            // Add to tasks list
            AddTaskToTasksList(process.Id, ScriptName.Split("\\")[ScriptName.Split("\\").Length - 1]);

            // Begin to read data from the output
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        private void TasksListStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button buttonStop = sender as Button;
                int processID = ((TaskListTask)buttonStop.DataContext).TaskID;
                Process.GetProcessById(processID).Kill();
                AddTextToEventsList("Process has been cancelled by user!", false);
                RemoveTaskFromTasksList(processID, false);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AddTextToEventsList("Process cancelled failed: " + ex.Message, false);
            }
        }

        private void ScrollToTopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RichTextEditor console = FindVisualChild<RichTextEditor>(MainTab);
                console.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                var scrollViewElement = console.Parent;
                ScrollViewer scrollView = scrollViewElement as ScrollViewer;
                scrollView.ScrollToTop();
            }
            catch { }
        }

        private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RichTextEditor console = FindVisualChild<RichTextEditor>(MainTab);
                console.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last));
                var scrollViewElement = console.Parent;
                ScrollViewer scrollView = scrollViewElement as ScrollViewer;
                scrollView.ScrollToBottom();
            }
            catch { }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int childCount = 0; childCount < VisualTreeHelper.GetChildrenCount(parent); childCount++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, childCount);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }


        #region MenuItems
        private void ReloadConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                if (config.clear_events_when_reload == true)
                {
                    EventsList.Items.Clear();
                }
                else 
                {
                    AddTextToEventsList("Relading config!", false);
                }

                MainTab.ItemsSource = null;
                LoadConfig();

                if(MainTab.Items.Count > 0)
                {
                    MainTab.SelectedIndex = 0;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AddTextToEventsList("Relading config failed: " + ex.Message, false);
            }
        }
        private void OpenAppFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                AddTextToEventsList("Opened application running folder", false);
            }
        }

        private void ClearEventsLogListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            EventsList.Items.Clear();
        }

        private void ReorderTabsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ReorderTabsDialog rtd = new ReorderTabsDialog(config);
            rtd.ShowDialog();
            if (rtd.changesOccured)
            {
                LoadConfig();

                MainTab.Items.Refresh();
                this.UpdateLayout();
                AddTextToEventsList("Reorder tabs dialog ended!", false);
            }
            else
            {
                AddTextToEventsList("Reorder tabs dialog ended. No changes occured!", false);
            }
        }

        private void ConfigurationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationDialog cfg = new ConfigurationDialog(config);
            cfg.ShowDialog();

            LoadConfig();
            MainTab.Items.Refresh();
            MainMenuItemsVisibility();
            this.UpdateLayout();
            AddTextToEventsList("Configuration ended!", false);
        }

        private void AddTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowAddNewTabDialog();
        }

        private void RemoveCurrentTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to delete current tab?", "Please confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                List<TabData> newSourceTabs = new List<TabData>();
                foreach (TabData tab in MainTab.Items)
                {
                    TabData currentTab = MainTab.Items[MainTab.SelectedIndex] as TabData;
                    if (tab != currentTab)
                    {
                        newSourceTabs.Add(tab);
                    }
                }

                MainTab.ItemsSource = null;
                MainTab.ItemsSource = newSourceTabs;

                if (SaveConfig())
                {
                    MainTab.Items.Refresh();
                    this.UpdateLayout();
                }
            }
        }

        private void RenameCurrentTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TabData tab = (TabData)MainTab.Items[MainTab.SelectedIndex];
            ShowRenameTabDialog(MainTab.SelectedIndex, tab.TabHeader);
        }

        private void AddButtonToCurrentTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowAddButtonDialog();
        }

        private void ReorderButtonsInCurrentTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowReorderActionButtonsDialog();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(tasksList.Count > 0)
            {
                if(MessageBox.Show("Task is still going. Do you want to exit? You will have to kill process manually if you exit", "Please confirm", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog aboutDialog = new AboutDialog();
            aboutDialog.ShowDialog();
        }

        private void MenuTroubleshooting_Click(object sender, RoutedEventArgs e)
        {
            TroubleshootingWindow troubleshootingDialog = new TroubleshootingWindow(config);
            troubleshootingDialog.Show();
        }

        private void MenuColorTags_Click(object sender, RoutedEventArgs e)
        {
            ColorTagsDialog colorTagsDialog = new ColorTagsDialog();
            colorTagsDialog.Show();
        }


        #endregion

        #region ContextMenuItems

        public void ActionButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                selectedActionButton = ((Button)e.Source).DataContext as ActionButton;
                ContextMenu cm = this.FindResource("OnActionButtonContextMenu") as ContextMenu;

                if (e.Source is not ScrollViewer && e.OriginalSource is TextBlock)
                {
                    if (config.restrictions.block_buttons_edit == true && config.restrictions.block_buttons_remove == true && config.restrictions.block_buttons_copy == true)
                    {
                        return;
                    }
                    else
                    {
                        if (config.restrictions.block_buttons_edit == false)
                        {
                            (cm.Items[0] as MenuItem).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (cm.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
                        }

                        if (config.restrictions.block_buttons_remove == false)
                        {
                            (cm.Items[1] as MenuItem).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (cm.Items[1] as MenuItem).Visibility = Visibility.Collapsed;
                        }

                        if (config.restrictions.block_buttons_copy == false)
                        {
                            (cm.Items[2] as MenuItem).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (cm.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                        }
                    }

                    cm.PlacementTarget = sender as Button;
                    cm.IsOpen = true;
                }
            }
        }

        public void OnActionButtonPannel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                ContextMenu cm = this.FindResource("OnActionButtonPannelContextMenu") as ContextMenu;
                if (e.Source is ScrollViewer && e.OriginalSource is not TextBlock)
                {
                    if (config.restrictions.block_buttons_add == true && config.restrictions.block_buttons_reorder == true && config.restrictions.block_buttons_paste == true)
                    {
                        return;
                    }
                    else
                    {
                        if (config.restrictions.block_buttons_add == false)
                        {
                            (cm.Items[0] as MenuItem).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (cm.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
                        }

                        if (config.restrictions.block_buttons_reorder == false)
                        {
                            (cm.Items[1] as MenuItem).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (cm.Items[1] as MenuItem).Visibility = Visibility.Collapsed;
                        }

                        if (config.restrictions.block_buttons_paste == false)
                        {
                            (cm.Items[2] as MenuItem).Visibility = Visibility.Visible;

                            if(actionButtonForCopy != null)
                            {
                                (cm.Items[2] as MenuItem).IsEnabled = true;
                            }
                            else
                            {
                                (cm.Items[2] as MenuItem).IsEnabled = false;
                            }
                        }
                        else
                        {
                            (cm.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                        }
                    }

                    cm.PlacementTarget = sender as ScrollViewer;
                    cm.IsOpen = true;
                }
            }
        }

        private void OnTabHeader_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                selectedTabItem = ((Label)e.Source).DataContext as TabData;
                ContextMenu cm = this.FindResource("OnTabContextMenu") as ContextMenu;

                if (config.restrictions.block_tabs_add == true && config.restrictions.block_tabs_remove == true && config.restrictions.block_tabs_rename == true)
                {
                    return;
                }
                else
                {
                    if (config.restrictions.block_tabs_add == false)
                    {
                        (cm.Items[0] as MenuItem).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        (cm.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
                    }

                    if (config.restrictions.block_tabs_remove == false)
                    {
                        (cm.Items[1] as MenuItem).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        (cm.Items[1] as MenuItem).Visibility = Visibility.Collapsed;
                    }

                    if (config.restrictions.block_tabs_rename == false)
                    {
                        (cm.Items[2] as MenuItem).Visibility = Visibility.Visible;
                    }
                    else
                    {
                        (cm.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                    }
                }

                cm.PlacementTarget = sender as Label;
                cm.IsOpen = true;
            }
        }

        private void ContextMenuRemoveTab_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTabItem == null)
            {
                MessageBox.Show("Selected Tab is still null. Please try again.");
                return;
            }

            List<TabData> newSourceTabs = new List<TabData>();
            foreach (TabData tab in MainTab.Items)
            {
                if (tab != selectedTabItem)
                {
                    newSourceTabs.Add(tab);
                }
            }

            MainTab.ItemsSource = null;
            MainTab.ItemsSource = newSourceTabs;

            if (SaveConfig())
            {
                MainTab.Items.Refresh();
                this.UpdateLayout();
            }
        }

        private void ContextMenuRenameTab_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTabItem == null)
            {
                MessageBox.Show("Selected Tab is still null. Please try again.");
                return;
            }

            int currentTabIndex = 0;

            List<TabData> newSourceTabs = new List<TabData>();
            foreach (TabData tab in MainTab.Items)
            {
                if (tab == selectedTabItem)
                {
                    ShowRenameTabDialog(currentTabIndex, selectedTabItem.TabHeader);
                    return;
                }
                else
                {
                    currentTabIndex = currentTabIndex + 1;
                }
            }
        }

        private void ContextMenuAddTab_Click(object sender, RoutedEventArgs e)
        {
            ShowAddNewTabDialog();
        }

        private void ContextMenuRemoveActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedActionButton == null)
            {
                MessageBox.Show("Selected Action button is still null. Please try again.");
                return;
            }

            for (int i = 0; i < MainTab.Items.Count; i++)
            {
                if (MainTab.Items[i] is TabData button)
                {
                    var item = button.TabActionButtons.Where(x => x.Equals(selectedActionButton)).FirstOrDefault();
                    if (item != null)
                    {
                        button.TabActionButtons.Remove(item);
                    }
                }
            }

            if (SaveConfig())
            {
                MainTab.Items.Refresh();
                this.UpdateLayout();
            }
        }

        private void ContextMenuCopyActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedActionButton == null)
            {
                MessageBox.Show("Selected Action button is still null. Please try again.");
                return;
            }

            actionButtonForCopy = selectedActionButton;
        }

        private void ContextMenuEditActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedActionButton == null)
            {
                MessageBox.Show("Selected Action button is still null. Please try again.");
                return;
            }

            EditActionButtonDialog eabd = new EditActionButtonDialog(selectedActionButton);
            if(eabd.ShowDialog() == true)
            {
                for (int i = 0; i < MainTab.Items.Count; i++)
                {
                    if (MainTab.Items[i] is TabData button)
                    {
                        var item = button.TabActionButtons.Where(x => x.Equals(selectedActionButton)).FirstOrDefault();
                        if (item != null)
                        {
                            item = eabd.actionButton;
                        }
                    }
                }

                if (SaveConfig())
                {
                    MainTab.Items.Refresh();
                    this.UpdateLayout();
                }
            }
            else
            {
                AddTextToEventsList("Edit button cancelled by user", false);
            }
        }

        private void ContextMenuAddActionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAddButtonDialog();
        }

        private void ContextMenuReorderActionButtons_Click(object sender, RoutedEventArgs e)
        {
            ShowReorderActionButtonsDialog();
        }

        private void ContextMenuPasteActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (actionButtonForCopy == null)
            {
                return;
            }

            TabData tab = (TabData) MainTab.Items[MainTab.SelectedIndex];
            actionButtonForCopy.ID = Guid.NewGuid();
            tab.TabActionButtons.Add(actionButtonForCopy);

            if (SaveConfig())
            {
                MainTab.Items.Refresh();
                this.UpdateLayout();
            }
        }

        #endregion

    }
}
