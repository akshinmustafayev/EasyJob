namespace EasyJob.Serialization
{
    public class ConfigArgument
    {
        private string _argument_question;
        private string _argument_answer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigArgument"/> class.
        /// </summary>
        /// <param name="argument_question">The argument question.</param>
        /// <param name="argument_answer">The argument answer.</param>
        public ConfigArgument(string argument_question, string argument_answer)
        {
            this._argument_question = argument_question;
            this._argument_answer = argument_answer;
        }

        /// <summary>
        /// Gets or sets the argument question.
        /// </summary>
        /// <value>
        /// The argument question.
        /// </value>
        public string ArgumentQuestion { get => _argument_question; set => _argument_question = value; }
        /// <summary>
        /// Gets or sets the argument answer.
        /// </summary>
        /// <value>
        /// The argument answer.
        /// </value>
        public string ArgumentAnswer { get => _argument_answer; set => _argument_answer = value; }
    }
}
