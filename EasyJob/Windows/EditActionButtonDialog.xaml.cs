using EasyJob.Serialization;
using EasyJob.Serialization.AnswerDialog;
using EasyJob.TabItems;
using EasyJob.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for EditButtonDialog.xaml
    /// </summary>
    public partial class EditActionButtonDialog : Window
    {
        public ActionButton actionButton = null;

        public EditActionButtonDialog(ActionButton _actionButton)
        {
            InitializeComponent();

            actionButton = _actionButton;
            ButtonText.Text = actionButton.ButtonText;
            ButtonDescription.Text = actionButton.ButtonDescription;
            ButtonScript.Text = actionButton.ButtonScript;
            if (actionButton.ButtonScriptPathType == "relative") { ButtonScriptPathType.SelectedIndex = 0; }
            else { ButtonScriptPathType.SelectedIndex = 1; }
            if (actionButton.ButtonScriptType == "powershell"){ ButtonScriptType.SelectedIndex = 0; }
            else { ButtonScriptType.SelectedIndex = 1; }
            List<Answer> answers = actionButton.ButtonArguments;
            foreach (Answer ans in answers)
            {
                ButtonScriptArguments.Items.Add(new Answer { AnswerQuestion = ans.AnswerQuestion, AnswerResult = ans.AnswerResult });
            }
        }

        private string ConvertScriptTypeComboBoxToString(ComboBox cb)
        {
            if (cb.SelectedIndex == 0)
            {
                return "powershell";
            }
            else
            {
                return "bat";
            }
        }

        private string ConvertScriptPathTypeComboBoxToString(ComboBox cb)
        {
            if (cb.SelectedIndex == 0)
            {
                return "relative";
            }
            else
            {
                return "absolute";
            }
        }

        private void ADDButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonScriptArguments.Items.Add(new Answer { AnswerQuestion = ButtonScriptArgumentText.Text, AnswerResult = "" });
                ButtonScriptArgumentText.Text = "";
            }
            catch { }
        }

        private void DeleteArgumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                ButtonScriptArguments.Items.Remove((Answer)btn.DataContext);
            }
            catch { }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            actionButton.ButtonText = ButtonText.Text;
            actionButton.ButtonDescription = ButtonDescription.Text;
            actionButton.ButtonScript = ButtonScript.Text;
            actionButton.ButtonScriptPathType = ConvertScriptPathTypeComboBoxToString(ButtonScriptPathType);
            actionButton.ButtonScriptType = ConvertScriptTypeComboBoxToString(ButtonScriptType);

            actionButton.ButtonArguments.Clear();
            foreach (Answer ans in ButtonScriptArguments.Items)
            {
                actionButton.ButtonArguments.Add(new Answer { AnswerQuestion = ans.AnswerQuestion, AnswerResult = ans.AnswerResult });
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HelpDialog hd = new HelpDialog(button.Name);
            hd.ShowDialog();
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.InitialDirectory = CommonUtils.ApplicationStartupPath();
            if (ofd.ShowDialog() == true)
            {
                if (ButtonScriptPathType.SelectedIndex == 0)
                {
                    ButtonScript.Text = CommonUtils.ConvertPartToRelative(ofd.FileName);
                }
                else
                {
                    ButtonScript.Text = ofd.FileName;
                }
            }
        }
    }
}
