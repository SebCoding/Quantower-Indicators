
// Quantower Custom Indicator.
//
// Combines Average True Range (ATR) and True Range (TR) into a single indicator.
// Offers the ability to calculate the ATR and/or the TR in points or ticks.
// Also allows to print on top left of the chart current values for ATR and TR for the ongoing unclosed bar.
//
// When Risk Calculation is enabled, it also calculates and prints the maximum number of contracts that should
// be used based on the max $ risk per trade and the size of the ATR, TR or signal bar + x ticks above and/or below
//
// Indicator created by Sebastien Vezina.
// Github User: SebCoding
// September 2024

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

        [InputParameter("Calculate True Range (TR) in Ticks")]
        public bool TRinTicks = true;

        [InputParameter("Calculate ATR in Ticks")]
        public bool ATRinTicks = true;

        [InputParameter("ATR Period", minimum: 1, increment: 1)]
        public int ATR_Period = 14;

        [InputParameter("Print Current ATR on Chart")]
        public bool printATRStringOnChart = true;

        [InputParameter("Print Previous Bar TR on Chart")]
        public bool printPrevTRStringOnChart = true;

        [InputParameter("Print Current TR on Chart")]
        public bool printTRStringOnChart = true;

        [InputParameter("X Offset from Top Right", minimum: 0)]
        public int xOffset = 230;

        [InputParameter("Y Offset from Top Right", minimum: 0)]
        public int yOffset = 10;

        [InputParameter("Font Color")]
        public Color fontColor = Color.LightGray;

        [InputParameter("Font Size", minimum: 6, maximum: 36)]
        public int fontSize = 10;

        // Risk Calculator
        [InputParameter("Print Risk Calculation")]
        public bool printRiskCalculation = true;

        [InputParameter("Maximum $ Risk per Trade", minimum: 0, maximum: 1000000, increment: 1, decimalPlaces: 0)]
        public double maxRisk = 75;

        [InputParameter("Additional ticks to add to the signal bar", minimum: 0)]
        public int riskAddTicks = 2;

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

            if (!printATRStringOnChart && !printTRStringOnChart && !printPrevTRStringOnChart)
                return;

            Graphics graphics = args.Graphics;
            var mainWindow = this.CurrentChart.MainWindow;

            int padding = 15;

            double riskInTicks;
            double tickCost = Symbol.GetTickCost(1);

            // ATR
            string atr_str;
            double atr = BuiltInATR.GetValue();
            riskInTicks = (atr / Symbol.TickSize) + riskAddTicks;
            double nbContractsATR = (atr > 0) ? (maxRisk / (riskInTicks * tickCost)): 0;
            if (ATRinTicks)
            {
                atr = atr / Symbol.TickSize;
                atr_str = $"ATR[{ATR_Period}]: " + atr.ToString("F1");
            }
            else
                atr_str = $"ATR[{ATR_Period}]: " + atr.ToString("F2");
            atr_str = atr_str.PadRight(padding);

            // Previous Bar TR75
            string prev_tr_str;
            double prev_tr = High(1) - Low(1);
            riskInTicks = (prev_tr / Symbol.TickSize) + riskAddTicks;
            double nbContractsPrevTR = (prev_tr > 0) ? (maxRisk / (riskInTicks * tickCost)): 0;
            if (TRinTicks)
            {
                prev_tr = prev_tr / Symbol.TickSize;
                prev_tr_str = "Previous: " + prev_tr.ToString("F0");
            }
            else
                prev_tr_str = "Previous: " + prev_tr.ToString("F2");
            prev_tr_str = prev_tr_str.PadRight(padding);

            // Current Bar TR
            string tr_str;
            double tr = High() - Low();
            riskInTicks = (tr / Symbol.TickSize) + riskAddTicks;
            double nbContractsTR = (tr > 0) ? (maxRisk / (riskInTicks * tickCost)): 0;
            if (TRinTicks)
            {
                tr = tr / Symbol.TickSize;
                tr_str = "Current: " + tr.ToString("F0");
            }
            else
                tr_str = "Current: " + tr.ToString("F2");
            tr_str = tr_str.PadRight(padding);


            // Create text to be printed on chart
            string str = "BarSize".PadRight(padding);
            if (printRiskCalculation)
                str += $"  Risk ${maxRisk}";
            str += "\n";
            //str += "".PadRight(str.Length-1, '-') + "\n";

            if (printATRStringOnChart) 
            { 
                str += atr_str;
                if (printRiskCalculation)
                    str += "|  " + nbContractsATR.ToString("F1");
                str += "\n";
            }

            if (printPrevTRStringOnChart)
            {
                str += prev_tr_str;
                if (printRiskCalculation)
                    str += "|  " + nbContractsPrevTR.ToString("F1");
                str += "\n";
            }

            if (printTRStringOnChart)
            {
                str += tr_str;
                if (printRiskCalculation)
                    str += "|  " + nbContractsTR.ToString("F1");
                str += "\n";
            }

            //if (printRiskCalculation)
            //{
            //    str += $"\nRisk ${maxRisk}\n";
            //    str += "ATR: " + nbContractsATR.ToString("F1") + "\n";
            //    str += "Prev TR: " + nbContractsPrevTR.ToString("F1") + "\n";
            //    str += "Curr TR: " + nbContractsTR.ToString("F1") + "\n";
            //}

            Font font = new Font("Consolas", fontSize, FontStyle.Regular);
            int textXCoord = mainWindow.ClientRectangle.Width - xOffset;
            int textYCoord = yOffset; // mainWindow.ClientRectangle.Height - 100;
            Brush brush = new SolidBrush(fontColor);

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
