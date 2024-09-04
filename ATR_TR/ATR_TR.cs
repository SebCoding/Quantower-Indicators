// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using static System.Collections.Specialized.BitVector32;

namespace ATR_TR
{
    /// <summary>
    /// An example of blank indicator. Add your code, compile it and use on the charts in the assigned trading terminal.
    /// Information about API you can find here: http://api.quantower.com
    /// Code samples: https://github.com/Quantower/Examples
    /// </summary>
	public class ATR_TR : Indicator
    {
        private Indicator BuiltInATR;

        //[InputParameter("Ticks per Point for this Symbol")]
        //public double TicksPerPoint = 4;

        [InputParameter("Calculate True Range (TR) in Ticks")]
        public bool TRinTicks = true;

        [InputParameter("Calculate ATR in Ticks")]
        public bool ATRinTicks = true;

        [InputParameter("ATR Period")]
        public int ATR_Period = 14;

        [InputParameter("Print Current ATR on Chart")]
        public bool printATRStringonChart = true;

        [InputParameter("Print Current TR on Chart")]
        public bool printTRStringonChart = true;

        [InputParameter("Print ATR/TR x Offset from Top Right")]
        public int xOffset = 120;

        [InputParameter("Print ATR/TR y Offset from Top Right")]
        public int yOffset = 20;

        [InputParameter("ATR/TR Font Color")]
        public Color atrFontColor = Color.Turquoise;

        [InputParameter("ATR/TR Font Size")]
        public int atrFontSize = 10;


        /// <summary>
        /// Indicator's constructor. Contains general information: name, description, LineSeries etc. 
        /// </summary>
        public ATR_TR()
            : base()
        {
            // Defines indicator's name and description.
            Name = "ATR_TR";
            Description = "Customized ATR and True Range Indicator";

            // Defines line on demand with particular parameters.
            AddLineSeries("ATR", Color.CadetBlue, 1, LineStyle.Solid);
            AddLineSeries("TR", Color.Crimson, 1, LineStyle.Solid);

            // By default indicator will be applied on separate window at the bottom of the chart
            SeparateWindow = true;

            // We use OnTick because we also want ATR/TR on current unclosed bar
            UpdateType = IndicatorUpdateType.OnTick;

            // We only need 2 digits for our calculated values
            Digits = 2;
        }

        /// <summary>
        /// This function will be called after creating an indicator as well as after its input params reset or chart (symbol or timeframe) updates.
        /// </summary>
        protected override void OnInit()
        {
            // Add your initialization code here
            this.BuiltInATR = Core.Indicators.BuiltIn.ATR(ATR_Period, MaMode.SMA);

            // Add created ATR indicator as a child to our script
            AddIndicator(this.BuiltInATR);
        }

        /// <summary>
        /// Calculation entry point. This function is called when a price data updates. 
        /// Will be runing under the HistoricalBar mode during history loading. 
        /// Under NewTick during realtime. 
        /// Under NewBar if start of the new bar is required.
        /// </summary>
        /// <param name="args">Provides data of updating reason and incoming price.</param>
        protected override void OnUpdate(UpdateArgs args)
        {
            // Add your calculations here.         

            //
            // An example of accessing the prices          
            // ----------------------------
            //
            // double bid = Bid();                          // To get current Bid price
            // double open = Open(5);                       // To get open price for the fifth bar before the current
            // 

            //
            // An example of settings values for indicator's lines
            // -----------------------------------------------
            //            
            // SetValue(1.43);                              // To set value for first line of the indicator
            // SetValue(1.43, 1);                           // To set value for second line of the indicator
            // SetValue(1.43, 1, 5);                        // To set value for fifth bar before the current for second line of the indicator


            // ATR
            double atr = BuiltInATR.GetValue();
            if (ATRinTicks)
                this.SetValue(atr/ Symbol.TickSize, 0);
            else
                this.SetValue(atr, 0);

            // True Range
            double range = this.High() - this.Low();
            if (TRinTicks)
                this.SetValue(range / Symbol.TickSize, 1);
            else
                SetValue(range, 1);
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            if (printATRStringonChart || printTRStringonChart)
            {
                Graphics graphics = args.Graphics;
                var mainWindow = this.CurrentChart.MainWindow;

                // ATR
                double atr = BuiltInATR.GetValue();
                if (ATRinTicks)
                    atr = atr / Symbol.TickSize;
                string atr_str = atr.ToString("F2");

                // TR
                double tr = High() - Low();
                if (TRinTicks)
                    tr = tr / Symbol.TickSize;
                string tr_str = tr.ToString("F2");

                // Output Text to print on Chart
                string str = "";
                if (printATRStringonChart)
                    str = "ATR: " + atr_str + "\n";
                if (printTRStringonChart)
                    str += "Bar: " + tr_str;


                Font font = new Font("Consolas", atrFontSize, FontStyle.Regular);
                int textXCoord = mainWindow.ClientRectangle.Width - xOffset;
                int textYCoord = yOffset; // mainWindow.ClientRectangle.Height - 100;
                Brush brush = new SolidBrush(atrFontColor);

                graphics.DrawString(str, font, brush, textXCoord, textYCoord);

                // Use StringFormat class to center text
                //StringFormat stringFormat = new StringFormat()
                //{
                //    LineAlignment = StringAlignment.Center,
                //    Alignment = StringAlignment.Center
                //};
                //graphics.DrawString(str, font, brush, textXCoord, textYCoord, stringFormat);

                // Print to log for debugging
                //Core.Instance.Loggers.Log($"Printing ATR: {atr} now.");
            }

        }
    }
}
