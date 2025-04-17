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
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.AddOns.SightEngine; 
using NinjaTrader.NinjaScript.AddOns.WyckoffRenderUtils;
using NinjaTrader.NinjaScript.AddOns.WyckoffVolumeUtils;
using System.Collections;
using System.Collections.Specialized; 
using System.Globalization;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion
 
 
 
//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.WyckoffZen
{
	public class VolumeProfileLines : Indicator
	{
		 
		private double currentMarketPrice = 0; 
		private String uniqueId;
		private SharpDX.DirectWrite.Factory factory;
		private TextFormat textFormat;
		
		#region GLOBAL_VARIABLES

       

        #endregion
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				 
				Description = @"";
                Name = "Volume Profile Lines";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = false; 
				_RealTimeColor = Brushes.CornflowerBlue;
				_ExcludeLimit = 0;
				_PocMaxLine = 5;
				_PocStrokeWidth = 2;
				_ShowPocLines = false;
				_ShowTestedPocLines = true;
				_PocShowRealTimeLines = false; 
				_PocLineRangePercentage = 0.0; 
				 factory = new SharpDX.DirectWrite.Factory();
       			 textFormat = new TextFormat(factory, "Arial", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, 8);
				
				 
			}
			 else if (State == State.DataLoaded)
            {
                
                SetZOrder();
				 
               
            }
			else if (State == State.Configure)
			{ 
			uniqueId = SharedPOCData.linkData(SharedPOCData.getLinkId(_LinkId,Instrument),Guid.NewGuid().ToString());
		
			}
			else if (State == State.Terminated){
				SharedPOCData.removeLinkData(SharedPOCData.getLinkId(_LinkId,Instrument),uniqueId);
			}
			
		}
		
		private void SetZOrder()
		{
		  if (ChartPanel.ChartObjects.Count > 0)
		        {
		            ZOrder = ChartPanel.ChartObjects.Min(z => z.ZOrder) - 1;
		        }
		        else
		        {
		            ZOrder = 0;
		        }
		}

		protected override void OnBarUpdate()
        {
			currentMarketPrice = Close[0];
			
		}
		
		 protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {

			base.OnRender(chartControl, chartScale);
			try  {
					if(_ShowPocLines) { 
						renderPOCLines( chartScale, chartControl, RenderTarget); 
					}
                  
                }
                catch(Exception ex) {
				 Print("Error in rendering profile lines: " + ex.Message);	
				}
		
			// Print("Debug : " + wyckoffVP.debugMessage);
		}
	 


		 
			 
 
	public void renderPOCLines(ChartScale chartScale,ChartControl chartControl, SharpDX.Direct2D1.RenderTarget RENDER_TARGET){
	String id  = SharedPOCData.getLinkId(_LinkId, Instrument);
		int excludeCount = 0;
	var pocList = SharedPOCData.pocByInstrument.Cast<DictionaryEntry>().ToList();
		var realTimeColor = WyckoffRenderControl.BrushToColor(_RealTimeColor);
		var count = 0;
		    for (int i = 0; i < pocList.Count; i++)  {
				
			  	DictionaryEntry entry = pocList[i];
	        	SharedPOCData.POCData _pocData = (SharedPOCData.POCData)entry.Value;
				if((!_ShowTestedPocLines && _pocData.tested) || (_pocData.realTime && !_PocShowRealTimeLines) || (_pocData.linkId != id)) {
					continue;
				}
				bool isClose =   _pocData.isCloseToPrice(_pocData.price, currentMarketPrice, _PocLineRangePercentage);
			 	 if(((_EnablePriceRange && isClose) || !_EnablePriceRange) && count < _PocMaxLine) {
					 
					 if(_ExcludeLimit > 0 && excludeCount <= _ExcludeLimit){
						 excludeCount++;
						continue;
						}
					 SharedPOCData.drawLineWithLabel(_pocData,realTimeColor,_PocStrokeWidth, chartScale, chartControl, ChartPanel.W, RENDER_TARGET,factory,textFormat); 
					 count =  count + 1;
					}
				 
	}
				
	 
	}
		
		
		
		 
		
		[NinjaScriptProperty]
        [Display(Name = "Show Lines", Order = 0, GroupName = "POC Lines")]
        public bool _ShowPocLines
        { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Show Tested Lines", Order = 1, GroupName = "POC Lines")]
        public bool _ShowTestedPocLines { get; set; } = true;
		 
		[NinjaScriptProperty] 
		[Display(Name = "Show RealTime Lines", Order = 2, GroupName = "POC Lines")]
		public bool _PocShowRealTimeLines { get; set; }
		
		
		[XmlIgnore]
        [Display(Name = "RealTime Color", Order = 3, GroupName = "POC Lines")]
        public System.Windows.Media.Brush _RealTimeColor
        { get; set; }
		
		[Browsable(false)]
        public string _RealTimeColorSerializable
        {
            get { return Serialize.BrushToString(_RealTimeColor); }
            set { _RealTimeColor = Serialize.StringToBrush(value); }
        }
		 
		
		[NinjaScriptProperty] 
		[Display(Name = "Enable", Order = 0, GroupName = "POC Range")]
		public bool _EnablePriceRange { get; set; } = true;
		
		[NinjaScriptProperty]
		[Range(0.0, 3)]
		[Display(Name = "Range From Price %", Order = 1, GroupName = "POC Range")]
		public double _PocLineRangePercentage { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "Lines Limit", Order = 5, GroupName = "POC Lines")]
		public int _PocMaxLine { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 20)]
		[Display(Name = "Exclude Recent Line", Order = 6, GroupName = "POC Lines")]
		public int _ExcludeLimit { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Stroke Width", Order = 7, GroupName = "POC Lines")]
		public int _PocStrokeWidth { get; set; }
		
		[NinjaScriptProperty] 
		[Display(Name = "Volume Profile Poc Id", Order = 8, GroupName = "POC Lines")]
		public String _LinkId { get; set; } = String.Empty;
		 
		

	}
}







public static class SharedPOCData
{
	 
	
public class POCData {
	public String id;
	public String linkId;
	public double price;
	public DateTime time;  
	public String instrumentName;
	public bool realTime;
	public bool tested;
	public double maxVol;
	public SharpDX.Color color; 
	public VolumeAnalysis.PeriodMode periodMode;
	
	public POCData(double price, DateTime time,String instrumentName, SharpDX.Color color){
		this.price = price;
		this.time = time;  
		this.instrumentName = instrumentName; 
		this.color = color; 
		
	}
		
	
		
	
	
	public bool isCloseToPrice(double pocPrice, double currentPrice,double range){ 
	// Calculate the absolute difference between the mid price and the POC price
	double priceDifference =   Math.Abs(currentPrice  - pocPrice); 
	// Calculate the percentage difference relative to the POC price
	double percentageDifference = (priceDifference / pocPrice) * 100.0;  
	// Determine if the POC is close based on the percentage threshold
	return percentageDifference <= range;
	}
	}
	
	
	
    public static OrderedDictionary pocByInstrument = new OrderedDictionary();
	public static Dictionary<String,int> link = new Dictionary<String,int>();
	public static Dictionary<String,bool> temp = new Dictionary<String,bool>();
	
	
	 
	
	public static String linkData(String id,String uniqueId){ 
		if(!temp.ContainsKey(uniqueId)){
			temp.Add(uniqueId,true);
			updateCount(id,true);
		}
		return uniqueId;
		 
	}
	private static void updateCount(String id,bool add){ 
		 
		if(!link.ContainsKey(id)){
			link.Add(id,1);
		}
		else {
			int size = link.TryGetValue(id, out int c) ? c : 0; 
			link[id] = add? size + 1 : Math.Max(0, size - 1);
		}
	}
	
	
	
	public static String removeLinkData(String id,String uniqueId){
		if(temp.ContainsKey(uniqueId)){
		 	updateCount(id,false);
			temp.Remove(uniqueId);
		}
		return uniqueId;
	}
	
		public static void clear(String linkId, Instrument intrument)
		{  
			if(linkId==null){
				return;
			}
		    List<string> keysToRemove = new List<string>();  
		    foreach (DictionaryEntry entry in SharedPOCData.pocByInstrument)
		    {
		        SharedPOCData.POCData pocData = (SharedPOCData.POCData)entry.Value;
		        if (pocData.linkId == getLinkId(linkId,intrument))
		        {
		            keysToRemove.Add((string)entry.Key);
		        }
		    } 
		    foreach (string key in keysToRemove)
		    {
		        SharedPOCData.pocByInstrument.Remove(key);
		    }
			
		}
	
	public static bool hasLink(String linkId,Instrument instrument) {
		return link.ContainsKey(getLinkId(linkId,instrument));
	}
	 
	
	public static String getLinkId(String linkId,Instrument instrument){
		return $"{linkId}_{instrument.FullName}";
	}
	 

	public static void recordVolumePoc(String id,double currentMarketPrice, Instrument instrument,int currentBar,
		bool realTime,  ChartScale CHART_SCALE,ChartControl CHART_CONTROL,ChartBars CHART_BARS,
		ref SharpDX.RectangleF Rect,SharpDX.Color color, double maxVol, VolumeAnalysis.PeriodMode periodMode){
		

		string linkId = getLinkId(id,instrument);
		link.TryGetValue(linkId, out int linkCount);
		 
		if(linkCount > 0 && maxVol>5000){
		double pocPrice = CHART_SCALE.GetValueByY(Rect.Y); // Convert Y to price
		DateTime time = CHART_CONTROL.GetTimeByX((int)Rect.X); // Convert X to time
		time = time.AddMinutes(20);
		if(time.DayOfWeek == DayOfWeek.Sunday){
			return;
		}
		
		string pocId = string.Format("{0}_{1}_{2}",linkId,periodMode!=VolumeAnalysis.PeriodMode.Hours? time.Date.ToString("yyyy-MM-dd") : time.ToString(),periodMode.ToString());
			
			if(!pocByInstrument.Contains(pocId) || realTime){
				SharedPOCData.POCData pocData = new  SharedPOCData.POCData(pocPrice,time,instrument.FullName,color); 
				pocData.realTime = realTime;
				pocData.linkId = linkId;
				pocData.maxVol = maxVol;
				pocData.periodMode = periodMode;
				//$"{linkId}_{currentBar}";
				pocData.id = pocId;
				pocData.tested =  hasFuturePriceTestedPoc(pocData,CHART_BARS.Bars); 
				
				if (pocByInstrument.Contains(pocId))
			        pocByInstrument[pocId] = pocData;
		        else{
		            pocByInstrument.Add(pocId, pocData);
					pocByInstrument = GetSortedPOCByTimeDescending(pocByInstrument);
				}
			
			}
			else {
				SharedPOCData.POCData pocData = pocByInstrument[pocId] as SharedPOCData.POCData;
				pocData.price = pocPrice;
				pocData.time = time;
				pocData.maxVol = maxVol;
				pocData.realTime = realTime;
				pocByInstrument[pocId] = pocData;
			}
			
		}
		
	}
		
		 public static bool hasFuturePriceTestedPoc(SharedPOCData.POCData pocData, Bars _Bars)
			{
			    int pocIndex = _Bars.GetBar(pocData.time);
			    if (pocIndex < 0 || pocIndex >= _Bars.Count - 1) return false;
			
			    DateTime pocTime = pocData.time;
			    double pocPrice = pocData.price;
			
			    DateTime currentDay = DateTime.MinValue;
			    double dailyHigh = double.MinValue;
			    double dailyLow = double.MaxValue;
			
			    for (int i = pocIndex + 1; i < _Bars.Count; i++)
			    {
			        DateTime barTime = _Bars.GetTime(i);
			
			        // Prevents checking today's bars
			        if (barTime.Date == DateTime.Today)
			            return false;
			
			        bool isSamePeriod = false;
			
			        // Period relationship check
			        switch (pocData.periodMode)
			        {
			            case VolumeAnalysis.PeriodMode.Months:
			                isSamePeriod = barTime.Month == pocTime.Month && barTime.Year == pocTime.Year;
			                break;
			
			            case VolumeAnalysis.PeriodMode.Weeks:
			                Calendar calendar = CultureInfo.CurrentCulture.Calendar;
			                int pocWeek = calendar.GetWeekOfYear(pocTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
			                int barWeek = calendar.GetWeekOfYear(barTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
			                isSamePeriod = barWeek == pocWeek && barTime.Year == pocTime.Year;
			                break;
			
			            case VolumeAnalysis.PeriodMode.Days:
			                isSamePeriod = barTime.Date == pocTime.Date;
			                break;
			        }
			
			        // Skip bars from the original POC period
			        if (isSamePeriod) continue;
			
			        double barHigh = _Bars.GetHigh(i);
			        double barLow = _Bars.GetLow(i);
			
			        // New day detection
			        if (barTime.Date != currentDay)
			        {
			            // Check previous day's values before resetting
			            if (currentDay != DateTime.MinValue)
			            {
			                double alignment = (pocPrice / 100) * 0.1; // 0.1% tolerance
			                if (dailyHigh >= (pocPrice + alignment) && dailyLow <= (pocPrice - alignment))
			                    return true;
			            }
			
			            // Reset for new day
			            currentDay = barTime.Date;
			            dailyHigh = barHigh;
			            dailyLow = barLow;
			        }
			        else
			        {
			            // Update current day's extremes
			            dailyHigh = Math.Max(dailyHigh, barHigh);
			            dailyLow = Math.Min(dailyLow, barLow);
			        }
			    }
			
			    // Final day check
			    return dailyHigh >= pocPrice && dailyLow <= pocPrice;
			}
 

     
	
	public static void RemovePOC(string id)  {
            pocByInstrument.Remove(id); 
    }
	
	
	// This method returns a new OrderedDictionary with entries sorted by time (most recent first)
	public static OrderedDictionary GetSortedPOCByTimeDescending(OrderedDictionary pocByInstrument)
	{
	    // Cast each entry to DictionaryEntry and sort by the 'time' property in descending order.
	    var sortedEntries = pocByInstrument.Cast<DictionaryEntry>()
	        .OrderByDescending(de => ((POCData)de.Value).time)
	        .ToList();
	
	    // Create a new OrderedDictionary to hold the sorted entries.
	    OrderedDictionary sortedDict = new OrderedDictionary();
	    foreach (DictionaryEntry entry in sortedEntries)
	    {
	        sortedDict.Add(entry.Key, entry.Value);
	    }
	    return sortedDict;
	}

	 
   

public static void drawLineWithLabel(POCData pocData, Color4 realTimeColor, float strokeWidth, 
                                     ChartScale CHART_SCALE, ChartControl CHART_CONTROL,  
                                     float chartPanelWidth, SharpDX.Direct2D1.RenderTarget RENDER_TARGET,
									SharpDX.DirectWrite.Factory factory, TextFormat textFormat)
{
    float rectY = CHART_SCALE.GetYByValue(pocData.price); // Convert price to Y-pixel
    float rectX = CHART_CONTROL.GetXByTime(pocData.time); // Convert time to X-pixel

	string labelText = string.Format("{0} V:{1:N0} {2}",formatDate(pocData.time,pocData.periodMode), pocData.maxVol,pocData.realTime?"":pocData.tested?"✖":"✔");
	bool isLongLabel = labelText.Length > 10;
	Color4 color =  pocData.realTime? realTimeColor : pocData.color;
    using (var rayBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, color))
    {
        var strokeStyleProperties = new StrokeStyleProperties
        {
            DashStyle = SharpDX.Direct2D1.DashStyle.Custom,
            DashCap   = CapStyle.Flat,
            LineJoin  = LineJoin.Miter
        };

        float[] dashPattern = { 3f, 1f, 1f, 1f };
		
        using (var dashStrokeStyle = new StrokeStyle(RENDER_TARGET.Factory, strokeStyleProperties, dashPattern))
        {
            var startPoint = new Vector2(rectX, rectY);
            var endPoint   = new Vector2(chartPanelWidth - (isLongLabel? 105 : 65), rectY); // Prevents overflow

            RENDER_TARGET.DrawLine(startPoint, endPoint, rayBrush, strokeWidth, dashStrokeStyle);
        }

        // ======== ADD LABEL AT END OF LINE ========
      
        using (var textBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET,  color))  
        {
            
            float textX = chartPanelWidth - (isLongLabel? 100 : 60);  // Adjusted for visibility
            float textY = rectY - 7; // Center text vertically

            using (var layout = new TextLayout(factory, labelText, textFormat, (isLongLabel? 120 : 60), 20))
            {
                RENDER_TARGET.DrawTextLayout(new Vector2(textX, textY), layout, textBrush);
            }
        }
    }
}

 
	
		public static string formatDate(DateTime time, VolumeAnalysis.PeriodMode periodMode)
			{
			    DateTime now = DateTime.Now;
				
			    CultureInfo culture = CultureInfo.CurrentCulture;
				
			 if (periodMode == VolumeAnalysis.PeriodMode.Days)
		        {
		            int currentWeek = culture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		            int timeWeek = culture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
					string formatedDate = time.ToString("ddd", culture);
		            if (timeWeek == currentWeek) // Only format if it's in the same week
		                return formatedDate; // Example: "Mon", "Tue", etc.
					else  if (timeWeek == currentWeek - 1) // Previous week
		                return string.Format("{0} {1}","PW", formatedDate);;
		        }
			else if (periodMode == VolumeAnalysis.PeriodMode.Months)
		        {
		            if (time.Year == now.Year)
		                return time.ToString("MMM", culture); // Example: "Dec"
		            else
		                return time.ToString("yyyy. MMM", culture); // Example: "2023. Dec"
		        }
			 else if (periodMode == VolumeAnalysis.PeriodMode.Weeks)
		        {
		            int currentWeek = culture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		            int timeWeek = culture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		
	           if (timeWeek == currentWeek - 1){ // Previous week
		                return "PW";
		        }
				if (timeWeek == currentWeek){ // Previous week
		                return "CW";
		        }
			   
				}
			     return "";
			}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WyckoffZen.VolumeProfileLines[] cacheVolumeProfileLines;
		public WyckoffZen.VolumeProfileLines VolumeProfileLines(bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			return VolumeProfileLines(Input, _showPocLines, _showTestedPocLines, _pocShowRealTimeLines, _enablePriceRange, _pocLineRangePercentage, _pocMaxLine, _excludeLimit, _pocStrokeWidth, _linkId);
		}

		public WyckoffZen.VolumeProfileLines VolumeProfileLines(ISeries<double> input, bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			if (cacheVolumeProfileLines != null)
				for (int idx = 0; idx < cacheVolumeProfileLines.Length; idx++)
					if (cacheVolumeProfileLines[idx] != null && cacheVolumeProfileLines[idx]._ShowPocLines == _showPocLines && cacheVolumeProfileLines[idx]._ShowTestedPocLines == _showTestedPocLines && cacheVolumeProfileLines[idx]._PocShowRealTimeLines == _pocShowRealTimeLines && cacheVolumeProfileLines[idx]._EnablePriceRange == _enablePriceRange && cacheVolumeProfileLines[idx]._PocLineRangePercentage == _pocLineRangePercentage && cacheVolumeProfileLines[idx]._PocMaxLine == _pocMaxLine && cacheVolumeProfileLines[idx]._ExcludeLimit == _excludeLimit && cacheVolumeProfileLines[idx]._PocStrokeWidth == _pocStrokeWidth && cacheVolumeProfileLines[idx]._LinkId == _linkId && cacheVolumeProfileLines[idx].EqualsInput(input))
						return cacheVolumeProfileLines[idx];
			return CacheIndicator<WyckoffZen.VolumeProfileLines>(new WyckoffZen.VolumeProfileLines(){ _ShowPocLines = _showPocLines, _ShowTestedPocLines = _showTestedPocLines, _PocShowRealTimeLines = _pocShowRealTimeLines, _EnablePriceRange = _enablePriceRange, _PocLineRangePercentage = _pocLineRangePercentage, _PocMaxLine = _pocMaxLine, _ExcludeLimit = _excludeLimit, _PocStrokeWidth = _pocStrokeWidth, _LinkId = _linkId }, input, ref cacheVolumeProfileLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WyckoffZen.VolumeProfileLines VolumeProfileLines(bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			return indicator.VolumeProfileLines(Input, _showPocLines, _showTestedPocLines, _pocShowRealTimeLines, _enablePriceRange, _pocLineRangePercentage, _pocMaxLine, _excludeLimit, _pocStrokeWidth, _linkId);
		}

		public Indicators.WyckoffZen.VolumeProfileLines VolumeProfileLines(ISeries<double> input , bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			return indicator.VolumeProfileLines(input, _showPocLines, _showTestedPocLines, _pocShowRealTimeLines, _enablePriceRange, _pocLineRangePercentage, _pocMaxLine, _excludeLimit, _pocStrokeWidth, _linkId);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WyckoffZen.VolumeProfileLines VolumeProfileLines(bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			return indicator.VolumeProfileLines(Input, _showPocLines, _showTestedPocLines, _pocShowRealTimeLines, _enablePriceRange, _pocLineRangePercentage, _pocMaxLine, _excludeLimit, _pocStrokeWidth, _linkId);
		}

		public Indicators.WyckoffZen.VolumeProfileLines VolumeProfileLines(ISeries<double> input , bool _showPocLines, bool _showTestedPocLines, bool _pocShowRealTimeLines, bool _enablePriceRange, double _pocLineRangePercentage, int _pocMaxLine, int _excludeLimit, int _pocStrokeWidth, String _linkId)
		{
			return indicator.VolumeProfileLines(input, _showPocLines, _showTestedPocLines, _pocShowRealTimeLines, _enablePriceRange, _pocLineRangePercentage, _pocMaxLine, _excludeLimit, _pocStrokeWidth, _linkId);
		}
	}
}

#endregion
