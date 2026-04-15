using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EXONSYSTEM.Common
{
    public static class KeyValueFileReader
    {
        public static Dictionary<string, string> Read(string path)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(path))
            {
                return dict;
            }

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

            return dict;
        }

        public static string Get(Dictionary<string, string> dict, string key)
        {
            string value;
            return dict.TryGetValue(key, out value) ? value : null;
        }

        public static long? GetLong(Dictionary<string, string> dict, string key)
        {
            long value;
            return long.TryParse(Get(dict, key), out value) ? (long?)value : null;
        }
    }
}
