using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TrayAlarm
{
    public class MainForm : Form
    {
        private readonly string _csvPath;
        private readonly CsvRepository _repo;
        private readonly List<AlarmItem> _alarms = new List<AlarmItem>();

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _trayMenu;
        private Timer _alarmCheckTimer;
        private FileSystemWatcher _fileWatcher;
        private Timer _fileWatcherDebounceTimer;

        // UI Controls
        private DateTimePicker _dtpDate;
        private DateTimePicker _dtpTime;
        private TextBox _txtTitle;
        private Button _btnAddAlarm;
        private DataGridView _grid;
        private ToolStripStatusLabel _statusLabel;
        private Label _lblSummary;
        private bool _isExplicitExit = false;
        private bool _hasShownMinimizeBalloon = false;
        private bool _isSavingInternal = false;

        public MainForm()
        {
            _csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alarms.csv");
            _repo = new CsvRepository(_csvPath);

            InitializeComponents();
            SetupTrayIcon();
            SetupFileWatcher();
            SetupAlarmTimer();

            LoadAlarmsFromDisk();
        }

        private void InitializeComponents()
        {
            this.Text = "Tray Alarm Manager";
            this.Size = new Size(780, 560);
            this.MinimumSize = new Size(680, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(243, 244, 246);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.Icon = IconHelper.GetAppIcon();

            // Top Header Panel
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            Label lblTitle = new Label
            {
                Text = "Tray Alarm Manager",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                Location = new Point(18, 12),
                AutoSize = true
            };

            _lblSummary = new Label
            {
                Text = "Loading alarms...",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Location = new Point(20, 38),
                AutoSize = true
            };

            Button btnOpenNotepad = new Button
            {
                Text = "Open alarms.csv",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 32),
                Location = new Point(480, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnOpenNotepad.FlatAppearance.BorderSize = 0;
            btnOpenNotepad.Click += (s, e) => OpenCsvInNotepad();

            Button btnReloadCsv = new Button
            {
                Text = "Reload CSV",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 32),
                Location = new Point(620, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnReloadCsv.FlatAppearance.BorderSize = 0;
            btnReloadCsv.Click += (s, e) =>
            {
                LoadAlarmsFromDisk();
                UpdateStatus("Reloaded alarms.csv from disk.");
            };

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(_lblSummary);
            pnlTop.Controls.Add(btnOpenNotepad);
            pnlTop.Controls.Add(btnReloadCsv);

            // Add Alarm Card Panel
            GroupBox grpAdd = new GroupBox
            {
                Text = " Add New Alarm ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Dock = DockStyle.Top,
                Height = 115,
                Padding = new Padding(12),
                BackColor = Color.White
            };

            Label lblDate = new Label
            {
                Text = "Date (default: today):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(16, 26),
                AutoSize = true
            };

            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd",
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(16, 48),
                Width = 120
            };

            Label lblTime = new Label
            {
                Text = "Time (HH:mm):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(148, 26),
                AutoSize = true
            };

            _dtpTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddMinutes(5),
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(148, 48),
                Width = 90
            };

            Label lblAlarmTitle = new Label
            {
                Text = "Alarm Title / Note (e.g. check stove, change tv to channel 5):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(250, 26),
                AutoSize = true
            };

            _txtTitle = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(250, 48),
                Width = 280,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _txtTitle.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    OnAddAlarmClicked();
                }
            };

            _btnAddAlarm = new Button
            {
                Text = "+ Add Alarm",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(540, 47),
                Size = new Size(110, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnAddAlarm.FlatAppearance.BorderSize = 0;
            _btnAddAlarm.Click += (s, e) => OnAddAlarmClicked();

            // Quick preset offset buttons
            Button btnPlus5 = CreatePresetButton("+5m", 5, 16, 80);
            Button btnPlus15 = CreatePresetButton("+15m", 15, 66, 80);
            Button btnPlus30 = CreatePresetButton("+30m", 30, 122, 80);
            Button btnPlus60 = CreatePresetButton("+1h", 60, 184, 80);

            grpAdd.Controls.Add(lblDate);
            grpAdd.Controls.Add(_dtpDate);
            grpAdd.Controls.Add(lblTime);
            grpAdd.Controls.Add(_dtpTime);
            grpAdd.Controls.Add(lblAlarmTitle);
            grpAdd.Controls.Add(_txtTitle);
            grpAdd.Controls.Add(_btnAddAlarm);
            grpAdd.Controls.Add(btnPlus5);
            grpAdd.Controls.Add(btnPlus15);
            grpAdd.Controls.Add(btnPlus30);
            grpAdd.Controls.Add(btnPlus60);

            // Container Panel for GroupBox with margin
            Panel pnlCardWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                Padding = new Padding(12, 8, 12, 4),
                BackColor = Color.Transparent
            };
            pnlCardWrap.Controls.Add(grpAdd);

            // DataGridView Panel
            Panel pnlGridWrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 4, 12, 8),
                BackColor = Color.Transparent
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9f),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowTemplate = { Height = 32 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 32;

            SetupGridColumns();
            _grid.CellContentClick += OnGridCellContentClick;

            pnlGridWrap.Controls.Add(_grid);

            // Status Strip
            StatusStrip statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 8.5f)
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Text = "Watching alarms.csv",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ToolStripDropDownButton dropDownTools = new ToolStripDropDownButton("Options");
            ToolStripMenuItem mnuClearFired = new ToolStripMenuItem("Clear Triggered / Done Alarms", null, (s, e) => ClearFiredAlarms());
            ToolStripMenuItem mnuMinToTray = new ToolStripMenuItem("Minimize to Tray", null, (s, e) => this.Hide());
            dropDownTools.DropDownItems.Add(mnuClearFired);
            dropDownTools.DropDownItems.Add(new ToolStripSeparator());
            dropDownTools.DropDownItems.Add(mnuMinToTray);

            statusStrip.Items.Add(_statusLabel);
            statusStrip.Items.Add(dropDownTools);

            // Add all controls to Form
            this.Controls.Add(pnlGridWrap);
            this.Controls.Add(pnlCardWrap);
            this.Controls.Add(pnlTop);
            this.Controls.Add(statusStrip);

            this.FormClosing += OnFormClosing;
        }

        private Button CreatePresetButton(string text, int offsetMinutes, int x, int y)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 8f),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(50, 22),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Click += (s, e) =>
            {
                DateTime target = DateTime.Now.AddMinutes(offsetMinutes);
                _dtpDate.Value = target.Date;
                _dtpTime.Value = target;
            };
            return btn;
        }

        private void SetupGridColumns()
        {
            _grid.Columns.Clear();

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColDate",
                HeaderText = "Date",
                Width = 95,
                ReadOnly = true
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTime",
                HeaderText = "Time",
                Width = 70,
                ReadOnly = true
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTitle",
                HeaderText = "Alarm Title",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColStatus",
                HeaderText = "Status",
                Width = 90,
                ReadOnly = true
            });

            DataGridViewButtonColumn btnToggle = new DataGridViewButtonColumn
            {
                Name = "ColToggle",
                HeaderText = "Active",
                Text = "Toggle",
                UseColumnTextForButtonValue = false,
                Width = 75
            };
            _grid.Columns.Add(btnToggle);

            DataGridViewButtonColumn btnTest = new DataGridViewButtonColumn
            {
                Name = "ColTest",
                HeaderText = "Test",
                Text = "Trigger",
                UseColumnTextForButtonValue = true,
                Width = 65
            };
            _grid.Columns.Add(btnTest);

            DataGridViewButtonColumn btnDelete = new DataGridViewButtonColumn
            {
                Name = "ColDelete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 65
            };
            _grid.Columns.Add(btnDelete);
        }

        private void SetupTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();

            ToolStripMenuItem mnuOpen = new ToolStripMenuItem("Open Alarm Manager", null, (s, e) => ShowAndRestore());
            mnuOpen.Font = new Font(mnuOpen.Font, FontStyle.Bold);

            ToolStripMenuItem mnuAdd = new ToolStripMenuItem("Add Quick Alarm...", null, (s, e) =>
            {
                ShowAndRestore();
                _txtTitle.Focus();
            });

            ToolStripMenuItem mnuReload = new ToolStripMenuItem("Reload alarms.csv", null, (s, e) =>
            {
                LoadAlarmsFromDisk();
                UpdateStatus("Reloaded alarms.csv");
            });

            ToolStripMenuItem mnuOpenCsv = new ToolStripMenuItem("Open alarms.csv in Notepad", null, (s, e) => OpenCsvInNotepad());

            ToolStripMenuItem mnuExit = new ToolStripMenuItem("Exit", null, (s, e) =>
            {
                _isExplicitExit = true;
                _notifyIcon.Visible = false;
                Application.Exit();
            });

            _trayMenu.Items.Add(mnuOpen);
            _trayMenu.Items.Add(mnuAdd);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(mnuReload);
            _trayMenu.Items.Add(mnuOpenCsv);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(mnuExit);

            _notifyIcon = new NotifyIcon
            {
                Icon = this.Icon,
                ContextMenuStrip = _trayMenu,
                Text = "Tray Alarm Manager",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowAndRestore();
        }

        private void SetupFileWatcher()
        {
            _fileWatcherDebounceTimer = new Timer();
            _fileWatcherDebounceTimer.Interval = 350;
            _fileWatcherDebounceTimer.Tick += (s, e) =>
            {
                _fileWatcherDebounceTimer.Stop();
                if (!_isSavingInternal)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        LoadAlarmsFromDisk();
                        UpdateStatus("Detected external changes in alarms.csv and reloaded.");
                    });
                }
            };

            try
            {
                string dir = Path.GetDirectoryName(_csvPath);
                if (string.IsNullOrEmpty(dir)) dir = AppDomain.CurrentDomain.BaseDirectory;

                _fileWatcher = new FileSystemWatcher(dir, "alarms.csv")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += (s, e) =>
                {
                    if (!_isSavingInternal)
                    {
                        _fileWatcherDebounceTimer.Stop();
                        _fileWatcherDebounceTimer.Start();
                    }
                };

                _fileWatcher.Created += (s, e) =>
                {
                    if (!_isSavingInternal)
                    {
                        _fileWatcherDebounceTimer.Stop();
                        _fileWatcherDebounceTimer.Start();
                    }
                };
            }
            catch (Exception ex)
            {
                UpdateStatus("File watcher warning: " + ex.Message);
            }
        }

        private void SetupAlarmTimer()
        {
            _alarmCheckTimer = new Timer();
            _alarmCheckTimer.Interval = 1000;
            _alarmCheckTimer.Tick += (s, e) => CheckPendingAlarms();
            _alarmCheckTimer.Start();
        }

        private void OnAddAlarmClicked()
        {
            string title = _txtTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show(this, "Please enter an alarm title or note (e.g. 'check stove').", "Alarm Title Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtTitle.Focus();
                return;
            }

            DateTime date = _dtpDate.Value.Date;
            TimeSpan time = new TimeSpan(_dtpTime.Value.Hour, _dtpTime.Value.Minute, 0);

            var item = new AlarmItem(date, time, title, "Pending");
            _alarms.Add(item);

            SaveAlarmsToDisk();
            RefreshGrid();

            _txtTitle.Clear();
            _dtpDate.Value = DateTime.Today;
            _dtpTime.Value = DateTime.Now.AddMinutes(15);
            _txtTitle.Focus();

            UpdateStatus(string.Format("Added alarm '{0}' for {1} {2}", item.Title, item.DateString, item.TimeString));
        }

        private void CheckPendingAlarms()
        {
            DateTime now = DateTime.Now;
            var alarmsToTrigger = new List<AlarmItem>();

            lock (_alarms)
            {
                foreach (var alarm in _alarms)
                {
                    if (alarm.IsPending)
                    {
                        DateTime scheduled = alarm.ScheduledDateTime;
                        if (now >= scheduled)
                        {
                            alarmsToTrigger.Add(alarm);
                        }
                    }
                }
            }

            if (alarmsToTrigger.Count > 0)
            {
                foreach (var alarm in alarmsToTrigger)
                {
                    alarm.Status = "Triggered";
                    TriggerAlarmPopup(alarm);
                }

                SaveAlarmsToDisk();
                RefreshGrid();
            }
        }

        private void TriggerAlarmPopup(AlarmItem alarm)
        {
            // Tray balloon notification
            _notifyIcon.ShowBalloonTip(5000, "Alarm: " + alarm.Title,
                string.Format("Scheduled for {0} {1}", alarm.DateString, alarm.TimeString),
                ToolTipIcon.Warning);

            // Pop up dedicated alert window
            AlarmAlertForm alertForm = new AlarmAlertForm(alarm);
            alertForm.AlarmDismissed += (s, e) =>
            {
                UpdateStatus(string.Format("Alarm '{0}' closed.", alarm.Title));
            };
            alertForm.AlarmSnoozed += (s, minutes) =>
            {
                DateTime newTime = DateTime.Now.AddMinutes(minutes);
                var snoozedAlarm = new AlarmItem(newTime.Date, newTime.TimeOfDay, alarm.Title + " (Snooze)", "Pending");
                lock (_alarms)
                {
                    _alarms.Add(snoozedAlarm);
                }
                SaveAlarmsToDisk();
                RefreshGrid();
                UpdateStatus(string.Format("Snoozed '{0}' by {1} minutes.", alarm.Title, minutes));
            };

            alertForm.Show();
            alertForm.BringToFront();
        }

        private void OnGridCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _alarms.Count) return;

            var alarm = _alarms[e.RowIndex];
            string colName = _grid.Columns[e.ColumnIndex].Name;

            if (colName == "ColToggle")
            {
                if (alarm.IsPending)
                {
                    alarm.Status = "Disabled";
                }
                else
                {
                    alarm.Status = "Pending";
                }
                SaveAlarmsToDisk();
                RefreshGrid();
            }
            else if (colName == "ColTest")
            {
                TriggerAlarmPopup(alarm.Clone());
            }
            else if (colName == "ColDelete")
            {
                var res = MessageBox.Show(this, string.Format("Delete alarm '{0}'?", alarm.Title), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    _alarms.RemoveAt(e.RowIndex);
                    SaveAlarmsToDisk();
                    RefreshGrid();
                    UpdateStatus("Deleted alarm.");
                }
            }
        }

        private void ClearFiredAlarms()
        {
            int removed = _alarms.RemoveAll(a => a.IsTriggered);
            if (removed > 0)
            {
                SaveAlarmsToDisk();
                RefreshGrid();
                UpdateStatus(string.Format("Cleared {0} triggered alarm(s).", removed));
            }
            else
            {
                UpdateStatus("No triggered alarms to clear.");
            }
        }

        private void LoadAlarmsFromDisk()
        {
            try
            {
                var loaded = _repo.Load();
                lock (_alarms)
                {
                    _alarms.Clear();
                    _alarms.AddRange(loaded);
                }
                RefreshGrid();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error loading CSV: " + ex.Message);
            }
        }

        private void SaveAlarmsToDisk()
        {
            try
            {
                _isSavingInternal = true;
                if (_fileWatcher != null) _fileWatcher.EnableRaisingEvents = false;

                lock (_alarms)
                {
                    _repo.Save(_alarms);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Error saving CSV: " + ex.Message);
            }
            finally
            {
                // Re-enable watcher with slight delay
                Timer t = new Timer { Interval = 400 };
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    t.Dispose();
                    _isSavingInternal = false;
                    if (_fileWatcher != null) _fileWatcher.EnableRaisingEvents = true;
                };
                t.Start();
            }
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();

            int pendingCount = 0;
            AlarmItem nextAlarm = null;
            DateTime now = DateTime.Now;

            foreach (var alarm in _alarms)
            {
                int rowIndex = _grid.Rows.Add();
                var row = _grid.Rows[rowIndex];

                row.Cells["ColDate"].Value = alarm.DateString;
                row.Cells["ColTime"].Value = alarm.TimeString;
                row.Cells["ColTitle"].Value = alarm.Title;
                row.Cells["ColStatus"].Value = alarm.Status;

                var toggleBtn = (DataGridViewButtonCell)row.Cells["ColToggle"];
                toggleBtn.Value = alarm.IsPending ? "Disable" : "Enable";

                // Styling row depending on status
                if (alarm.IsPending)
                {
                    pendingCount++;
                    DateTime sched = alarm.ScheduledDateTime;
                    if (sched >= now)
                    {
                        if (nextAlarm == null || sched < nextAlarm.ScheduledDateTime)
                        {
                            nextAlarm = alarm;
                        }
                    }
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
                    row.Cells["ColTime"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
                }
                else if (alarm.IsTriggered)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
                }
                else if (alarm.IsDisabled)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
                    row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Italic);
                }
            }

            // Update Summary Label & Tray Text
            string summaryText;
            if (pendingCount == 0)
            {
                summaryText = "No pending alarms scheduled.";
                _notifyIcon.Text = "Tray Alarm - No pending alarms";
            }
            else
            {
                if (nextAlarm != null)
                {
                    var diff = nextAlarm.ScheduledDateTime - now;
                    string diffStr = diff.TotalHours >= 1 ? string.Format("{0:F1} hours", diff.TotalHours) : string.Format("{0} mins", Math.Max(1, (int)diff.TotalMinutes));
                    summaryText = string.Format("{0} pending alarm(s) | Next: {1} ({2}) in ~{3}",
                        pendingCount, nextAlarm.TimeString, nextAlarm.Title, diffStr);
                    _notifyIcon.Text = string.Format("Tray Alarm - Next: {0} ({1})", nextAlarm.TimeString, Truncate(nextAlarm.Title, 16));
                }
                else
                {
                    summaryText = string.Format("{0} pending alarm(s) scheduled.", pendingCount);
                    _notifyIcon.Text = string.Format("Tray Alarm - {0} pending", pendingCount);
                }
            }

            _lblSummary.Text = summaryText;
        }

        private static string Truncate(string val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return val.Length <= maxLen ? val : val.Substring(0, maxLen - 2) + "..";
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), message);
        }

        private void OpenCsvInNotepad()
        {
            try
            {
                if (!File.Exists(_csvPath))
                {
                    SaveAlarmsToDisk();
                }
                Process.Start("notepad.exe", _csvPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open Notepad: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAndRestore()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.BringToFront();
            this.Activate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isExplicitExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                if (!_hasShownMinimizeBalloon)
                {
                    _hasShownMinimizeBalloon = true;
                    _notifyIcon.ShowBalloonTip(3000, "Tray Alarm Running",
                        "The app is still running in the system tray. Right-click the tray icon to exit.",
                        ToolTipIcon.Info);
                }
            }
        }
    }
}
