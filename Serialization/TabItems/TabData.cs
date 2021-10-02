using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace EasyJob.TabItems
{
    public class TabData : INotifyPropertyChanged
    {
        private string _TabTextBoxText;
        public event PropertyChangedEventHandler PropertyChanged;

        public string TabHeader { get; set; }
        public string ConsoleBackground { get; set; }
        public string ConsoleForeground { get; set; }
        public List<ActionButton> TabActionButtons { get; set; }
        public string TabTextBoxText {
            get
            {
                return this._TabTextBoxText;
            }
            set
            {
                if (_TabTextBoxText != value)
                {
                    _TabTextBoxText = value;
                    Task.Run(() => {
                        OnChange("TabTextBoxText");
                    });
                }
            }
        }

        protected void OnChange(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
