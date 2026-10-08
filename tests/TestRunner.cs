using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using TrayAlarm;

namespace TrayAlarm.Tests
{
    class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;

        static int Main(string[] args)
        {
            Console.WriteLine("======================================");
            Console.WriteLine(" Running TrayAlarm Unit & Logic Tests ");
            Console.WriteLine("======================================");

            RunTest("LoadStandardCsv", TestLoadStandardCsv);
            RunTest("LoadManual2ColCsv", TestLoadManual2ColCsv);
            RunTest("LoadManual3ColCsv", TestLoadManual3ColCsv);
            RunTest("LoadQuotedCsv", TestLoadQuotedCsv);
            RunTest("Load12HourTimeFormats", TestLoad12HourTimeFormats);
            RunTest("SpecialDates_TodayAndTomorrow", TestSpecialDates);
            RunTest("SaveRoundTrip", TestSaveRoundTrip);
            RunTest("TriggerEvaluationLogic", TestTriggerEvaluationLogic);
            RunTest("SnoozeCalculation", TestSnoozeCalculation);
            RunTest("DateFormattingAnd12HourAmPm", TestDateFormattingAnd12HourAmPm);

            // System Theme Tests
            RunTest("ThemeManagerDefaultModeAndPalette", TestThemeManagerDefaultModeAndPalette);
            RunTest("ThemeManagerLightPalette", TestThemeManagerLightPalette);
            RunTest("ThemeManagerDarkPalette", TestThemeManagerDarkPalette);
            RunTest("ThemeManagerLuminanceAndContrast", TestThemeManagerLuminanceAndContrast);
            RunTest("ThemeManagerBrightnessAdjustment", TestThemeManagerBrightnessAdjustment);
            RunTest("ThemeManagerModeSwitchAndEvent", TestThemeManagerModeSwitchAndEvent);
            RunTest("ThemeManagerSystemAccentRetrieval", TestThemeManagerSystemAccentRetrieval);
            RunTest("ThemeManagerSystemDarkDetection", TestThemeManagerSystemDarkDetection);

            Console.WriteLine("\n--------------------------------------");
            Console.WriteLine(string.Format("Results: {0} Passed, {1} Failed", _passed, _failed));
            Console.WriteLine("--------------------------------------");

            return _failed == 0 ? 0 : 1;
        }

        static void RunTest(string name, Action testAction)
        {
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[PASS] " + name);
                Console.ResetColor();
                _passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FAIL] " + name + ": " + ex.Message);
                Console.ResetColor();
                _failed++;
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        static void TestLoadStandardCsv()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Date,Time,Title,Status\n2026-10-07,08:32,check stove,Pending\n2026-10-07,08:49,change tv to channel 5,Pending\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 2, "Expected 2 items, got " + list.Count);
                Assert(list[0].Title == "check stove", "Expected title 'check stove', got " + list[0].Title);
                Assert(list[0].Time == new TimeSpan(8, 32, 0), "Expected time 08:32:00, got " + list[0].Time);
                Assert(list[0].Date == new DateTime(2026, 10, 7), "Expected date 2026-10-07, got " + list[0].DateString);
                Assert(list[0].IsPending, "Expected Pending status");

                Assert(list[1].Title == "change tv to channel 5", "Expected title 'change tv to channel 5', got " + list[1].Title);
                Assert(list[1].Time == new TimeSpan(8, 49, 0), "Expected time 08:49:00, got " + list[1].Time);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestLoadManual2ColCsv()
        {
            // User modified CSV by hand without date or header: just time and title!
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "8:32, check stove\n08:49, change tv to channel 5\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 2, "Expected 2 items, got " + list.Count);
                Assert(list[0].Title == "check stove", "Expected 'check stove', got " + list[0].Title);
                Assert(list[0].Date == DateTime.Today, "Date should default to today");
                Assert(list[0].Time.Hours == 8 && list[0].Time.Minutes == 32, "Time should be 8:32");
                Assert(list[0].IsPending, "Status should default to Pending");

                Assert(list[1].Title == "change tv to channel 5", "Expected 'change tv to channel 5', got " + list[1].Title);
                Assert(list[1].Time.Hours == 8 && list[1].Time.Minutes == 49, "Time should be 8:49");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestLoadManual3ColCsv()
        {
            // User wrote Date, Time, Title without status
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "2026-12-25, 09:00, Open presents\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 1, "Expected 1 item");
                Assert(list[0].Date == new DateTime(2026, 12, 25), "Expected Christmas date");
                Assert(list[0].Time == new TimeSpan(9, 0, 0), "Expected 9:00 AM");
                Assert(list[0].Title == "Open presents", "Expected title");
                Assert(list[0].IsPending, "Expected Pending status");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestLoadQuotedCsv()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Date,Time,Title,Status\n2026-10-07,08:32,\"check stove, turn off flame\",Pending\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 1, "Expected 1 item");
                Assert(list[0].Title == "check stove, turn off flame", "Expected unescaped title with comma");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestLoad12HourTimeFormats()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Time,Title\n8:32 AM,Morning stretch\n8:49 PM,Watch evening news\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 2, "Expected 2 items");
                Assert(list[0].Time == new TimeSpan(8, 32, 0), "Expected 8:32 AM");
                Assert(list[1].Time == new TimeSpan(20, 49, 0), "Expected 20:49 (8:49 PM)");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestSpecialDates()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Date,Time,Title\ntoday,10:00,Team sync\ntomorrow,11:30,Dentist\n");
                var repo = new CsvRepository(tempFile);
                var list = repo.Load();

                Assert(list.Count == 2, "Expected 2 items");
                Assert(list[0].Date == DateTime.Today, "Expected today");
                Assert(list[1].Date == DateTime.Today.AddDays(1), "Expected tomorrow");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestSaveRoundTrip()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                var repo = new CsvRepository(tempFile);
                var alarms = new List<AlarmItem>
                {
                    new AlarmItem(DateTime.Today, new TimeSpan(8, 32, 0), "check stove, carefully", "Pending"),
                    new AlarmItem(DateTime.Today, new TimeSpan(8, 49, 0), "change tv to channel 5", "Triggered")
                };

                repo.Save(alarms);
                var loaded = repo.Load();

                Assert(loaded.Count == 2, "Expected 2 items after save and reload");
                Assert(loaded[0].Title == "check stove, carefully", "Expected exact title with comma");
                Assert(loaded[0].TimeString == "08:32 AM", "Expected time 08:32 AM");
                Assert(loaded[0].DateString == DateTime.Today.ToString("MM/dd/yyyy"), "Expected MM/dd/yyyy date format");
                Assert(loaded[0].Status == "Pending", "Expected status Pending");
                Assert(loaded[1].Title == "change tv to channel 5", "Expected title");
                Assert(loaded[1].TimeString == "08:49 AM", "Expected time 08:49 AM");
                Assert(loaded[1].Status == "Triggered", "Expected status Triggered");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestDateFormattingAnd12HourAmPm()
        {
            // 1. Date format verification (MM/dd/yyyy)
            var alarmDate1 = new AlarmItem(new DateTime(2026, 10, 8), new TimeSpan(8, 32, 0), "Date test");
            Assert(alarmDate1.DateString == "10/08/2026", "Expected 10/08/2026, got " + alarmDate1.DateString);

            var alarmDate2 = new AlarmItem(new DateTime(2026, 1, 5), new TimeSpan(8, 32, 0), "Date test single digit");
            Assert(alarmDate2.DateString == "01/05/2026", "Expected 01/05/2026, got " + alarmDate2.DateString);

            // 2. Time format verification (hh:mm AM/PM)
            var morning = new AlarmItem(DateTime.Today, new TimeSpan(8, 32, 0), "Morning");
            Assert(morning.TimeString == "08:32 AM", "Expected 08:32 AM, got " + morning.TimeString);

            var noon = new AlarmItem(DateTime.Today, new TimeSpan(12, 0, 0), "Noon");
            Assert(noon.TimeString == "12:00 PM", "Expected 12:00 PM, got " + noon.TimeString);

            var afternoon = new AlarmItem(DateTime.Today, new TimeSpan(13, 45, 0), "Afternoon");
            Assert(afternoon.TimeString == "01:45 PM", "Expected 01:45 PM, got " + afternoon.TimeString);

            var evening = new AlarmItem(DateTime.Today, new TimeSpan(20, 49, 0), "Evening");
            Assert(evening.TimeString == "08:49 PM", "Expected 08:49 PM, got " + evening.TimeString);

            var midnight = new AlarmItem(DateTime.Today, new TimeSpan(0, 0, 0), "Midnight");
            Assert(midnight.TimeString == "12:00 AM", "Expected 12:00 AM, got " + midnight.TimeString);

            var midnightFive = new AlarmItem(DateTime.Today, new TimeSpan(0, 5, 0), "Midnight 5");
            Assert(midnightFive.TimeString == "12:05 AM", "Expected 12:05 AM, got " + midnightFive.TimeString);

            // 3. Round-trip serialization and tolerant parsing of MM/dd/yyyy and hh:mm AM/PM
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "Date,Time,Title,Status\n10/08/2026,08:32 AM,Morning stretch,Pending\n10/08/2026,08:49 PM,Evening film,Pending\n");
                var repo = new CsvRepository(tempFile);
                var loaded = repo.Load();

                Assert(loaded.Count == 2, "Expected 2 items");
                Assert(loaded[0].DateString == "10/08/2026", "Expected 10/08/2026");
                Assert(loaded[0].TimeString == "08:32 AM", "Expected 08:32 AM");
                Assert(loaded[0].Time == new TimeSpan(8, 32, 0), "Expected 8:32:00 TimeSpan");

                Assert(loaded[1].DateString == "10/08/2026", "Expected 10/08/2026");
                Assert(loaded[1].TimeString == "08:49 PM", "Expected 08:49 PM");
                Assert(loaded[1].Time == new TimeSpan(20, 49, 0), "Expected 20:49:00 TimeSpan");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        static void TestTriggerEvaluationLogic()
        {
            var pastAlarm = new AlarmItem(DateTime.Today, DateTime.Now.AddMinutes(-5).TimeOfDay, "Past alarm", "Pending");
            var futureAlarm = new AlarmItem(DateTime.Today, DateTime.Now.AddMinutes(10).TimeOfDay, "Future alarm", "Pending");

            DateTime now = DateTime.Now;

            Assert(now >= pastAlarm.ScheduledDateTime, "Past alarm should be due");
            Assert(now < futureAlarm.ScheduledDateTime, "Future alarm should not be due yet");
        }

        static void TestSnoozeCalculation()
        {
            var baseAlarm = new AlarmItem(DateTime.Today, new TimeSpan(12, 0, 0), "Lunch time", "Triggered");
            DateTime snoozeTarget = DateTime.Now.AddMinutes(5);

            var snoozed = new AlarmItem(snoozeTarget.Date, snoozeTarget.TimeOfDay, baseAlarm.Title + " (Snooze)", "Pending");

            Assert(snoozed.IsPending, "Snoozed alarm should be Pending");
            Assert(snoozed.Title == "Lunch time (Snooze)", "Title should indicate snooze");
            Assert(snoozed.ScheduledDateTime > DateTime.Now, "Snoozed alarm should be in the future");
        }

        static void TestThemeManagerDefaultModeAndPalette()
        {
            ThemeManager.Initialize();
            Assert(ThemeManager.CurrentTheme != null, "CurrentTheme should not be null");
            Assert(ThemeManager.Mode == ThemeMode.System, "Default mode should be ThemeMode.System");
            Assert(ThemeManager.CurrentTheme.AccentColor.A == 255, "Accent color alpha should be 255");
        }

        static void TestThemeManagerLightPalette()
        {
            Color testAccent = Color.FromArgb(0, 120, 212);
            ThemeColors palette = ThemeManager.CreatePalette(false, testAccent);

            Assert(!palette.IsDarkMode, "Light palette IsDarkMode should be false");
            Assert(palette.CardBackground == Color.White, "CardBackground should be White");
            Assert(palette.TextPrimary == Color.FromArgb(15, 23, 42), "TextPrimary should be dark slate");
            Assert(palette.AccentColor == testAccent, "AccentColor should match provided accent");
            Assert(palette.GridBackground == Color.White, "GridBackground should be White");
        }

        static void TestThemeManagerDarkPalette()
        {
            Color testAccent = Color.FromArgb(0, 120, 212);
            ThemeColors palette = ThemeManager.CreatePalette(true, testAccent);

            Assert(palette.IsDarkMode, "Dark palette IsDarkMode should be true");
            Assert(palette.WindowBackground == Color.FromArgb(32, 32, 32), "WindowBackground should be dark");
            Assert(palette.CardBackground == Color.FromArgb(43, 43, 43), "CardBackground should be dark surface");
            Assert(palette.TextPrimary == Color.FromArgb(245, 245, 245), "TextPrimary should be bright white");
            Assert(palette.AccentColor == testAccent, "AccentColor should match provided accent");
            Assert(palette.GridBackground == Color.FromArgb(28, 28, 30), "GridBackground should be dark gray");
        }

        static void TestThemeManagerLuminanceAndContrast()
        {
            double blackLum = ThemeManager.CalculateLuminance(Color.Black);
            double whiteLum = ThemeManager.CalculateLuminance(Color.White);

            Assert(Math.Abs(blackLum - 0.0) < 0.01, "Black luminance should be ~0.0");
            Assert(Math.Abs(whiteLum - 1.0) < 0.01, "White luminance should be ~1.0");

            // Bright yellow accent should yield dark text for WCAG contrast
            Color yellowAccent = Color.FromArgb(255, 235, 59);
            ThemeColors yellowPalette = ThemeManager.CreatePalette(false, yellowAccent);
            Assert(yellowPalette.AccentTextColor == Color.FromArgb(15, 23, 42), "Bright yellow accent requires dark text");

            // Dark blue accent should yield white text
            Color navyAccent = Color.FromArgb(10, 30, 80);
            ThemeColors navyPalette = ThemeManager.CreatePalette(false, navyAccent);
            Assert(navyPalette.AccentTextColor == Color.White, "Dark navy accent requires white text");
        }

        static void TestThemeManagerBrightnessAdjustment()
        {
            Color baseColor = Color.FromArgb(100, 100, 100);
            Color brighter = ThemeManager.AdjustBrightness(baseColor, 1.5f);
            Color darker = ThemeManager.AdjustBrightness(baseColor, 0.5f);

            Assert(brighter.R == 150 && brighter.G == 150 && brighter.B == 150, "Brighter should be 150");
            Assert(darker.R == 50 && darker.G == 50 && darker.B == 50, "Darker should be 50");

            Color maxCapped = ThemeManager.AdjustBrightness(Color.FromArgb(200, 200, 200), 2.0f);
            Assert(maxCapped.R == 255 && maxCapped.G == 255 && maxCapped.B == 255, "Should cap at 255");
        }

        static void TestThemeManagerModeSwitchAndEvent()
        {
            bool eventFired = false;
            EventHandler handler = (s, e) => { eventFired = true; };

            ThemeManager.ThemeChanged += handler;
            try
            {
                ThemeManager.Mode = ThemeMode.Dark;
                Assert(eventFired, "ThemeChanged event should fire when switching to Dark");
                Assert(ThemeManager.CurrentTheme.IsDarkMode, "CurrentTheme should be dark");

                eventFired = false;
                ThemeManager.Mode = ThemeMode.Light;
                Assert(eventFired, "ThemeChanged event should fire when switching to Light");
                Assert(!ThemeManager.CurrentTheme.IsDarkMode, "CurrentTheme should be light");
            }
            finally
            {
                ThemeManager.ThemeChanged -= handler;
                ThemeManager.Mode = ThemeMode.System; // Reset to default
            }
        }

        static void TestThemeManagerSystemAccentRetrieval()
        {
            Color accent = ThemeManager.GetSystemAccentColor();
            Assert(accent != Color.Empty, "System accent color should not be empty");
            Assert(accent.A == 255, "System accent alpha should be 255");
            Assert(accent.R > 0 || accent.G > 0 || accent.B > 0, "System accent should have non-black components");
        }

        static void TestThemeManagerSystemDarkDetection()
        {
            // Verifies IsSystemInDarkMode executes safely without throwing any exceptions
            bool isDark = ThemeManager.IsSystemInDarkMode();
            Assert(isDark == true || isDark == false, "IsSystemInDarkMode should return valid boolean");
        }
    }
}
