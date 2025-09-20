using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EXONSYSTEM.Common
{
    public class ConnectionEvent
    {
        public string TimeText { get; set; }
        public string EventType { get; set; }
        public string Detail { get; set; }
    }

    public static class ConnectionLogger
    {
        public static readonly List<ConnectionEvent> Events = new List<ConnectionEvent>();

        public static void Log(string eventType, string detail)
        {
            string time;
            try { time = DAO.DAO.ConvertDateTime.GetDateTimeServer().ToString("dd-MM-yyyy HH:mm:ss.fff"); }
            catch { time = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff"); }

            Events.Add(new ConnectionEvent
            {
                TimeText = time,
                EventType = eventType,
                Detail = detail
            });
        }

        public static void Log(string eventType)
        {
            Log(eventType, string.Empty);
        }

        public static void WriteToFile(string contestantCode, int contestantShiftID)
        {
            try
            {
                string folder = @"C:\ProgramData\EXON\Logs";
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, string.Format("{0}_{1}_connection.log", contestantCode, contestantShiftID));
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("{0,-28} {1,-15} {2}", "Time", "Event", "Detail"));
                sb.AppendLine(new string('-', 70));
                foreach (ConnectionEvent e in Events)
                    sb.AppendLine(string.Format("{0,-28} {1,-15} {2}", e.TimeText, e.EventType, e.Detail));
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static void Clear()
        {
            Events.Clear();
        }
    }
}
