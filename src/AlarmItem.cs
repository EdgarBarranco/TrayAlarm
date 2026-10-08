using System;

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
            get { return Date.ToString("yyyy-MM-dd"); }
        }

        public string TimeString
        {
            get { return string.Format("{0:D2}:{1:D2}", Time.Hours, Time.Minutes); }
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
