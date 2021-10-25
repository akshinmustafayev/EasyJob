using EasyJobPSTools.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyJobPSTools
{
    public class Program
    {
        public static string ShowEJInputBoxWindow(string Header, string Text, bool AllowEmptyResult)
        {
            ShowEJInputBox sejib = new ShowEJInputBox(Header, Text, AllowEmptyResult);
            if (sejib.ShowDialog() == true)
            {
                return sejib.windowResult;
            }
            else
            {
                return "";
            }
        }

        public static string ShowEJSelectFileWindow()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                return ofd.FileName;
            }
            else
            {
                return "";
            }
        }

        public static string ShowEJSelectFileWindow(string fileType)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = fileType;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                return ofd.FileName;
            }
            else
            {
                return "";
            }
        }

        public static string ShowEJSelectFolderWindow()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }
                else
                {
                    return "";
                }
            }
        }
    }
}
