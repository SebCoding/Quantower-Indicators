// Quantower Custom Indicator.
//
//
// Indicator created by Sebastien Vezina.
// Github User: SebCoding
// September 2024

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;

namespace BarSpeed
{
    /// <summary>
    /// An example of blank indicator. Add your code, compile it and use on the charts in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// Code samples: https://github.com/Quantower/Examples
    /// </summary>
	public class BarSpeed : Indicator
    {
        [InputParameter("Font Color")]
        public Color FontColor = Color.LightGray;

        [InputParameter("Font Size", minimum: 6, maximum: 36)]
        public int FontSize = 10;

        [InputParameter("X Offset from Top Right", minimum: 0)]
        public int xOffset = 230;

        [InputParameter("Y Offset from Top Right", minimum: 0)]
        public int yOffset = 100;

        [InputParameter("Market Open Time")]
        public DateTime MarketOpenTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 30, 0, DateTimeKind.Local);

        [InputParameter("Market Close Time")]
        public DateTime MarketCloseTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 00, 0, DateTimeKind.Local);

        [InputParameter("Time Span In Minutes (within opening hours)", minimum: 1, maximum: 500)]
        public int TimeSpanInMin_Open = 10;

        [InputParameter("Time Span In Minutes (outside opening hours)", minimum: 1, maximum: 500)]
        public int TimeSpanInMin_Closed = 60;

        //[InputParameter("Invert Opening Hours")]
        //public bool InvertOpeningHours = false;

        // Display only time for the DatePickers of the input parameters
        public override IList<SettingItem> Settings
        {
            get
            {
                var settings = base.Settings;

                if (settings.GetItemByName("Market Open Time") is SettingItemDateTime openSi)
                {
                    openSi.Format = DatePickerFormat.Time;
                }

                if (settings.GetItemByName("Market Close Time") is SettingItemDateTime endSi)
                {
                    endSi.Format = DatePickerFormat.Time;
                }

                return settings;
            }
            set { base.Settings = value; }
        }


        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public BarSpeed()
            : base()
        {
            // Defines indicator's name and description.
            Name = "BarSpeed";
            Description = "Bar Speed for Tick Charts";

            // Defines line on demand with particular parameters.
            AddLineSeries("BarSpeed", Color.Yellow, 1, LineStyle.Solid);

            // By default indicator will be applied on separate window at the bottom of the chart
            SeparateWindow = false;

            // We use OnTick because we also want ATR/TR on current unclosed bar
            UpdateType = IndicatorUpdateType.OnBarClose;

            // We only need 2 digits for our calculated values
            Digits = 1;
        }

        protected override void OnInit() { }
        protected override void OnUpdate(UpdateArgs args) { }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            Graphics graphics = args.Graphics;
            var mainWindow = this.CurrentChart.MainWindow;

            Brush brush = new SolidBrush(FontColor);
            Font font = new Font("Consolas", FontSize, FontStyle.Regular);
            int textXCoord = mainWindow.ClientRectangle.Width - xOffset;
            int textYCoord = yOffset; // mainWindow.ClientRectangle.Height - 100;            

            // Check if it's tick chart, otherwise exit
            if (HistoricalData.Aggregation.Name != HistoryAggregation.TICK)
            {
                graphics.DrawString("The Bar Speed Indicator\nonly works on Tick Charts", font, Brushes.Red, textXCoord, textYCoord);
                return;
            }

            // We ignore current bar because it might not be closed yet
            DateTime currentBarDateTime = (HistoricalData[1] as HistoryItemTick).TimeLeft;

            DateTime MarketOpen = new DateTime(
                currentBarDateTime.Year, 
                currentBarDateTime.Month, 
                currentBarDateTime.Day,
                MarketOpenTime.Hour,
                MarketOpenTime.Minute, 0);

            DateTime MarketClose = new DateTime(
                currentBarDateTime.Year, 
                currentBarDateTime.Month, 
                currentBarDateTime.Day,
                MarketCloseTime.Hour,
                MarketCloseTime.Minute, 0);

            bool MarketIsOpen;
            int TimeRangeInMinutes;

            // It's a week day and the market is open
            if ((currentBarDateTime >= MarketOpen)
                && (currentBarDateTime <= MarketClose)
                && (currentBarDateTime.DayOfWeek != DayOfWeek.Saturday)
                && (currentBarDateTime.DayOfWeek != DayOfWeek.Sunday))
            {
                MarketIsOpen = true;
                TimeRangeInMinutes = TimeSpanInMin_Open;
                //if (InvertOpeningHours)
                //    TimeRangeInMinutes = TimeSpanInMin_Closed;
            }
            else
            {
                MarketIsOpen = false;
                TimeRangeInMinutes = TimeSpanInMin_Closed;
                //if (InvertOpeningHours)
                //    TimeRangeInMinutes = TimeSpanInMin_Open;
            }

            // Get Bars in the Time Span
            Symbol symbol = Core.Instance.Symbols.FirstOrDefault();
            HistoricalData historicalData = symbol.GetHistory(Period.TICK1, HistoryType.Last, currentBarDateTime.AddMinutes(-TimeRangeInMinutes), currentBarDateTime);

            double nbSecs = TimeRangeInMinutes * 60;
            double nbBars = historicalData.Count;

            // Bars Per Minute (BPM)
            double bpm = Math.Round(nbBars / (double)TimeRangeInMinutes, 1);
            double barDuration = (nbBars > 0) ? Math.Round(nbSecs / nbBars, 1) : 0;

            string barDurationStr = "";
            if (barDuration >= 60)
            {
                int remaingSecs = ((int)barDuration % 60);
                barDurationStr = Math.Floor(barDuration / 60) + "m " + ((remaingSecs >= 1) ? remaingSecs + "s" : "");
            }
            else
            {
                barDurationStr = barDuration + "s";
            }

            string message = "BAR SPEED [last " + TimeRangeInMinutes + "m]\n";
            if (barDuration > 0)
            {
                message += "1 bar = " + barDurationStr + "\n";
                message += "1 min = " + bpm + " bar" + ((bpm >= 2) ? "s" : "");
            }
            else
            {
                message += "Interval does not contain enough bars";
            }

            graphics.DrawString(message, font, brush, textXCoord, textYCoord);

            graphics.DrawString(currentBarDateTime.ToString(), font, Brushes.Yellow, textXCoord, textYCoord+50);

            graphics.DrawString($"Symbol: {symbol}", font, Brushes.Yellow, textXCoord, textYCoord+100);

        }
    }
}
