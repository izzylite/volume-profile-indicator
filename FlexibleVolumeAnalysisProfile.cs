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
#endregion

using NinjaTrader.NinjaScript.AddOns.SightEngine;
using NinjaTrader.NinjaScript.AddOns.WyckoffRenderUtils;
using NinjaTrader.NinjaScript.AddOns.WyckoffVolumeUtils;


namespace NinjaTrader.NinjaScript.Indicators.WyckoffZen
{
    public class FlexibleVolumeAnalysisProfile : Indicator
    {
        
        #region GLOBAL_VARIABLES

        private VolumeAnalysis.WyckoffBars wyckoffBars; 
        private WyckoffVolumeProfile wyckoffVP;
		private bool IsAttachToVolumeRegion;
		public static bool IsIndicatorActive { get; private set; }

        #endregion
        #region INDICATOR_SETUP
		public VolumeAnalysis.PeriodMode _PeriodMode = VolumeAnalysis.PeriodMode.Days;
        public int _Period = 1;

        private void setStyle()
        {
            _TotalVolColor = Brushes.CornflowerBlue;
            _AskVolColor = Brushes.Green;
            _BidVolColor = Brushes.Red;
            _POCColor = Brushes.Tan;
            _POIColor = Brushes.Indigo; 

            _FontColor = Brushes.LightYellow;
            // *- 90%
            _POCOpacity = 90f;
            _Vol_Opacity = 90f;
            // *- 80%
            _POIOpacity = 80f;
            _TextOpacity = 100f;

        }
        private void setCalculations()
        {
            
            _VolumeFormula = _VolumeAnalysisProfileEnums.Formula.TotalAndBidAsk;
            _VolumeRenderInfo = _VolumeAnalysisProfileEnums.RenderInfo.Total;
             
            _showTotalVolumeInfo = true;
            _showDeltaInfo = true;
            _ShowPOC = true;
            _ShowPOI = true; 
        }
        private void setFontStyle()
        {
            _TextFont = new SimpleFont();
            _TextFont.Family = new FontFamily("Arial");
            _TextFont.Size = 10f;
            _TextFont.Bold = false;
            _TextFont.Italic = false;
            _ShowText = true;
            _MinFontWidth = 1f;
            _MinFontHeight = 8f;
        }

        #endregion
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"";
                Name = "Flexible Volume Analysis Profile";
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
                _OpacityType = _VolumeAnalysisProfileEnums.OpacityType.Default;
                _Vol_Opacity = 90f;  

                setStyle();
                setCalculations();
                setFontStyle();

                wyckoffVP = new WyckoffVolumeProfile(true);
				 
            }
            else if (State == State.Configure)
            {
				
				IsIndicatorActive = true;
                wyckoffVP.setProfileBidAskColor(_BidVolColor, _AskVolColor, _TotalVolColor);
                wyckoffVP.setProfileCalculationColor(_POCColor, _POIColor); 
                wyckoffVP.setFontStyle(_TextFont);
                wyckoffVP.setFontColor(_FontColor);
                wyckoffVP.setShowFont(_ShowText, _MinFontWidth, _MinFontHeight);

                wyckoffVP.setShowCalculations(_ShowPOC, _ShowPOI);
                wyckoffVP.setVolumeRenderInfo(_VolumeRenderInfo);
                wyckoffVP.setVolMainOpacity(_OpacityType, _Vol_Opacity);


                wyckoffVP.setCalculationsOpacity(_POCOpacity, _POIOpacity);
                wyckoffVP.setVolumeFormula(_VolumeFormula);
                wyckoffVP.setShowInfo(_showTotalVolumeInfo, _showDeltaInfo);
                wyckoffVP.setLadderWidthPercentage(100.0f);
                wyckoffVP.setRangeSize(_RangeSize);
				

                // !- Seteamos la formula correcta para calcular las barras totales en cada perfil de volumen
                wyckoffVP.setBarsPeriodFormula(_PeriodMode);

                Calculate = Calculate.OnBarClose;
            }
            else if (State == State.DataLoaded)
            {
                wyckoffBars = new VolumeAnalysis.WyckoffBars(Bars);
                wyckoffVP.setWyckoffBars(wyckoffBars);

                /// !- menor a 6 barras el volume profile da error
                if (wyckoffVP.getCalculatedBars(_Period) < 6)
                {
                    wyckoffBars = null;
                    return;
                }
 
                wyckoffVP.setFontStyle(_TextFont);  
                SetZOrder();
            }
            else if (State == State.Realtime)
            {
                if (wyckoffBars != null)
                    wyckoffVP.setRealtime(true);  
            }
			else if (State == State.Terminated){
				DetachVolumeRegionDrawingTools();
			}
			 
            else if (State == State.Terminated)
            {
                IsIndicatorActive = false;
            }
           
        }
		
		 

		 public void OnCustomDrawingToolDataChanged(object sender, PointsEventArgs e){ 
			 if(wyckoffVP!=null){
				 
		 	  wyckoffVP.OnCustomDrawingToolDataChanged(((VolumeProfileRegion)sender).Id,e); 
			 }
		 }
		
		  private void AttachVolumeRegionDrawingTools() {
			   
            // Access the chart control
            if (IsAttachToVolumeRegion || ChartControl == null)
                return;
			  // Get all chart panels
            var chartPanels = ChartControl.ChartPanels;
            foreach (var panel in chartPanels) {
                // Get all objects on the chart panel
                var chartObjects = panel.ChartObjects; 
                foreach (var chartObject in chartObjects)
                {
                    if (chartObject is DrawingTool drawingTool)
                    {
                        if (drawingTool is VolumeProfileRegion volumeProfileRegion)
                        {
							 Print("Attach Drawing Tool: " + drawingTool.Name);
							 volumeProfileRegion.DataChanged += OnCustomDrawingToolDataChanged;
							IsAttachToVolumeRegion = true;
                        }
                    }
                }
            }
				
        }
		  
		   private void DetachVolumeRegionDrawingTools() {
            // Access the chart control
            if (IsAttachToVolumeRegion || ChartControl == null)
                return;
			  // Get all chart panels
            var chartPanels = ChartControl.ChartPanels;
            foreach (var panel in chartPanels) {
                // Get all objects on the chart panel
                var chartObjects = panel.ChartObjects; 
                foreach (var chartObject in chartObjects)
                {
                    if (chartObject is DrawingTool drawingTool)
                    { 
						 
                        if (drawingTool is VolumeProfileRegion volumeProfileRegion)
                        {
							Print("Detach Drawing Tool: " + drawingTool.Name);
                            volumeProfileRegion.DataChanged -= OnCustomDrawingToolDataChanged;
							IsAttachToVolumeRegion = false;
                        }
                    }
                }
            }
				
        }
		  private void SetZOrder()
		{
		    if (_BehindBars)
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
		    else
		    { 
				ZOrder = ChartPanel.ChartObjects.Max(z => z.ZOrder) + 1;
		    }
		}
 
        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
			 if(_BehindBars)
             ZOrder = ChartPanel.ChartObjects.Min(z => z.ZOrder) -1;
            base.OnRender(chartControl, chartScale);
            if (wyckoffBars == null)
            {
                wyckoffVP.setChartPanelHW(ChartPanel.H, ChartPanel.W);
                wyckoffVP.setRenderTarget(chartControl, chartScale, ChartBars, RenderTarget);
                wyckoffVP.renderMessageInfo(string.Format("Bars number error:{0} minimum required for volume profile:6", wyckoffVP.getCalculatedBars(_Period)), ChartPanel.W / 3, ChartPanel.H / 2, SharpDX.Color.Beige, 14);
            }
            if (!wyckoffVP.IsRealtime || IsInHitTest == null || chartControl == null || ChartBars.Bars == null)
                return;
            // 1- Altura minima de un tick
            // 2- Ancho de barra en barra 

            wyckoffVP.setHW(chartScale.GetPixelsForDistance(TickSize), chartControl.Properties.BarDistance);

            wyckoffVP.setChartPanelHW(ChartPanel.H, ChartPanel.W);
            // !- Apuntamos al target de renderizado
            wyckoffVP.setRenderTarget(chartControl, chartScale, ChartBars, RenderTarget);

             
            wyckoffVP.renderRangeProfile();
			 AttachVolumeRegionDrawingTools();
             
        }

        protected override void OnMarketData(MarketDataEventArgs MarketArgs)
        {
            if (wyckoffBars == null)
            {
                return;
            }
            if (!wyckoffBars.onMarketData(MarketArgs,  wyckoffVP.getRangeSize(_RangeSize, _VolumeAnalysisProfileEnums.RangeType.Aggregation)))
            {
                return;
            }
           
        }

        #region Properties

        // !- Setup
        [NinjaScriptProperty]
        [Display(Name = "Formula", Order = 0, GroupName = "Volume Profile Calculations")]
        public _VolumeAnalysisProfileEnums.Formula _VolumeFormula
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ladder information", Order = 1, GroupName = "Volume Profile Calculations")]
        public _VolumeAnalysisProfileEnums.RenderInfo _VolumeRenderInfo
        { get; set; }

         
         


        [NinjaScriptProperty]
        [Display(Name = "Show POC", Order = 4, GroupName = "Volume Profile Calculations")]
        public bool _ShowPOC
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show POI", Order = 5, GroupName = "Volume Profile Calculations")]
        public bool _ShowPOI
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Aggregate volume", Order = 6, GroupName = "Volume Profile Calculations")]
        public _VolumeAnalysisProfileEnums.RangeSize _RangeSize { get; set; }

          [NinjaScriptProperty]
        [Display(Name = "Behind Bars", Order = 7, GroupName = "Volume Profile Calculations")]
        public bool _BehindBars
        { get; set; }    
     

        // !- Style	
        [NinjaScriptProperty]
        [Display(Name = "Histogram opacity type", Order = 0, GroupName = "Volume Profile Style")]
        public _VolumeAnalysisProfileEnums.OpacityType _OpacityType { get; set; }

        [NinjaScriptProperty]
        [Range(1.0f, 100.0f)]
        [Display(Name = "Histogram opacity %", Description = "Opacity value for Default Opacity Type", Order = 1, GroupName = "Volume Profile Style")]
        public double _Vol_Opacity { get; set; }

         

        [XmlIgnore]
        [Display(Name = "Total volume color", Order = 3, GroupName = "Volume Profile Style")]
        public Brush _TotalVolColor
        { get; set; }
        [Browsable(false)]
        public string _TotalVolColorSerializable
        {
            get { return Serialize.BrushToString(_TotalVolColor); }
            set { _TotalVolColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "Bid volume color", Order = 4, GroupName = "Volume Profile Style")]
        public Brush _BidVolColor
        { get; set; }
        [Browsable(false)]
        public string _BidVolColorSerializable
        {
            get { return Serialize.BrushToString(_BidVolColor); }
            set { _BidVolColor = Serialize.StringToBrush(value); }
        }
        [XmlIgnore]
        [Display(Name = "Ask volume color", Order = 5, GroupName = "Volume Profile Style")]
        public Brush _AskVolColor
        { get; set; }
        [Browsable(false)]
        public string _AskVolColorSerializable
        {
            get { return Serialize.BrushToString(_AskVolColor); }
            set { _AskVolColor = Serialize.StringToBrush(value); }
        }

        // *- POC color style
        [XmlIgnore]
        [Display(Name = "POC color", Order = 6, GroupName = "Volume Profile Style")]
        public Brush _POCColor
        { get; set; }
        [Browsable(false)]
        public string _POCColorSerializable
        {
            get { return Serialize.BrushToString(_POCColor); }
            set { _POCColor = Serialize.StringToBrush(value); }
        }
        [NinjaScriptProperty]
        [Range(1.0f, 100.0f)]
        [Display(Name = "POC opacity %", Order = 7, GroupName = "Volume Profile Style")]
        public float _POCOpacity
        { get; set; }

        // *- POI color style
        [XmlIgnore]
        [Display(Name = "POI color", Order = 8, GroupName = "Volume Profile Style")]
        public Brush _POIColor
        { get; set; }
        [Browsable(false)]
        public string _POIColorSerializable
        {
            get { return Serialize.BrushToString(_POIColor); }
            set { _POIColor = Serialize.StringToBrush(value); }
        }
        [NinjaScriptProperty]
        [Range(1.0f, 100.0f)]
        [Display(Name = "POI opacity %", Order = 9, GroupName = "Volume Profile Style")]
        public float _POIOpacity
        { get; set; }

        

        [XmlIgnore]
        [Display(Name = "Font Color", Order = 14, GroupName = "Volume Profile Style")]
        public Brush _FontColor
        { get; set; }
        [Browsable(false)]
        public string _FontColorSerializable
        {
            get { return Serialize.BrushToString(_FontColor); }
            set { _FontColor = Serialize.StringToBrush(value); }
        }
        [NinjaScriptProperty]
        [Range(1.0f, 100.0f)]
        [Display(Name = "Text opacity %", Order = 15, GroupName = "Volume Profile Style")]
        public float _TextOpacity
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Font", Order = 16, GroupName = "Volume Profile Style")]
        public SimpleFont _TextFont
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show text", Order = 17, GroupName = "Volume Profile Style")]
        public bool _ShowText
        { get; set; }

        [NinjaScriptProperty]
        [Range(1.0f, float.MaxValue)]
        [Display(Name = "Min font width", Order = 18, GroupName = "Volume Profile Style")]
        public float _MinFontWidth
        { get; set; }
        [NinjaScriptProperty]
        [Range(1.0f, float.MaxValue)]
        [Display(Name = "Min font height", Order = 19, GroupName = "Volume Profile Style")]
        public float _MinFontHeight
        { get; set; }

        // !- Info
        [NinjaScriptProperty]
        [Display(Name = "Total volume", Order = 0, GroupName = "Volume Profile Information")]
        public bool _showTotalVolumeInfo
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta", Order = 1, GroupName = "Volume Profile Information")]
        public bool _showDeltaInfo
        { get; set; }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WyckoffZen.FlexibleVolumeAnalysisProfile[] cacheFlexibleVolumeAnalysisProfile;
		public WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(_VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			return FlexibleVolumeAnalysisProfile(Input, _volumeFormula, _volumeRenderInfo, _showPOC, _showPOI, _rangeSize, _behindBars, _opacityType, _vol_Opacity, _pOCOpacity, _pOIOpacity, _textOpacity, _textFont, _showText, _minFontWidth, _minFontHeight, _showTotalVolumeInfo, _showDeltaInfo);
		}

		public WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(ISeries<double> input, _VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			if (cacheFlexibleVolumeAnalysisProfile != null)
				for (int idx = 0; idx < cacheFlexibleVolumeAnalysisProfile.Length; idx++)
					if (cacheFlexibleVolumeAnalysisProfile[idx] != null && cacheFlexibleVolumeAnalysisProfile[idx]._VolumeFormula == _volumeFormula && cacheFlexibleVolumeAnalysisProfile[idx]._VolumeRenderInfo == _volumeRenderInfo && cacheFlexibleVolumeAnalysisProfile[idx]._ShowPOC == _showPOC && cacheFlexibleVolumeAnalysisProfile[idx]._ShowPOI == _showPOI && cacheFlexibleVolumeAnalysisProfile[idx]._RangeSize == _rangeSize && cacheFlexibleVolumeAnalysisProfile[idx]._BehindBars == _behindBars && cacheFlexibleVolumeAnalysisProfile[idx]._OpacityType == _opacityType && cacheFlexibleVolumeAnalysisProfile[idx]._Vol_Opacity == _vol_Opacity && cacheFlexibleVolumeAnalysisProfile[idx]._POCOpacity == _pOCOpacity && cacheFlexibleVolumeAnalysisProfile[idx]._POIOpacity == _pOIOpacity && cacheFlexibleVolumeAnalysisProfile[idx]._TextOpacity == _textOpacity && cacheFlexibleVolumeAnalysisProfile[idx]._TextFont == _textFont && cacheFlexibleVolumeAnalysisProfile[idx]._ShowText == _showText && cacheFlexibleVolumeAnalysisProfile[idx]._MinFontWidth == _minFontWidth && cacheFlexibleVolumeAnalysisProfile[idx]._MinFontHeight == _minFontHeight && cacheFlexibleVolumeAnalysisProfile[idx]._showTotalVolumeInfo == _showTotalVolumeInfo && cacheFlexibleVolumeAnalysisProfile[idx]._showDeltaInfo == _showDeltaInfo && cacheFlexibleVolumeAnalysisProfile[idx].EqualsInput(input))
						return cacheFlexibleVolumeAnalysisProfile[idx];
			return CacheIndicator<WyckoffZen.FlexibleVolumeAnalysisProfile>(new WyckoffZen.FlexibleVolumeAnalysisProfile(){ _VolumeFormula = _volumeFormula, _VolumeRenderInfo = _volumeRenderInfo, _ShowPOC = _showPOC, _ShowPOI = _showPOI, _RangeSize = _rangeSize, _BehindBars = _behindBars, _OpacityType = _opacityType, _Vol_Opacity = _vol_Opacity, _POCOpacity = _pOCOpacity, _POIOpacity = _pOIOpacity, _TextOpacity = _textOpacity, _TextFont = _textFont, _ShowText = _showText, _MinFontWidth = _minFontWidth, _MinFontHeight = _minFontHeight, _showTotalVolumeInfo = _showTotalVolumeInfo, _showDeltaInfo = _showDeltaInfo }, input, ref cacheFlexibleVolumeAnalysisProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(_VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			return indicator.FlexibleVolumeAnalysisProfile(Input, _volumeFormula, _volumeRenderInfo, _showPOC, _showPOI, _rangeSize, _behindBars, _opacityType, _vol_Opacity, _pOCOpacity, _pOIOpacity, _textOpacity, _textFont, _showText, _minFontWidth, _minFontHeight, _showTotalVolumeInfo, _showDeltaInfo);
		}

		public Indicators.WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(ISeries<double> input , _VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			return indicator.FlexibleVolumeAnalysisProfile(input, _volumeFormula, _volumeRenderInfo, _showPOC, _showPOI, _rangeSize, _behindBars, _opacityType, _vol_Opacity, _pOCOpacity, _pOIOpacity, _textOpacity, _textFont, _showText, _minFontWidth, _minFontHeight, _showTotalVolumeInfo, _showDeltaInfo);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(_VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			return indicator.FlexibleVolumeAnalysisProfile(Input, _volumeFormula, _volumeRenderInfo, _showPOC, _showPOI, _rangeSize, _behindBars, _opacityType, _vol_Opacity, _pOCOpacity, _pOIOpacity, _textOpacity, _textFont, _showText, _minFontWidth, _minFontHeight, _showTotalVolumeInfo, _showDeltaInfo);
		}

		public Indicators.WyckoffZen.FlexibleVolumeAnalysisProfile FlexibleVolumeAnalysisProfile(ISeries<double> input , _VolumeAnalysisProfileEnums.Formula _volumeFormula, _VolumeAnalysisProfileEnums.RenderInfo _volumeRenderInfo, bool _showPOC, bool _showPOI, _VolumeAnalysisProfileEnums.RangeSize _rangeSize, bool _behindBars, _VolumeAnalysisProfileEnums.OpacityType _opacityType, double _vol_Opacity, float _pOCOpacity, float _pOIOpacity, float _textOpacity, SimpleFont _textFont, bool _showText, float _minFontWidth, float _minFontHeight, bool _showTotalVolumeInfo, bool _showDeltaInfo)
		{
			return indicator.FlexibleVolumeAnalysisProfile(input, _volumeFormula, _volumeRenderInfo, _showPOC, _showPOI, _rangeSize, _behindBars, _opacityType, _vol_Opacity, _pOCOpacity, _pOIOpacity, _textOpacity, _textFont, _showText, _minFontWidth, _minFontHeight, _showTotalVolumeInfo, _showDeltaInfo);
		}
	}
}

#endregion
