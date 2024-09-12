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
        private string outputText;

        [InputParameter ("Font Color", sortIndex: 0)]
        public Color FontColor = Color.LightGray;

        [InputParameter("Font Size", sortIndex:1, minimum: 6, maximum: 36)]
        public int FontSize = 10;

        [InputParameter("Text Location", sortIndex:2, variants: new object[]{
            "TopLeft", TextLocation.TopLeft,
            "TopRight", TextLocation.TopRight,
            "BottomLeft", TextLocation.BottomLeft,
            "BottomRight", TextLocation.BottomRight
        })]
        public TextLocation textLocation = TextLocation.BottomLeft;

        [InputParameter("Text Pos. X Offset (+/- values)", sortIndex: 3)]
        public int xOffset = 15;

        [InputParameter("Text Pos. Y Offset (+/- values)", sortIndex: 4)]
        public int yOffset = -60;

        [InputParameter("Market Open Time", sortIndex: 5)]
        public DateTime MarketOpenTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 30, 0, DateTimeKind.Local);

        [InputParameter("Market Close Time", sortIndex: 6)]
        public DateTime MarketCloseTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 17, 00, 0, DateTimeKind.Local);

        [InputParameter("Time Span In Minutes (within opening hours)", sortIndex: 7, minimum: 1, maximum: 500)]
        public int TimeSpanInMin_Open = 10;

        [InputParameter("Time Span In Minutes (outside opening hours)", sortIndex: 8, minimum: 1, maximum: 500)]
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
            AddLineSeries("BarSpeed", Color.Transparent, 1, LineStyle.Solid);

            // By default indicator will be applied on separate window at the bottom of the chart
            SeparateWindow = false;

            // We use OnTick because we also want ATR/TR on current unclosed bar
            UpdateType = IndicatorUpdateType.OnBarClose;

            // We only need 2 digits for our calculated values
            Digits = 1;

            
        }

        protected override void OnInit() { }
        protected override void OnUpdate(UpdateArgs args) 
        {
            if (HistoricalData == null || HistoricalData.Count <= 0)
                return;

            // Check if it's tick chart, otherwise exit
            if (HistoricalData.Aggregation.Name != HistoryAggregation.TICK)
            {
                outputText = "BAR SPEED\nThe Bar Speed Indicator\nonly works on Tick Charts";
                return;
            }

            if (UpdateType != IndicatorUpdateType.OnBarClose)
            {
                outputText = $"BAR SPEED\nUpdateType is set to {UpdateType}.\nPlease change it to OnBarClose.";
                return;
            }

            // We use local time to determine if market is open or closed
            DateTime currentBarDateTimeLocal = this.Time(0).ToLocalTime();

            DateTime MarketOpenLocal = new DateTime(
                currentBarDateTimeLocal.Year,
                currentBarDateTimeLocal.Month,
                currentBarDateTimeLocal.Day,
                MarketOpenTime.Hour,
                MarketOpenTime.Minute, 0).ToLocalTime();

            DateTime MarketCloseLocal = new DateTime(
                currentBarDateTimeLocal.Year,
                currentBarDateTimeLocal.Month,
                currentBarDateTimeLocal.Day,
                MarketCloseTime.Hour,
                MarketCloseTime.Minute, 0).ToLocalTime();

            bool MarketIsOpen;
            int TimeRangeInMinutes;

            // It's a week day and the market is open
            if ((currentBarDateTimeLocal >= MarketOpenLocal)
                && (currentBarDateTimeLocal <= MarketCloseLocal)
                && (currentBarDateTimeLocal.DayOfWeek != DayOfWeek.Saturday)
                && (currentBarDateTimeLocal.DayOfWeek != DayOfWeek.Sunday))
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

            //outputText = $"MarketIsOpen: {MarketIsOpen}\n MarketOpen: {MarketOpenLocal.ToString()}\nMarketClose: {MarketCloseLocal.ToString()}";
            //return;

            // Get Nb Bars in the Time Span
            int TimeRangeInSeconds = TimeRangeInMinutes * 60;
            Symbol symbol = Core.Instance.Symbols.FirstOrDefault();
            var tickhistoricalData = this.Symbol.GetHistory(new HistoryRequestParameters()
            {
                Symbol = this.Symbol,
                FromTime = this.Time(0).AddSeconds(-TimeRangeInSeconds),
                ToTime = this.Time(0),
                HistoryType = HistoryType.Last,
                Aggregation = HistoricalData.Aggregation,
                ForceReload = false
            });

            int nbBars = tickhistoricalData.Count;

            //int n = 0;
            //int count = tickhistoricalData.Count;
            //outputText = $"tickhistoricalData[{n}]: {tickhistoricalData[n].TimeLeft.ToLocalTime().ToString()}\nCount: {count}";
            //outputText += $"\nTimeSpanMin {TimeRangeInMinutes}";
            //outputText += $"\nFromTime: {this.Time(0).AddSeconds(-TimeRangeInSeconds).ToString()}";
            //outputText += $"\nToTime: {this.Time(0).ToString()}";

            // Bars Per Minute (BPM)
            double bpm = Math.Round(nbBars / (double)TimeRangeInMinutes, 1);
            double barDuration = (nbBars > 0) ? Math.Round((double)TimeRangeInSeconds / nbBars, 1) : 0;

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

            outputText = "BAR SPEED [last " + TimeRangeInMinutes + "m]\n";
            if (barDuration > 0)
            {
                outputText += "1 bar = " + barDurationStr + "\n";
                outputText += "1 min = " + bpm + " bar" + ((bpm >= 2) ? "s" : "");
            }
            else
            {
                outputText += "Interval does not contain enough bars";
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            Graphics graphics = args.Graphics;
            var mainWindow = this.CurrentChart.MainWindow;

            Brush brush = new SolidBrush(FontColor);
            Font font = new Font("Consolas", FontSize, FontStyle.Regular);

            int textCoordX = 0;
            int textCoordY = 0;
            switch (textLocation) 
            {
                case TextLocation.TopLeft:
                    {
                        textCoordX = xOffset;
                        textCoordY = yOffset;
                        break;
                    }
                case TextLocation.TopRight:
                    {
                        textCoordX = mainWindow.ClientRectangle.Width + xOffset;
                        textCoordY = yOffset;
                        break;
                    }
                case TextLocation.BottomRight:
                    {
                        textCoordX = mainWindow.ClientRectangle.Width + xOffset;
                        textCoordY = mainWindow.ClientRectangle.Height + yOffset;
                        break;
                    }
                case TextLocation.BottomLeft:
                default:
                    {
                        textCoordX = xOffset;
                        textCoordY = mainWindow.ClientRectangle.Height + yOffset;
                        break;
                    }
            }

            // Drawing on Chart
            graphics.DrawString(outputText, font, brush, textCoordX, textCoordY);
        }
    }

    public enum TextLocation
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
        
}
