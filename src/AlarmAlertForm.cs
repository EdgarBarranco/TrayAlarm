using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TrayAlarm
{
    public class AlarmAlertForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private readonly AlarmItem _alarm;
        private readonly Timer _soundTimer;
        private readonly Timer _flashTimer;
        private bool _soundMuted = false;

        public event EventHandler AlarmDismissed;
        public event EventHandler<int> AlarmSnoozed; // minutes

        public AlarmAlertForm(AlarmItem alarm)
        {
            _alarm = alarm;

            InitializeCustomComponent();

            _soundTimer = new Timer();
            _soundTimer.Interval = 2500;
            _soundTimer.Tick += (s, e) =>
            {
                if (!_soundMuted)
                {
                    try { SystemSounds.Exclamation.Play(); } catch { }
                }
            };
            _soundTimer.Start();

            _flashTimer = new Timer();
            _flashTimer.Interval = 800;
            _flashTimer.Tick += (s, e) =>
            {
                try { FlashWindow(this.Handle, true); } catch { }
            };
            _flashTimer.Start();

            // Play initial alert sound
            try { SystemSounds.Exclamation.Play(); } catch { }
        }

        private void InitializeCustomComponent()
        {
            this.Text = "ALARM - " + _alarm.Title;
            this.Size = new Size(460, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Banner header
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(220, 38, 38) // Vibrant coral red for urgency
            };

            Label lblBanner = new Label
            {
                Text = "ALARM ALERT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Location = new Point(20, 12),
                AutoSize = true
            };

            Label lblBannerSub = new Label
            {
                Text = string.Format("Scheduled for {0} at {1}", _alarm.DateString, _alarm.TimeString),
                ForeColor = Color.FromArgb(254, 226, 226),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Location = new Point(21, 38),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblBanner);
            pnlHeader.Controls.Add(lblBannerSub);

            // Main Content
            Label lblTitle = new Label
            {
                Text = string.IsNullOrWhiteSpace(_alarm.Title) ? "Alarm" : _alarm.Title,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 80),
                Size = new Size(405, 60),
                AutoEllipsis = true
            };

            Label lblCurrentTime = new Label
            {
                Text = "Triggered at: " + DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(22, 145),
                AutoSize = true
            };

            // Buttons
            Button btnClose = new Button
            {
                Text = "Close Alarm (Enter)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 42),
                Location = new Point(20, 180),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => DismissAlarm();

            Button btnSnooze5 = new Button
            {
                Text = "+5m Snooze",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 42),
                Location = new Point(210, 180),
                Cursor = Cursors.Hand
            };
            btnSnooze5.FlatAppearance.BorderSize = 0;
            btnSnooze5.Click += (s, e) => SnoozeAlarm(5);

            Button btnSnooze10 = new Button
            {
                Text = "+10m Snooze",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(105, 42),
                Location = new Point(330, 180),
                Cursor = Cursors.Hand
            };
            btnSnooze10.FlatAppearance.BorderSize = 0;
            btnSnooze10.Click += (s, e) => SnoozeAlarm(10);

            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblCurrentTime);
            this.Controls.Add(btnClose);
            this.Controls.Add(btnSnooze5);
            this.Controls.Add(btnSnooze10);

            this.AcceptButton = btnClose;
            this.CancelButton = btnClose;

            this.FormClosing += (s, e) =>
            {
                _soundTimer.Stop();
                _flashTimer.Stop();
            };
        }

        private void DismissAlarm()
        {
            _soundMuted = true;
            _soundTimer.Stop();
            _flashTimer.Stop();
            if (AlarmDismissed != null)
            {
                AlarmDismissed(this, EventArgs.Empty);
            }
            this.Close();
        }

        private void SnoozeAlarm(int minutes)
        {
            _soundMuted = true;
            _soundTimer.Stop();
            _flashTimer.Stop();
            if (AlarmSnoozed != null)
            {
                AlarmSnoozed(this, minutes);
            }
            this.Close();
        }
    }
}
