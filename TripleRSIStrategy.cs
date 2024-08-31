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
namespace NinjaTrader.NinjaScript.Strategies
{
	public class TripleRSIStrategy : Strategy //Version 1
	{
		private RSI rsi;
		private EMA ema200;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "TripleRSIStrategy";
				Calculate									= Calculate.OnPriceChange;
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
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				StopTicks									= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure)
			{
				//SetTrailStop(CalculationMode.Ticks, StopTicks);
			}
			else if(State == State.DataLoaded)
			{
				rsi = RSI(5,1);
				ema200 = EMA(200);
			}
		}

		protected override void OnBarUpdate()
		{
			if(CurrentBar < 200)
				return;
			
			if(rsi[0] < 30) //rule 1
			{
				//if(rsi[0] < rsi[1] && rsi[1] < rsi[2]) //rule 2
				if(Close[0] < Close[1] && Close[1] < Close[2])
				{
					if(rsi[2] < 60) //rule 3
					{
						if(Close[0] > ema200[0]) //rule 4
						{
							
							EnterLong(CurrentBar+"TripleRsi");
							SetStopLoss(CalculationMode.Ticks, 20);
						}
					}
				}
			}
			if(Position.MarketPosition == MarketPosition.Long)
			{
				if(rsi[0] >= 70)
					ExitLong(1);
			}
		}
		
		#region Properties

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "StopTicks", GroupName = "NinjaScriptStrategyParameters", Order = 1)]
		public int StopTicks
		{ get; set; }
		#endregion
	}
}
