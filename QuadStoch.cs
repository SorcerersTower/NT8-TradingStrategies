#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.Momentum
{
    public class QuadStoch : Strategy
    {
        private Stochastics stoch9;
        private Stochastics stoch14;
        private Stochastics stoch40;
        private Stochastics stoch60;

        private bool longSetup, shortSetup = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "QuadStoch";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
            }
            else if (State == State.Configure)
            {

            }

            else if (State == State.DataLoaded)
            {
                stoch9 = Stochastics(3, 9, 1);
                stoch14 = Stochastics(3, 14, 1);
                stoch40 = Stochastics(4, 40, 1);
                stoch60 = Stochastics(10, 60, 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 60)
                return;


            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (longSetup || shortSetup)
                {
                    if (longSetup)
                    {
                        if (stoch14.D[1] < 20 && stoch14.D[0] > 20)
                        {
                            EnterLong("LongEntry");
                            SetStopLoss(CalculationMode.Ticks, 20);
                            longSetup = false;
                        }
                    }
                    if (shortSetup)
                    {
                        if (stoch14.D[1] > 80 && stoch14.D[0] < 80)
                        {
                            EnterShort("ShortEntry");
                            SetStopLoss(CalculationMode.Ticks, 20);
                            shortSetup = false;
                        }
                    }
                }
                else
                {
                    if (stoch9.D[0] < 20 && stoch14.D[0] < 20 && stoch40.D[0] < 20 && stoch60.D[0] < 20)
                    {
                        longSetup = true;

                        Draw.ArrowUp(this, CurrentBar + "Long", false, 0, Low[0] - TickSize * 10, Brushes.Gold);

                    }
                    else if (stoch9.D[0] > 80 && stoch14.D[0] > 80 && stoch40.D[0] > 80 && stoch60.D[0] > 80)
                    {

                        shortSetup = true;
                        Draw.ArrowDown(this, CurrentBar + "Long", false, 0, High[0] + TickSize * 10, Brushes.Gold);
                    }
                }
            }

            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (stoch14.D[0] > 80)
                {
                    ExitLong();
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                if (stoch14.D[0] < 20)
                {
                    ExitShort();
                }
            }

        }
    }
}
