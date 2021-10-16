using EasyJob.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml;
using System.Xml.Linq;

namespace EasyJob.Windows
{
    /// <summary>
    /// Interaction logic for HelpDialog.xaml
    /// </summary>
    public partial class HelpDialog : Window
    {
        public HelpDialog(string helpitem)
        {
            InitializeComponent();
            LoadXml(helpitem);
        }

        private void LoadXml(string helpitem)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(CommonUtils.ReadAssemblyFile(@"EasyJob.Documentation.HelpDocumentation.xml"));

            XmlNodeList nodeList = doc.SelectNodes("/items/item[name='" + helpitem + "']");

            HelpHeading.Text = nodeList[0]["heading"].InnerText;
            HelpUsed.Text = nodeList[0]["used"].InnerText;
            HelpDescription.Text = nodeList[0]["description"].InnerText;
        }
    }
}
