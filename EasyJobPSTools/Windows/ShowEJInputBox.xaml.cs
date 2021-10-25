using EasyJobPSTools.Utils;
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

namespace EasyJobPSTools.Windows
{
    /// <summary>
    /// Interaction logic for ShowEJInputBox.xaml
    /// </summary>
    public partial class ShowEJInputBox : Window
    {
        private string _Header;
        private string _Text;
        private bool _AllowEmptyResult;
        public string windowResult = "";

        public ShowEJInputBox(string Header, string Text, bool AllowEmptyResult)
        {
            CommonUtils.FixModernWpfUI();
            InitializeComponent();

            FormError.Visibility = Visibility.Collapsed;

            _Header = Header;
            _Text = Text;
            _AllowEmptyResult = AllowEmptyResult;

            this.Title = Header;
            FormText.Text = Text;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (FormAnswer.Text.Length == 0 && _AllowEmptyResult == false)
            {
                FormError.Text = "Please input value";
                FormError.Visibility = Visibility.Visible;
            }
            else
            {
                windowResult = FormAnswer.Text;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void FormAnswer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_AllowEmptyResult == false)
            {
                if(FormAnswer.Text.Length > 0 && FormError.Visibility == Visibility.Visible)
                {
                    FormError.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
