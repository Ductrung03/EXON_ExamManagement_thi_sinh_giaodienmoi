using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace EXONSYSTEM.Common
{
    public static class ExamAnswerHistoryStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        private static List<ExamAnswerHistoryEntry> _entries = new List<ExamAnswerHistoryEntry>();
        private static int _contestantShiftId = -1;

        public static void InitializeSession(int contestantShiftId, bool reset)
        {
            lock (SyncRoot)
            {
                _contestantShiftId = contestantShiftId;
                _entries = LoadEntries(contestantShiftId);
                if (reset)
                {
                    _entries = new List<ExamAnswerHistoryEntry>();
                    SaveEntries(contestantShiftId, _entries);
                }
            }
        }

        public static void AddEntry(int questionNo, string answerText, DateTime selectedAt)
        {
            lock (SyncRoot)
            {
                if (_contestantShiftId <= 0)
                {
                    return;
                }

                var entry = new ExamAnswerHistoryEntry
                {
                    ContestantShiftID = _contestantShiftId,
                    QuestionNo = questionNo,
                    AnswerText = NormalizeAnswerText(answerText),
                    SelectedAt = selectedAt
                };

                _entries.Add(entry);
                SaveEntries(_contestantShiftId, _entries);
            }
        }

        public static List<ExamAnswerHistoryEntry> GetEntries(int contestantShiftId)
        {
            lock (SyncRoot)
            {
                if (_contestantShiftId != contestantShiftId)
                {
                    _contestantShiftId = contestantShiftId;
                    _entries = LoadEntries(contestantShiftId);
                }

                return _entries
                    .OrderBy(x => x.QuestionNo)
                    .ThenBy(x => x.SelectedAt)
                    .ToList();
            }
        }

        private static string NormalizeAnswerText(string answerText)
        {
            if (string.IsNullOrWhiteSpace(answerText))
            {
                return "(trong)";
            }

            var normalized = answerText.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
            while (normalized.Contains("  "))
            {
                normalized = normalized.Replace("  ", " ");
            }

            return normalized;
        }

        private static List<ExamAnswerHistoryEntry> LoadEntries(int contestantShiftId)
        {
            var path = GetStoragePath(contestantShiftId);
            if (!File.Exists(path))
            {
                return new List<ExamAnswerHistoryEntry>();
            }

            try
            {
                var json = File.ReadAllText(path);
                var entries = Serializer.Deserialize<List<ExamAnswerHistoryEntry>>(json);
                return entries ?? new List<ExamAnswerHistoryEntry>();
            }
            catch
            {
                return new List<ExamAnswerHistoryEntry>();
            }
        }

        private static void SaveEntries(int contestantShiftId, List<ExamAnswerHistoryEntry> entries)
        {
            var path = GetStoragePath(contestantShiftId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = Serializer.Serialize(entries);
            File.WriteAllText(path, json);
        }

        private static string GetStoragePath(int contestantShiftId)
        {
            return Path.Combine(Constant.PATH_EXON, "AnswerHistory", contestantShiftId + ".json");
        }
    }
}
