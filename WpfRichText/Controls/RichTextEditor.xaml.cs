using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfRichText
{
    /// <summary>
    /// Interaction logic for BindableRichTextbox.xaml
    /// </summary>
    public partial class RichTextEditor : UserControl
    {
		/// <summary></summary>
		public static readonly DependencyProperty TextProperty =
		  DependencyProperty.Register("Text", typeof(string), typeof(RichTextEditor),
		  new PropertyMetadata(string.Empty));

		/// <summary></summary>
		public RichTextEditor()
        {
            InitializeComponent();
		}

		/// <summary></summary>
		public string Text
		{
			get { return GetValue(TextProperty) as string; }
			set
			{
				SetValue(TextProperty, value);
			}
		}
	}
}
