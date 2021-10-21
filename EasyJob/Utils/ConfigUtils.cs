using EasyJob.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EasyJob.Utils
{
    public class ConfigUtils
    {
        public static string path = AppDomain.CurrentDomain.BaseDirectory + "config.json";

        public static bool SaveFromConfigToFile(Config config)
        {
            try
            {
                string conf = JsonConvert.SerializeObject(config);
                File.WriteAllText(path, conf, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
    }
}
