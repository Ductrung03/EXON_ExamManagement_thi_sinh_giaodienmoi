using System;

namespace EXONSYSTEM.Common
{
    public class ExamAnswerHistoryEntry
    {
        public int ContestantShiftID { get; set; }
        public int QuestionNo { get; set; }
        public string AnswerText { get; set; }
        public DateTime SelectedAt { get; set; }

        public string SelectedAtText
        {
            get { return SelectedAt.ToString("HH:mm:ss:fff"); }
        }
    }
}
