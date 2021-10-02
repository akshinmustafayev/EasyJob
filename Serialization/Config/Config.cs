using System.Collections.Generic;

namespace EasyJob.Serialization
{
    public class Config
    {
        public string default_powershell_path { get; set; }
        public string console_background { get; set; }
        public string console_foreground { get; set; }
        public bool clear_events_when_reload { get; set; }
        public List<ConfigTab> tabs { get; set; }
    }
}
