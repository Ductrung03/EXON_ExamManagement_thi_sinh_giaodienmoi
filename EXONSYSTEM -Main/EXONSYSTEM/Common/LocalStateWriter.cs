using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EXONSYSTEM.Common
{
    public static class LocalStateWriter
    {
        public static void UpsertKeyValues(string path, Dictionary<string, string> updates)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    int idx = line.IndexOf('=');
                    if (idx <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1);
                    dict[key] = value;
                }
            }

            foreach (KeyValuePair<string, string> kv in updates)
            {
                dict[kv.Key] = kv.Value ?? string.Empty;
            }

            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> item in dict)
            {
                lines.Add(item.Key + "=" + item.Value);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
        }
    }
}
