using EasyJob.Serialization.AnswerDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for AnswerDialog.xaml
    /// </summary>
    public partial class AnswerDialog : Window
    {
        public AnswerData answerData = null;
        public AnswerDialog(AnswerData _answerData)
        {
            InitializeComponent();
            answerData = _answerData;
            if (_answerData != null)
            {
                foreach (Answer answer in _answerData.Answers)
                {
                    answer.AnswerResult = "";
                }
                AnswerDialogItems.ItemsSource = _answerData.Answers;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            bool AllowConfirm = true;
            answerData.Answers = (List<Answer>)AnswerDialogItems.ItemsSource;
            foreach (Answer answer in answerData.Answers)
            {
                if(answer.AnswerResult == "" || answer.AnswerResult == null)
                {
                    AllowConfirm = false;
                }
            }
            if(AllowConfirm == false)
            {
                MessageBox.Show("Please provide value to all textboxes!");
                return;
            }
            DialogResult = true;
        }

        private void CANCELButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /*
        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }
        */
    }
}
