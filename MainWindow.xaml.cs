using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using Newtonsoft.Json;

namespace EasyJob
{
    public partial class MainWindow : Window
    {
        public string configJson = "";
        public Config config;
        ObservableCollection<TaskListTask> tasksList = new ObservableCollection<TaskListTask>();

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        public void LoadConfig()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                try { 
                    configJson = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                    config = JsonConvert.DeserializeObject<Config>(configJson);
                    List<TabData> tabs = new List<TabData>();
                    
                    foreach (ConfigTab configTab in config.tabs)
                    {
                        List<ActionButton> actionButtons = new List<ActionButton>();
                        
                        foreach (ConfigButton configButton in configTab.buttons)
                        {
                            List<Answer> configArguments = new List<Answer>();
                            foreach(ConfigArgument configArgument in configButton.arguments)
                            {
                                configArguments.Add(new Answer { AnswerQuestion = configArgument.argument_question, AnswerResult = configArgument.argument_answer });
                            }
                            
                            actionButtons.Add(new ActionButton { ButtonText = configButton.text, ButtonDescription = configButton.description, ButtonScript = configButton.script, ButtonScriptPathType = configButton.scriptpathtype, ButtonArguments = configArguments });
                        }

                        tabs.Add(new TabData { TabHeader = configTab.header, ConsoleBackground = config.console_background, ConsoleForeground = config.console_foreground, TabActionButtons = actionButtons, TabTextBoxText = "" });
                    }

                    MainTab.ItemsSource = tabs;
                    AddTextToEventsList("Config loaded from file: " + AppDomain.CurrentDomain.BaseDirectory + "config.json", false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    AddTextToEventsList("Error occured while loading file: " + ex.Message, false);
                }
            }
            else
            {
                MessageBox.Show("File " + AppDomain.CurrentDomain.BaseDirectory + "config.json does not exist.");
            }
        }

        private void AddTextToConsole (string Text, int OwnerTab)
        {
            if(Text == "" || Text == null || Text == "Error: ")
            {
                return;
            }

            this.Dispatcher.Invoke(() =>
            {
                TabData td = (TabData)MainTab.Items[OwnerTab];
                td.TabTextBoxText = td.TabTextBoxText + Environment.NewLine + Text;
            });
        }

        private void AddTextToEventsList(string Text, bool IsAsync)
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
                });
            }
            else
            {
                EventsList.Items.Add(Text);
            }
        }

        private void ScrollToBottomListBox(ListBox listBox, bool IsAsync)
        {
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
            string scriptPath = GetScriptPath(buttonScript, buttonScriptPathType);
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
            AddTextToConsole("Start script: " + scriptPath + Environment.NewLine + "===============================================================" + Environment.NewLine, ownerTab);
            AddTextToEventsList("Execution of " + scriptPath + " has been started.", false);

            if (buttonArguments.Count == 0)
            {
                AddTextToEventsList("Starting script " + scriptPath, false);
                await RunProcessAsync(config.default_powershell_path, "-File \"" + scriptPath + "\"", ownerTab, buttonScript);
            }
            else
            {
                AnswerDialog dialog = new AnswerDialog(ConvertArgumentsToAnswers(buttonArguments));
                if (dialog.ShowDialog() == true)
                {
                    AddTextToEventsList("Starting script" + scriptPath, false);
                    await RunProcessAsync(config.default_powershell_path, "-File \"" + scriptPath + "\" " + ConvertArgumentsToPowerShell(dialog.answerData.Answers), ownerTab, buttonScript);
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
            process.Exited += (s, ea) => {
                RemoveTaskFromTasksList(process.Id, true);
                tcs.SetResult(process.ExitCode);
                AddTextToConsole(Environment.NewLine + "Task finished!" + Environment.NewLine, OwnerTab);
                AddTextToEventsList("Task " + process.StartInfo.Arguments.Replace("-File ","") + " finished", true);
                ScrollToBottomListBox(EventsList, true);
            };

            // Process Output data received
            process.OutputDataReceived += (s, ea) => {
                AddTextToConsole(ea.Data, OwnerTab); 
            };

            // Process Error output received
            process.ErrorDataReceived += (s, ea) => {
                AddTextToConsole("Error: " + ea.Data, OwnerTab); 
                if (ea.Data != "" || ea.Data != null) 
                { 
                    AddTextToEventsList("Task " + process.StartInfo.Arguments.Replace("-File ", "") + " failed", true); 
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
                MessageBox.Show(ex.Message);
                AddTextToEventsList("Process cancelled failed: " + ex.Message, false);
            }
        }

        private void ScrollToTopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox console = FindVisualChild<TextBox>(MainTab);
                console.ScrollToHome();
            }
            catch { }
        }

        private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox console = FindVisualChild<TextBox>(MainTab);
                console.ScrollToEnd();
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
            try {
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
                MessageBox.Show(ex.Message);
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
                MessageBox.Show(ex.Message); 
                AddTextToEventsList("Opened application running folder", false);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
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


        #endregion

    }
}
