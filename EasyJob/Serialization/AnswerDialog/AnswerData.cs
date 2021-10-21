using System.Collections.Generic;

namespace EasyJob.Serialization.AnswerDialog
{
    public class AnswerData
    {
        private List<Answer> _Answers = new List<Answer>();

        public List<Answer> Answers
        {
            get { return _Answers; }
            set { _Answers = value; }
        }
    }
}
