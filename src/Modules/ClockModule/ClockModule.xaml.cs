namespace OverlayApp.Modules.Clock
{
    using System;
    using System.Text.Json.Nodes;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;

    public partial class ClockModule : System.Windows.Controls.UserControl
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private string _timeFormat = "HH:mm:ss";
        private string _dateFormat = "yyyy-MM-dd";
        private bool _showDate = true;
        private bool _showWeekday = false;
        private double _backgroundOpacity = 0.6;
        private Thickness _innerPadding = new Thickness(8);
        private double _timeScale = 1.0;

        public ClockModule()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += (_, __) => RenderNow(DateTime.Now);
            _timer.Start();

            Pad.Margin = _innerPadding;
            Bg.Opacity = _backgroundOpacity;
            RenderNow(DateTime.Now);
        }

        public void ApplySettings(JsonObject settings)
        {
            if (settings == null) return;

            _timeFormat = settings["timeFormat"]?.GetValue<string>() ?? _timeFormat;
            _dateFormat = settings["dateFormat"]?.GetValue<string>() ?? _dateFormat;
            _showDate = settings["showDate"]?.GetValue<bool>() ?? _showDate;
            _showWeekday = settings["showWeekday"]?.GetValue<bool>() ?? _showWeekday;

            _backgroundOpacity = Clamp01(settings["backgroundOpacity"]?.GetValue<double>() ?? _backgroundOpacity);
            Bg.Opacity = _backgroundOpacity;

            var pad = settings["padding"]?.GetValue<double>();
            if (pad.HasValue) _innerPadding = new Thickness(pad.Value);
            Pad.Margin = _innerPadding;

            var ts = settings["timeScale"]?.GetValue<double>();
            if (ts.HasValue) SetTimeScale(Clamp(ts.Value, 1.0, 2.0));

            RenderNow(DateTime.Now);
        }

        private void SetTimeScale(double s)
        {
            _timeScale = s;
            TimeScale.ScaleX = s;
            TimeScale.ScaleY = s;
        }

        private static double Clamp(double v, double min, double max) =>
            v < min ? min : (v > max ? max : v);

        private void RenderNow(DateTime now)
        {
            try
            {
                TimeBlock.Text = now.ToString(_timeFormat);

                // Build second line; use empty strings to avoid nullable warnings
                var weekday = _showWeekday ? now.ToString("dddd") : string.Empty;
                var date = _showDate ? now.ToString(_dateFormat) : string.Empty;

                string line = string.Empty;
                if (!string.IsNullOrEmpty(weekday) && !string.IsNullOrEmpty(date)) line = $"{weekday} {date}";
                else line = !string.IsNullOrEmpty(weekday) ? weekday : date;

                if (!string.IsNullOrEmpty(line))
                {
                    DateBlock.Text = line;
                    DateBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    DateBlock.Text = string.Empty;
                    DateBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                TimeBlock.Text = now.ToString("HH:mm:ss");
                DateBlock.Text = string.Empty;
                DateBlock.Visibility = Visibility.Collapsed;
            }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
