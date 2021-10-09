using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace EasyJob.TabItems
{
    public class TabData : INotifyPropertyChanged
    {
        private string _TabTextBoxText;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabData"/> class.
        /// </summary>
        public TabData()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabData"/> class.
        /// </summary>
        /// <param name="tabTextBoxText">The tab text box text.</param>
        public TabData(string tabTextBoxText)
        {
            this._TabTextBoxText = tabTextBoxText;
            ID = Guid.NewGuid();
            TabHeader = tabTextBoxText;
            TabActionButtons = new List<ActionButton>();
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public Guid ID { get; set; }

        /// <summary>
        /// Gets or sets the tab header.
        /// </summary>
        /// <value>
        /// The tab header.
        /// </value>
        public string TabHeader { get; set; }
        /// <summary>
        /// Gets or sets the console background.
        /// </summary>
        /// <value>
        /// The console background.
        /// </value>
        public string ConsoleBackground { get; set; }
        /// <summary>
        /// Gets or sets the console foreground.
        /// </summary>
        /// <value>
        /// The console foreground.
        /// </value>
        public string ConsoleForeground { get; set; }
        /// <summary>
        /// Gets or sets the tab action buttons.
        /// </summary>
        /// <value>
        /// The tab action buttons.
        /// </value>
        public List<ActionButton> TabActionButtons { get; set; }
        /// <summary>
        /// Gets or sets the tab text box text.
        /// </summary>
        /// <value>
        /// The tab text box text.
        /// </value>
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

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Called when [change].
        /// </summary>
        /// <param name="info">The information.</param>
        protected void OnChange(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
