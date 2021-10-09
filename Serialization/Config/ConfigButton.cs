using System.Collections.Generic;

namespace EasyJob.Serialization
{
    public class ConfigButton
    {
        private string _text;
        private string _description;
        private string _script;
        private string _scriptpathtype;
        private string _scripttype;
        private List<ConfigArgument> _arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigButton"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="description">The description.</param>
        /// <param name="script">The script.</param>
        /// <param name="scriptpathtype">The scriptpathtype.</param>
        /// <param name="scripttype">The scripttype.</param>
        /// <param name="arguments">The arguments.</param>
        public ConfigButton(string text, string description, string script, string scriptpathtype, string scripttype, List<ConfigArgument> arguments)
        {
            this.Text = text;
            this._description = description;
            this._script = script;
            this._scriptpathtype = scriptpathtype;
            this._scripttype = scripttype;
            this._arguments = arguments;
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get => _text; set => _text = value; }
        
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get => _description; set => _description = value; }
        
        /// <summary>
        /// Gets or sets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        public string Script { get => _script; set => _script = value; }
        
        /// <summary>
        /// Gets or sets the type of the script path.
        /// </summary>
        /// <value>
        /// The type of the script path.
        /// </value>
        public string ScriptPathType { get => _scriptpathtype; set => _scriptpathtype = value; }
        
        /// <summary>
        /// Gets or sets the type of the script.
        /// </summary>
        /// <value>
        /// The type of the script.
        /// </value>
        public string ScriptType { get => _scripttype; set => _scripttype = value; }
        
        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        /// <value>
        /// The arguments.
        /// </value>
        public List<ConfigArgument> Arguments { get => _arguments; set => _arguments = value; }
    }
}
