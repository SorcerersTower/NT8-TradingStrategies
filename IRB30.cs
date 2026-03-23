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
	public class IRB30 : Strategy
	{

private Order longEntry  = null;
        private Order shortEntry = null;
        private Order stopOrder  = null;
        private Order limitOrder = null;

private int barsSinceOrder = 0;
        private int stopTicks      = 20;
        private int targetTicks    = 40;
        private bool tradePlacedToday = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "IRB30";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.High;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				IsUnmanaged = true;
				Contracts = 1;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			LastResortStopLoss();
			// Ensure we have enough bars to calculate the highest/lowest (lookback 7)
if (Bars.IsFirstBarOfSession) tradePlacedToday = false;

            if (CurrentBar < 7 || tradePlacedToday) return;

            // Trigger at 9:55 AM (Ensure your chart is on Exchange Time/EST)
            if (Time[0].Hour == 9 && Time[0].Minute == 55)
            {
                double high7 = MAX(High, 7)[0];
                double low7  = MIN(Low, 7)[0];

                double longPrice  = high7 + (TickSize * 2);
                double shortPrice = low7 - (TickSize * 2);

                // Use a unique OCO string so the broker handles the entry cancellation
                string entryOco = "EntryOCO_" + DateTime.Now.Ticks.ToString();

                SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, Contracts, 0, longPrice, entryOco, "LongEntryIrb30");
                SubmitOrderUnmanaged(0, OrderAction.SellShort, OrderType.StopMarket, Contracts, 0, shortPrice, entryOco, "ShortEntryIrb30");

                barsSinceOrder = 1;
                tradePlacedToday = true; 
            }

            // Cancel entry orders if not filled within 5 bars
            if (barsSinceOrder > 5 && Position.MarketPosition == MarketPosition.Flat)
            {
                if (longEntry != null)  CancelOrder(longEntry);
                if (shortEntry != null) CancelOrder(shortEntry);
                barsSinceOrder = 0;
            }

            if (barsSinceOrder > 0) barsSinceOrder++;
			
			
        }
		
protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, 
            int quantity, int filled, double averageFillPrice, 
            OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            // 1. Track Entry Orders
            if (order.Name == "LongEntryIrb30") longEntry = order;
            if (order.Name == "ShortEntryIrb30") shortEntry = order;

            // 2. Handle Entry Fill -> Submit Exit Bracket
            if (orderState == OrderState.Filled)
            {
                string exitOco = "ExitOCO_" + DateTime.Now.Ticks.ToString();

                if (order.Name == "LongEntryIrb30")
                {
                    stopOrder  = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.StopMarket, Contracts, 0, averageFillPrice - (stopTicks * TickSize), exitOco, "Stop");
                    limitOrder = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Limit, Contracts, averageFillPrice + (targetTicks * TickSize),0, exitOco, "Target");
                }
                else if (order.Name == "ShortEntryIrb30")
                {
                    stopOrder  = SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.StopMarket, Contracts, 0, averageFillPrice + (stopTicks * TickSize), exitOco, "Stop");
                    limitOrder = SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.Limit, Contracts, averageFillPrice - (targetTicks * TickSize), 0,exitOco, "Target");
                }

                // 3. Handle Exit Fill -> Cancel the other side of the bracket
                // If the 'Stop' or 'Target' fills, the OCO string above handles the cancellation automatically 
                // on most brokers, but we null them out here for safety.
                if (order.Name == "Stop" || order.Name == "Target")
                {
                    stopOrder = null;
                    limitOrder = null;
                }
            }
        }
		
		        private void LastResortStopLoss()
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (Math.Abs(Position.GetUnrealizedProfitLoss(PerformanceUnit.Ticks, Close[0])) > stopTicks)
                {
                    if (Close[0] < Position.AveragePrice)
                    {
                        if (longEntry != null)
                        {
                            SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Market, 1);

                            if (stopOrder != null)
                                CancelOrder(stopOrder);
                            if (limitOrder != null)
                                CancelOrder(limitOrder);
                        }
                    }

                }
            }
            if (Position.MarketPosition == MarketPosition.Short )
            {
                if (Math.Abs(Position.GetUnrealizedProfitLoss(PerformanceUnit.Ticks, Close[0])) > stopTicks)
                {
                    if (Close[0] > Position.AveragePrice)
                    {
                        if (shortEntry != null)
                        {
                            SubmitOrderUnmanaged(0, OrderAction.BuyToCover, OrderType.Market, 1);

                            if (stopOrder != null)
                                CancelOrder(stopOrder);
                            if (limitOrder != null)
                                CancelOrder(limitOrder);
                        }
                    }

                }
            }
        }
		
		
				        [NinjaScriptProperty]
        [Display(Name = "Contracts", Description = "# Contracts", Order = 1, GroupName = "Parameters")]
        public int Contracts
        { get; set; }
		
	}
	
	
}
