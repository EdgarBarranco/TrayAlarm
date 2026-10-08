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

        private Panel _pnlHeader;
        private Label _lblTitle;
        private Label _lblCurrentTime;
        private Button _btnClose;
        private Button _btnSnooze5;
        private Button _btnSnooze10;

        public event EventHandler AlarmDismissed;
        public event EventHandler<int> AlarmSnoozed; // minutes

        public AlarmAlertForm(AlarmItem alarm)
        {
            _alarm = alarm;

            InitializeCustomComponent();
            ApplyTheme(ThemeManager.CurrentTheme);
            ThemeManager.ThemeChanged += OnThemeChanged;

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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeManager.ApplyImmersiveDarkMode(this.Handle, ThemeManager.CurrentTheme.IsDarkMode);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    ApplyTheme(ThemeManager.CurrentTheme);
                });
            }
            else
            {
                ApplyTheme(ThemeManager.CurrentTheme);
            }
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
            _pnlHeader = new Panel
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

            _pnlHeader.Controls.Add(lblBanner);
            _pnlHeader.Controls.Add(lblBannerSub);

            // Main Content
            _lblTitle = new Label
            {
                Text = string.IsNullOrWhiteSpace(_alarm.Title) ? "Alarm" : _alarm.Title,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 80),
                Size = new Size(405, 60),
                AutoEllipsis = true
            };

            _lblCurrentTime = new Label
            {
                Text = "Triggered at: " + DateTime.Now.ToString("hh:mm:ss tt"),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(22, 145),
                AutoSize = true
            };

            // Buttons
            _btnClose = new Button
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
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => DismissAlarm();

            _btnSnooze5 = new Button
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
            _btnSnooze5.FlatAppearance.BorderSize = 0;
            _btnSnooze5.Click += (s, e) => SnoozeAlarm(5);

            _btnSnooze10 = new Button
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
            _btnSnooze10.FlatAppearance.BorderSize = 0;
            _btnSnooze10.Click += (s, e) => SnoozeAlarm(10);

            this.Controls.Add(_pnlHeader);
            this.Controls.Add(_lblTitle);
            this.Controls.Add(_lblCurrentTime);
            this.Controls.Add(_btnClose);
            this.Controls.Add(_btnSnooze5);
            this.Controls.Add(_btnSnooze10);

            this.AcceptButton = _btnClose;
            this.CancelButton = _btnClose;

            this.FormClosing += (s, e) =>
            {
                ThemeManager.ThemeChanged -= OnThemeChanged;
                _soundTimer.Stop();
                _flashTimer.Stop();
            };
        }

        private void ApplyTheme(ThemeColors theme)
        {
            this.BackColor = theme.CardBackground;

            if (this.IsHandleCreated)
            {
                ThemeManager.ApplyImmersiveDarkMode(this.Handle, theme.IsDarkMode);
            }

            if (_lblTitle != null)
            {
                _lblTitle.ForeColor = theme.TextPrimary;
            }
            if (_lblCurrentTime != null)
            {
                _lblCurrentTime.ForeColor = theme.TextMuted;
            }

            if (_btnClose != null)
            {
                _btnClose.BackColor = theme.DangerColor;
                _btnClose.ForeColor = Color.White;
                _btnClose.FlatAppearance.MouseOverBackColor = ThemeManager.AdjustBrightness(theme.DangerColor, theme.IsDarkMode ? 1.15f : 0.85f);
            }

            if (_btnSnooze5 != null)
            {
                _btnSnooze5.BackColor = theme.ButtonBackground;
                _btnSnooze5.ForeColor = theme.ButtonForeground;
                _btnSnooze5.FlatAppearance.BorderColor = theme.InputBorder;
                _btnSnooze5.FlatAppearance.BorderSize = 1;
                _btnSnooze5.FlatAppearance.MouseOverBackColor = theme.ButtonHoverBackground;
            }

            if (_btnSnooze10 != null)
            {
                _btnSnooze10.BackColor = theme.ButtonBackground;
                _btnSnooze10.ForeColor = theme.ButtonForeground;
                _btnSnooze10.FlatAppearance.BorderColor = theme.InputBorder;
                _btnSnooze10.FlatAppearance.BorderSize = 1;
                _btnSnooze10.FlatAppearance.MouseOverBackColor = theme.ButtonHoverBackground;
            }
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
