using System.Collections.Generic;

namespace EasyJob.Serialization
{
    public class ConfigTab
    {
        private string _header;
        private List<ConfigButton> _buttons;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigTab"/> class.
        /// </summary>
        /// <param name="header">The header.</param>
        /// <param name="buttons">The buttons.</param>
        public ConfigTab(string header, List<ConfigButton> buttons)
        {
            this._header = header;
            this._buttons = buttons;
        }

        /// <summary>
        /// Gets or sets the header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        public string Header { get => _header; set => _header = value; }
        /// <summary>
        /// Gets or sets the buttons.
        /// </summary>
        /// <value>
        /// The buttons.
        /// </value>
        public List<ConfigButton> Buttons { get => _buttons; set => _buttons = value; }
    }
}
