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
namespace NinjaTrader.NinjaScript.Strategies.Trend
{
	public class B34Strategy : Strategy
	{
	
		
		private EMA ema34;
		private EMA ema89;
		private EMA ema200;
		
		private double stopPrice;
        private double trailAmount;
        private Order entryOrderId;
        private Order stopOrderId;
		private Order profitOrderId;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "B34Strategy";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				IsUnmanaged									= true;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information

			}
			else if (State == State.Configure)
			{
				trailAmount =5 * TickSize;
			}
			else if (State == State.DataLoaded)
			{
				ema34 = EMA(34);
				ema89 = EMA(89);
				ema200 = EMA(200);
			}
		}

		protected override void OnBarUpdate()
		{
//			Print(string.Format("{0},{1},{2},{3}",Open[0], High[0], Low[0], Close[0]));
			if (Position.MarketPosition == MarketPosition.Flat)
            {
				bool isEma89Long = Low[0] <= ema89[0] &&  Open[0] > ema89[0] && Close[0] > Open[0];
				bool isEma34Long = Low[0] <= ema34[0] &&  Open[0] > ema34[0] && Close[0] > Open[0];
				bool isEma200Long = Low[0] <= ema200[0] &&  Open[0] > ema200[0] && Close[0] > Open[0];
				
				bool isEma34Short = High[0] >= ema34[0] &&  Open[0] < ema34[0] && Close[0] < Open[0];
				bool isEma89Short = High[0] >= ema89[0] &&  Open[0] < ema89[0] && Close[0] < Open[0];
				bool isEma200Short = High[0] >= ema200[0] &&  Open[0] < ema200[0] && Close[0] < Open[0];
				
			
				
				if(isEma200Long || isEma34Long || isEma89Long) //Buy
				{
					if(Open[1] > Close[1])
					{
						entryOrderId = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.MIT, 1, 0, Close[0],"", "entryOrder");
                		stopPrice = Close[0] - trailAmount;
                		stopOrderId = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.StopMarket, 1, 0, stopPrice, "", "stopOrder");	
					}
				
				}
				
				if(isEma200Short || isEma34Short || isEma89Short) //Buy
				{
					if(Close[1]>Open[1])
					{
						entryOrderId = SubmitOrderUnmanaged(0, OrderAction.Sell, OrderType.Market, 1, 0, 0,"", "entryOrder");
                		stopPrice = Close[0] + trailAmount;
                		stopOrderId = SubmitOrderUnmanaged(0, OrderAction.Buy, OrderType.StopMarket, 1, 0, stopPrice, "", "stopOrder");	
					}
				}
			}
			
			HandleTrailingStopVersionOne();
		}
		
		private void HandleTrailingStopVersionOne()
		{
			if (Position.MarketPosition == MarketPosition.Long)
            {
                double newStop = Close[0] - trailAmount;
                if (newStop > stopPrice)
                {
                    stopPrice = newStop;
                    ChangeOrder(stopOrderId, 1,0, stopPrice);
                }
            }
			if (Position.MarketPosition == MarketPosition.Short)
            {
                double newStop = Close[0] + trailAmount;
                if (newStop < stopPrice)
                {
                    stopPrice = newStop;
                    ChangeOrder(stopOrderId, 1,0, stopPrice);
                }
            }
		}
		

		

		
	}
}
