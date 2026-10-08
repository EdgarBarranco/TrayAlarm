using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace TrayAlarm
{
    public class CsvRepository
    {
        private readonly string _filePath;

        public CsvRepository(string filePath)
        {
            _filePath = filePath;
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public List<AlarmItem> Load()
        {
            var list = new List<AlarmItem>();
            if (!File.Exists(_filePath))
            {
                return list;
            }

            // Retry read up to 5 times in case user editor has temporary lock
            string[] lines = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        var readLines = new List<string>();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            readLines.Add(line);
                        }
                        lines = readLines.ToArray();
                        break;
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(60);
                }
            }

            if (lines == null || lines.Length == 0)
            {
                return list;
            }

            bool isFirstLine = true;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#") || line.StartsWith("//")) continue;

                var columns = ParseCsvRow(line);
                if (columns.Count == 0) continue;

                // Check if this is a header line
                if (isFirstLine && IsHeaderRow(columns))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var alarm = ParseAlarmFromColumns(columns);
                if (alarm != null)
                {
                    list.Add(alarm);
                }
            }

            return list;
        }

        public void Save(IEnumerable<AlarmItem> alarms)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Time,Title,Status");

            foreach (var alarm in alarms)
            {
                string dateStr = alarm.DateString;
                string timeStr = alarm.TimeString;
                string titleEscaped = EscapeCsvField(alarm.Title);
                string status = string.IsNullOrWhiteSpace(alarm.Status) ? "Pending" : alarm.Status.Trim();

                sb.AppendLine(string.Format("{0},{1},{2},{3}", dateStr, timeStr, titleEscaped, status));
            }

            // Retry save with file sharing up to 5 times
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    string dir = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
                    {
                        writer.Write(sb.ToString());
                    }
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(60);
                }
            }
        }

        private static bool IsHeaderRow(List<string> cols)
        {
            if (cols.Count == 0) return false;
            string first = cols[0].Trim().ToLowerInvariant();
            if (first == "date" || first == "time" || first == "title" || first == "name") return true;

            if (cols.Count > 1)
            {
                string second = cols[1].Trim().ToLowerInvariant();
                if (second == "time" || second == "title" || second == "name") return true;
            }
            return false;
        }

        private static AlarmItem ParseAlarmFromColumns(List<string> cols)
        {
            // Possible patterns:
            // 1 col: invalid or "8:32"
            // 2 cols: [Time, Title] or [Date, Title]
            // 3 cols: [Date, Time, Title] or [Time, Title, Status]
            // 4+ cols: [Date, Time, Title, Status, ...]

            if (cols.Count == 0) return null;

            DateTime date = DateTime.Today;
            TimeSpan time = TimeSpan.Zero;
            string title = string.Empty;
            string status = "Pending";

            if (cols.Count == 1)
            {
                if (TryParseTime(cols[0], out time))
                {
                    title = "Alarm";
                }
                else
                {
                    return null;
                }
            }
            else if (cols.Count == 2)
            {
                // Likely Time, Title
                if (TryParseTime(cols[0], out time))
                {
                    title = cols[1];
                }
                else if (TryParseDate(cols[0], out date) && TryParseTime(cols[1], out time))
                {
                    title = "Alarm";
                }
                else
                {
                    title = cols[1];
                    time = DateTime.Now.TimeOfDay;
                }
            }
            else if (cols.Count == 3)
            {
                // Check if col[0] is Date and col[1] is Time
                if (TryParseDate(cols[0], out date) && TryParseTime(cols[1], out time))
                {
                    title = cols[2];
                }
                // Check if col[0] is Time and col[1] is Title and col[2] is Status
                else if (TryParseTime(cols[0], out time))
                {
                    title = cols[1];
                    status = cols[2];
                }
                else
                {
                    // Fallback
                    TryParseDate(cols[0], out date);
                    TryParseTime(cols[1], out time);
                    title = cols[2];
                }
            }
            else
            {
                // 4 or more columns: Date, Time, Title, Status
                if (!TryParseDate(cols[0], out date))
                {
                    date = DateTime.Today;
                }
                if (!TryParseTime(cols[1], out time))
                {
                    // Maybe col 0 was time and col 1 was title?
                    TimeSpan altTime;
                    if (TryParseTime(cols[0], out altTime))
                    {
                        time = altTime;
                        title = cols[1];
                        status = cols[2];
                        return new AlarmItem(date, time, title, status);
                    }
                }
                title = cols[2];
                status = cols[3];
            }

            return new AlarmItem(date, time, title, status);
        }

        public static bool TryParseDate(string input, out DateTime result)
        {
            result = DateTime.Today;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string text = input.Trim();
            if (string.Equals(text, "today", StringComparison.OrdinalIgnoreCase))
            {
                result = DateTime.Today;
                return true;
            }
            if (string.Equals(text, "tomorrow", StringComparison.OrdinalIgnoreCase))
            {
                result = DateTime.Today.AddDays(1);
                return true;
            }

            string[] formats = new string[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy.MM.dd",
                "M/d/yyyy",
                "MM/dd/yyyy",
                "d/M/yyyy",
                "dd/MM/yyyy",
                "yyyy-M-d",
                "M-d-yyyy",
                "d-M-yyyy"
            };

            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                result = result.Date;
                return true;
            }

            DateTime parsed;
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed) ||
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                result = parsed.Date;
                return true;
            }

            return false;
        }

        public static bool TryParseTime(string input, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string text = input.Trim();

            // Try standard TimeSpan (e.g. "8:32", "08:32:00")
            TimeSpan ts;
            if (TimeSpan.TryParse(text, out ts))
            {
                result = ts;
                return true;
            }

            // Try DateTime parsing for 12-hour AM/PM formats (e.g. "8:32 AM", "8:32pm")
            DateTime dt;
            string[] timeFormats = new string[]
            {
                "h:mm tt",
                "hh:mm tt",
                "H:mm",
                "HH:mm",
                "h:mm:ss tt",
                "hh:mm:ss tt",
                "H:mm:ss",
                "HH:mm:ss",
                "h:m tt",
                "H:m"
            };

            if (DateTime.TryParseExact(text, timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                result = dt.TimeOfDay;
                return true;
            }

            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out dt) ||
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out dt))
            {
                result = dt.TimeOfDay;
                return true;
            }

            return false;
        }

        private static List<string> ParseCsvRow(string line)
        {
            var list = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '\"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '\"')
                        {
                            cur.Append('\"');
                            i++; // skip escaped quote
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
                else
                {
                    if (c == '\"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        list.Add(cur.ToString().Trim());
                        cur.Length = 0;
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
            }

            list.Add(cur.ToString().Trim());
            return list;
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }
}
