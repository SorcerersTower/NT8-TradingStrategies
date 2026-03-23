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
namespace NinjaTrader.NinjaScript.Strategies.IRB
{
	public class EDRIB60V3 : Strategy
	{
private double 	_ibHigh;
        private double 	_ibLow;
        private double 	_ibRange;
        private bool 	_isIBSet;
        private bool 	_orderPlacedToday;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Quantity", Order=1, GroupName="Parameters")]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name="Max IB Size %", Order=2, GroupName="Parameters")]
        public double MaxIBSizePct { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Fixed IB60 Strategy with corrected Exit logic";
                Name                                        = "EDR_IB60_v3";
                Calculate                                   = Calculate.OnBarClose;
                EntriesPerDirection                         = 2;
                EntryHandling                               = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy                = true;
                ExitOnSessionCloseSeconds                   = 30;
                IsFillLimitOnTouch                          = true; // Better for Limit order testing
                StartBehavior                               = StartBehavior.WaitUntilFlat;
                TimeInForce                                 = TimeInForce.Gtc;
                
                Quantity                                    = 1;
                MaxIBSizePct                                = 1.5;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) return;

            // 1. Session Reset at 09:30
            if (ToTime(Time[0]) == 093000)
            {
                _ibHigh = High[0];
                _ibLow = Low[0];
                _isIBSet = false;
                _orderPlacedToday = false;
            }

            // 2. IB Range Calculation at 10:30
            if (ToTime(Time[0]) == 103000 && !_isIBSet)
            {
                // Re-calculating the High/Low of the first hour
                for (int i = 0; i <= 10; i++) // Check the last 10 bars (assuming ~5-6 min bars)
                {
                    _ibHigh = Math.Max(_ibHigh, High[i]);
                    _ibLow = Math.Min(_ibLow, Low[i]);
                }

                _isIBSet = true;
                _ibRange = _ibHigh - _ibLow;
                
                double ibPctOfPrice = (_ibRange / _ibHigh) * 100;
                if (ibPctOfPrice > MaxIBSizePct) _isIBSet = false; 
            }

            // 3. Trade Entry & Dynamic Exit Setting
            if (_isIBSet && !_orderPlacedToday && Position.MarketPosition == MarketPosition.Flat)
            {
                // --- LONG LOGIC ---
                if (High[0] > _ibHigh)
                {
                    double entryPrice = _ibHigh - (_ibRange * 0.1); 
                    double stopPrice  = entryPrice - (_ibRange * 0.4); 
                    double targetPrice2 = entryPrice + (_ibRange * 0.6);
					double targetPrice1 = entryPrice + (_ibRange * 0.25);
					
					
					
					Draw.Line(this, "ibr60high" + CurrentBar, false, 1, _ibHigh , -1, _ibHigh , Brushes.Orange, DashStyleHelper.Solid, 2, false);
					Draw.Line(this, "ibr60low" + CurrentBar, false, 1, _ibLow , -1, _ibLow , Brushes.Orange, DashStyleHelper.Solid, 2, false);
					
                    SetStopLoss("IB_Long 1", CalculationMode.Price, stopPrice, false);
					SetStopLoss("IB_Long 2", CalculationMode.Price, stopPrice, false);
					
                    SetProfitTarget("IB_Long 1", CalculationMode.Price, targetPrice1);
					SetProfitTarget("IB_Long 2", CalculationMode.Price, targetPrice2);

                    EnterLongLimit(0, true, Quantity, entryPrice, "IB_Long 1");
					EnterLongLimit(0, true, Quantity, entryPrice, "IB_Long 2");
                    _orderPlacedToday = true;
                }
                
                // --- SHORT LOGIC ---
                else if (Low[0] < _ibLow)
                {
                    double entryPrice = _ibLow + (_ibRange * 0.1); 
                    // CRITICAL FIX: The stop for a short must be ABOVE the entry price.
                    // Original logic: entryPrice + (_ibRange * 6.0)
                    double stopPrice  = entryPrice + (_ibRange * 0.6);
					double targetPrice1 = entryPrice - (_ibRange * 0.25);
                    double targetPrice2 = entryPrice - (_ibRange * 0.4);
					
					 EnterShortLimit(0, true, Quantity, entryPrice, "IB_Short 1");
					EnterShortLimit(0, true, Quantity, entryPrice, "IB_Short 2");
					
                    SetStopLoss("IB_Short 1", CalculationMode.Price, stopPrice, false);
					 SetStopLoss("IB_Short 2", CalculationMode.Price, stopPrice, false);
					
                    SetProfitTarget("IB_Short 1", CalculationMode.Price, targetPrice1);
					SetProfitTarget("IB_Short 2", CalculationMode.Price, targetPrice2);

                   
                    _orderPlacedToday = true;
                }
            }
        }
	}
}
