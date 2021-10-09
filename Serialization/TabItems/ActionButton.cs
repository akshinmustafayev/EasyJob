using System;
using System.Collections.Generic;
using System.Windows.Controls;
using EasyJob.Serialization.AnswerDialog;

namespace EasyJob.TabItems
{
    public class ActionButton
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public Guid ID { get; set; }

        /// <summary>
        /// Gets or sets the button text.
        /// </summary>
        /// <value>
        /// The button text.
        /// </value>
        public string ButtonText { get; set; }
        
        /// <summary>
        /// Gets or sets the button description.
        /// </summary>
        /// <value>
        /// The button description.
        /// </value>
        public string ButtonDescription { get; set; }
        
        /// <summary>
        /// Gets or sets the button script.
        /// </summary>
        /// <value>
        /// The button script.
        /// </value>
        public string ButtonScript { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the button script path.
        /// </summary>
        /// <value>
        /// The type of the button script path.
        /// </value>
        public string ButtonScriptPathType { get; set; }

        /// <summary>
        /// Gets or sets the type of the button script.
        /// </summary>
        /// <value>
        /// The type of the button script.
        /// </value>
        public string ButtonScriptType { get; set; }

        /// <summary>
        /// Gets or sets the button arguments.
        /// </summary>
        /// <value>
        /// The button arguments.
        /// </value>
        public List<Answer> ButtonArguments { get; set; }
        
        /// <summary>
        /// Gets or sets the context menu.
        /// </summary>
        /// <value>
        /// The context menu.
        /// </value>
        public ContextMenu contextMenu { get; set; }
    }
}
