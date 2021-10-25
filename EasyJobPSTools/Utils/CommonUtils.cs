using ModernWpf;
using ModernWpf.Controls;
using System.Windows;

namespace EasyJobPSTools.Utils
{
    public class CommonUtils
    {
        public static void FixModernWpfUI()
        {
            if (Application.Current is null)
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                var themeResources = new ThemeResources();
                themeResources.BeginInit();
                app.Resources.MergedDictionaries.Add(themeResources);
                themeResources.EndInit();

                app.Resources.MergedDictionaries.Add(new XamlControlsResources());
            }
        }
    }
}
