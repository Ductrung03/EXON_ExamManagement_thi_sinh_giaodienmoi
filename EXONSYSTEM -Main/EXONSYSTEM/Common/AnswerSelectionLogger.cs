using System;
using System.Collections.Generic;

namespace EXONSYSTEM.Common
{
    public class AnswerSelectionLogEntry
    {
        public DateTime SelectedAt { get; set; }
        public string SelectedAtMsText { get; set; }
        public int QuestionNo { get; set; }
        public string AnswerLabel { get; set; }
    }

    public static class AnswerSelectionLogger
    {
        public static readonly List<AnswerSelectionLogEntry> Logs = new List<AnswerSelectionLogEntry>();
        private static DateTime _sessionStart = DateTime.Now;

        public static void Init(DateTime sessionStart)
        {
            _sessionStart = sessionStart;
            Logs.Clear();
        }

        public static void Log(int questionNo, string answerLabel)
        {
            DateTime selectedAt;
            try
            {
                selectedAt = DAO.DAO.ConvertDateTime.GetDateTimeServer();
            }
            catch
            {
                selectedAt = _sessionStart.AddMilliseconds(Environment.TickCount & Int32.MaxValue);
                if (selectedAt < _sessionStart)
                {
                    selectedAt = DateTime.Now;
                }
            }

            Logs.Add(new AnswerSelectionLogEntry
            {
                SelectedAt = selectedAt,
                SelectedAtMsText = selectedAt.ToString("dd-MM-yyyy HH:mm:ss.fff"),
                QuestionNo = questionNo,
                AnswerLabel = NormalizeAnswer(answerLabel)
            });
        }

        private static string NormalizeAnswer(string answerLabel)
        {
            if (string.IsNullOrWhiteSpace(answerLabel))
            {
                return string.Empty;
            }

            return answerLabel.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        }

        public static void WriteToFile(string contestantCode, int contestantShiftID)
        {
            try
            {
                string folder = @"C:\ProgramData\EXON\Logs";
                System.IO.Directory.CreateDirectory(folder);
                string path = System.IO.Path.Combine(folder, string.Format("{0}_{1}_actions.log", contestantCode, contestantShiftID));
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine(string.Format("{0,-28} {1,-6} {2,-8} {3}", "Time", "Q", "Action", "Answer"));
                sb.AppendLine(new string('-', 60));
                for (int i = 0; i < Logs.Count; i++)
                {
                    AnswerSelectionLogEntry log = Logs[i];
                    string action = (i > 0 && Logs[i - 1].QuestionNo == log.QuestionNo) ? "CHANGE" : "SELECT";
                    sb.AppendLine(string.Format("{0,-28} Q{1,-5} {2,-8} {3}", log.SelectedAtMsText, log.QuestionNo, action, log.AnswerLabel));
                }
                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch { }
        }
    }
}
