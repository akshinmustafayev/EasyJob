using System.Collections.Generic;
using EasyJob.Serialization.AnswerDialog;

namespace EasyJob.TabItems
{
    public class ActionButton
    {
        public string ButtonText { get; set; }
        public string ButtonDescription { get; set; }
        public string ButtonScript { get; set; }
        public string ButtonScriptPathType { get; set; }
        public List<Answer> ButtonArguments { get; set; }
    }
}
