using EasyJob.Serialization;
using EasyJob.Serialization.AnswerDialog;
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
    /// Interaction logic for AddActionButtonDialog.xaml
    /// </summary>
    public partial class AddActionButtonDialog : Window
    {
        public ConfigButton configButton;
        public AddActionButtonDialog()
        {
            InitializeComponent();
            configButton = null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            List<ConfigArgument> configArguments = new List<ConfigArgument>();
            foreach (Answer answer in ButtonScriptArguments.Items) { configArguments.Add(new ConfigArgument(answer.AnswerQuestion, answer.AnswerResult)); };
            string buttonScriptPathTypeValue = ConvertScriptPathTypeComboBoxToString(ButtonScriptPathType);
            string buttonScriptTypeValue = ConvertScriptTypeComboBoxToString(ButtonScriptType);
            ConfigButton newConfigButton = new ConfigButton(Guid.NewGuid() ,ButtonText.Text, ButtonDescription.Text, ButtonScript.Text, buttonScriptPathTypeValue, buttonScriptTypeValue, configArguments);
            configButton = newConfigButton;

            DialogResult = true;
        }
        
        private string ConvertScriptTypeComboBoxToString(ComboBox cb)
        {
            string result = "";
            if(cb.SelectedIndex == 0)
            {
                result = "powershell";
            }
            else
            {
                result = "bat";
            }
            return result;
        }

        private string ConvertScriptPathTypeComboBoxToString(ComboBox cb)
        {
            string result = "";
            if (cb.SelectedIndex == 0)
            {
                result = "relative";
            }
            else
            {
                result = "absolute";
            }
            return result;
        }

        private void CANCELButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            HelpDialog hd = new HelpDialog(button.Name);
            hd.ShowDialog();
        }
    }
}
