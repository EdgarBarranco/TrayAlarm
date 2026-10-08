using System;
using System.Globalization;

namespace TrayAlarm
{
    public class AlarmItem
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }

        public AlarmItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Date = DateTime.Today;
            Time = DateTime.Now.TimeOfDay;
            Title = string.Empty;
            Status = "Pending";
        }

        public AlarmItem(DateTime date, TimeSpan time, string title, string status = "Pending")
        {
            Id = Guid.NewGuid().ToString("N");
            Date = date.Date;
            Time = new TimeSpan(time.Hours, time.Minutes, time.Seconds);
            Title = title ?? string.Empty;
            Status = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        }

        public DateTime ScheduledDateTime
        {
            get { return Date.Date.Add(Time); }
        }

        public bool IsPending
        {
            get { return string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsTriggered
        {
            get
            {
                return string.Equals(Status, "Triggered", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Status, "Fired", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Status, "Dismissed", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Status, "Done", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsDisabled
        {
            get { return string.Equals(Status, "Disabled", StringComparison.OrdinalIgnoreCase); }
        }

        public string DateString
        {
            get { return Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture); }
        }

        public string TimeString
        {
            get
            {
                int h = Math.Abs(Time.Hours) % 24;
                int m = Math.Abs(Time.Minutes) % 60;
                var dt = new DateTime(2000, 1, 1, h, m, 0);
                return dt.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
        }

        public AlarmItem Clone()
        {
            return new AlarmItem(this.Date, this.Time, this.Title, this.Status)
            {
                Id = this.Id
            };
        }
    }
}
