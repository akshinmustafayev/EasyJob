using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.IO;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Data;

namespace WpfRichText
{
	/// <summary></summary>
	public static class RichTextBoxAssistant
    {
		/// <summary></summary>
		public static readonly DependencyProperty BoundDocument =
           DependencyProperty.RegisterAttached("BoundDocument", typeof(string), typeof(RichTextBoxAssistant),
           new FrameworkPropertyMetadata(null,
               FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
               OnBoundDocumentChanged)
               );

        private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox box = d as RichTextBox;

            if (box == null)
                return;

            RemoveEventHandler(box);

            string newXAML = GetBoundDocument(d);

            box.Document.Blocks.Clear();

            if (!string.IsNullOrEmpty(newXAML))
            {
                using (MemoryStream xamlMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(newXAML)))
                {
                    ParserContext parser = new ParserContext();
                    parser.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                    parser.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                    //FlowDocument doc = new FlowDocument();
                    Section section = XamlReader.Load(xamlMemoryStream, parser) as Section;

                    box.Document.Blocks.Add(section);

                }
            }

            AttachEventHandler(box);

        }

        private static void RemoveEventHandler(RichTextBox box)
        {
            Binding binding = BindingOperations.GetBinding(box, BoundDocument);

            if (binding != null)
            {
                if (binding.UpdateSourceTrigger == UpdateSourceTrigger.Default ||
                    binding.UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
                {

                    box.LostFocus -= HandleLostFocus;
                }
                else
                {
                    box.TextChanged -= HandleTextChanged;
                }
            }
        }

        private static void AttachEventHandler(RichTextBox box)
        {
            Binding binding = BindingOperations.GetBinding(box, BoundDocument);

            if (binding != null)
            {
                if (binding.UpdateSourceTrigger == UpdateSourceTrigger.Default ||
                    binding.UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
                {

                    box.LostFocus += HandleLostFocus;
                }
                else
                {
                    box.TextChanged += HandleTextChanged;
                }
            }
        }

        private static void HandleLostFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox box = sender as RichTextBox;

            TextRange tr = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, DataFormats.Xaml);
				string xamlText = Encoding.UTF8.GetString(ms.ToArray());
                SetBoundDocument(box, xamlText);
            }
        }

        private static void HandleTextChanged(object sender, RoutedEventArgs e)
        {
            // TODO: TextChanged is currently not working!
            RichTextBox box = sender as RichTextBox;

            TextRange tr = new TextRange(box.Document.ContentStart,
                               box.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, DataFormats.Xaml);
				string xamlText = Encoding.UTF8.GetString(ms.ToArray());
                SetBoundDocument(box, xamlText);
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dependencyObject"></param>
		/// <returns></returns>
        public static string GetBoundDocument(DependencyObject dependencyObject)
        {
			if (dependencyObject != null)
			{
				var html = dependencyObject.GetValue(BoundDocument) as string;
				var xaml = string.Empty;

				if (!string.IsNullOrEmpty(html))
					xaml = HtmlToXamlConverter.ConvertHtmlToXaml(html, false);

				return xaml;
			}
			return string.Empty;
        }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dependencyObject"></param>
		/// <param name="value"></param>
		public static void SetBoundDocument(DependencyObject dependencyObject, string value)
        {
			if (dependencyObject != null)
			{
				var xaml = value;
				var html = HtmlFromXamlConverter.ConvertXamlToHtml(xaml, false);
				dependencyObject.SetValue(BoundDocument, html);
			}
        }
    }

 
}