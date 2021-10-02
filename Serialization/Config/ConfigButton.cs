using System.Collections.Generic;

namespace EasyJob.Serialization
{
    public class ConfigButton
    {
        public string text { get; set; }
        public string description { get; set; }
        public string script { get; set; }
        public string scriptpathtype { get; set; }
        public List<ConfigArgument> arguments { get; set; }
    }
}
