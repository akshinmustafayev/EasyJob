using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace EasyJob.Utils
{
    public class CommonUtils
    {
        public static void OpenLinkInBrowser(string url)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = url;
                proc.Start();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public static string ReadAssemblyFile(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(EasyJob)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string ApplyConsoleColorTagsToText(string Text)
        {
            string[][] colorCodes = new string[][] { 
                new string[] { @"\c01EJ", "/c01EJ", "<span style=\"color: #DFFF00;\">" },
                new string[] { @"\c02EJ", "/c02EJ", "<span style=\"color: #FFBF00;\">" },
                new string[] { @"\c03EJ", "/c03EJ", "<span style=\"color: #FF7F50;\">" },
                new string[] { @"\c04EJ", "/c04EJ", "<span style=\"color: #DE3163;\">" },
                new string[] { @"\c05EJ", "/c05EJ", "<span style=\"color: #9FE2BF;\">" },
                new string[] { @"\c06EJ", "/c06EJ", "<span style=\"color: #40E0D0;\">" },
                new string[] { @"\c07EJ", "/c07EJ", "<span style=\"color: #6495ED;\">" },
                new string[] { @"\c08EJ", "/c08EJ", "<span style=\"color: #CCCCFF;\">" },
                new string[] { @"\c09EJ", "/c09EJ", "<span style=\"color: #800080;\">" },
                new string[] { @"\c10EJ", "/c10EJ", "<span style=\"color: #3D3D6B;\">" },
                new string[] { @"\c11EJ", "/c11EJ", "<span style=\"color: #ADD45C;\">" },
                new string[] { @"\c12EJ", "/c12EJ", "<span style=\"color: #57C785;\">" },
                new string[] { @"\c13EJ", "/c13EJ", "<span style=\"color: #00BAAD;\">" },
                new string[] { @"\c14EJ", "/c14EJ", "<span style=\"color: #2A7B9B;\">" }
            };

            for (int i = 0; i <= colorCodes.GetLength(0) - 1; i++)
            {
                if (Text.Contains(colorCodes[i][0]) == true && Text.Contains(colorCodes[i][1]) == false)
                {
                    Text = Text.Replace(colorCodes[i][0], colorCodes[i][2]);
                    Text = Text + "</span>";
                }
                else if (Text.Contains(colorCodes[i][0]) == false && Text.Contains(colorCodes[i][1]) == true)
                {
                    Text = Text.Replace(colorCodes[i][1], "");
                }
                else if (Text.Contains(colorCodes[i][0]) == true && Text.Contains(colorCodes[i][1]) == true)
                {
                    Text = Text.Replace(colorCodes[i][0], colorCodes[i][2]);
                    Text = Text.Replace(colorCodes[i][1], "</span>");
                }
            }

            return Text;
        }

        public static string ConvertPartToRelative(string Path)
        {
            Path = Path.Replace(AppDomain.CurrentDomain.BaseDirectory, "");
            return Path;
        }

        public static string ApplicationStartupPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
