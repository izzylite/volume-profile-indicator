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
using System.IO;
using NinjaTrader.NinjaScript.AddOns.SightEngine;
using NinjaTrader.NinjaScript.AddOns.WyckoffRenderUtils;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
#endregion
 

namespace NinjaTrader.NinjaScript.AddOns
{
    namespace WyckoffVolumeUtils
    {
        public static class Debug { public static void toFile(string info) { File.AppendAllText(NinjaTrader.Core.Globals.UserDataDir + "Debug.txt", info + Environment.NewLine); } }

        public static class _VolumeAnalysisProfileEnums
        {
            public enum Formula
            {
                Total,
                Delta,
                BidAsk,
                TotalAndBidAsk,
                TotalAndDelta,
                TotalAndDeltaAndBidAsk
            }
            public enum RenderInfo
            {
                BidAsk,
                TotalAndDelta,
                Total,
                Delta
            }

            public enum OpacityType
            {
                Default,
                VolPercent
            }
            public enum RangeSize
            {
                Milli,
                Micro,
                Mini,
                Small,
                Medium,
                Large,
                Larger,
                ExtraLarge,
                ExtraExtraLarge,
                Maximum
            }

            public enum RangeType
            {
                Size,
                Aggregation
            }
        }
		
		
		
		
		

        public class WyckoffVolumeProfile : WyckoffRenderControl
        {
            private VolumeAnalysis.WyckoffBars wyckoffBars;
            private VolumeAnalysis.Profile marketVolumeProfile;
			private Dictionary<string,VolumeAnalysis.Profile> rangeVolumeProfile;
            
            private float POCOpacity;
            private float POIOpacity;
            private int timeFrame;
            private bool isRealtime;
            private DateTime beginTime;
            private Func<int, int> calculateBars;
            private Action<int, int, int, double, VolumeAnalysis.MarketOrder> renderVolumeFormula;
            private Action<int, VolumeAnalysis.MarketOrder, VolumeAnalysis.Profile.Ladder> renderVolumeInfo;
            private SharpDX.DirectWrite.TextFormat volumeTextFormat;
            private SharpDX.RectangleF Rect;
            private float minFontWidth;
            private float minFontHeight;
            private bool showTotalVolumeInfo;
            private bool showDeltaInfo;
            private bool showFont;
            private bool showPOC;
            private bool showPOI;
            private _VolumeAnalysisProfileEnums.OpacityType opacityType;
            private double vol_opacity; 
			private String pocId;
			 

            private SharpDX.Direct2D1.Brush gColor;
            private SharpDX.Direct2D1.Brush gColorFont;
			
            private SharpDX.Color colorBid;
            private SharpDX.Color colorAsk;
            private SharpDX.Color colorTotal;
            private SharpDX.Color colorPOC;
            private SharpDX.Color colorPOI;
            private SharpDX.Color colorFont;
            private float ladder_width_percentage;
            private _VolumeAnalysisProfileEnums.RangeSize range_size;
			
			private Instrument instrument;
			private double currentMarketPrice;
			private VolumeAnalysis.PeriodMode periodMode;
			public string debugMessage;
			
			
			public void setMarketPrice(double price){
				currentMarketPrice = price;
			}
		    

            public WyckoffVolumeProfile(bool flexible)
            {
                this.Rect = new SharpDX.RectangleF();
                this.isRealtime = false;
					if(flexible){
					this.rangeVolumeProfile = new Dictionary<string,VolumeAnalysis.Profile>();
				}
					
				
            }
			
			   
		    

            #region SETS
			
			public void setPocLineInfo(String pocId, Instrument instrument){ 
				this.pocId = pocId; 
				this.instrument = instrument;  
			}
 		 
			 
			 

            public void setFontStyle(SimpleFont font)
            {
                base.setFontStyle(font, out volumeTextFormat);
            }
            public void setShowFont(bool showFont, float minFontWidth, float minFontHeight)
            {
                this.showFont = showFont;
                this.minFontWidth = minFontWidth;
                this.minFontHeight = minFontHeight;
            }

            public void setRealtime(bool isRealtime) { this.isRealtime = isRealtime; }
            public bool IsRealtime { get { return this.isRealtime; } }
            // !- obtenemos las barras segun la temporalidad elegida
            public int getCalculatedBars(int period)
            {
                return this.calculateBars(period);
            }
            public void setWyckoffBars(VolumeAnalysis.WyckoffBars wyckoffBars)
            {
                this.wyckoffBars = wyckoffBars;
                // !- Valor del timeframe en el que estamos
                this.timeFrame = wyckoffBars.NT8Bars.BarsType.BarsPeriod.Value;
                //this.lastBar = wyckoffBars.NT8Bars.Count - 1;
            }
            public void setVolumeProfile(VolumeAnalysis.Profile marketVolumeProfile)
            {
                this.marketVolumeProfile = marketVolumeProfile; 
				
            }
			private  VolumeAnalysis.Profile getRangeVolumeProfile(string id){
				if(!rangeVolumeProfile.ContainsKey(id)){
					  VolumeAnalysis.Profile  rvp = new VolumeAnalysis.Profile(this.wyckoffBars); 
                rvp.setRealtimeCalculations(false); 
				rangeVolumeProfile.Add(id,rvp);
					return rvp;
				}
				return rangeVolumeProfile[id];
				
			}

           

            public void setProfileBidAskColor(Brush brushBidColor, Brush brushAskColor, Brush brushTotalColor)
            {
                this.colorBid = WyckoffRenderControl.BrushToColor(brushBidColor);
                this.colorAsk = WyckoffRenderControl.BrushToColor(brushAskColor);
                this.colorTotal = WyckoffRenderControl.BrushToColor(brushTotalColor);
            }
            public void setProfileCalculationColor(Brush brushPOCColor, Brush brushPOIColor)
            {
                this.colorPOC = WyckoffRenderControl.BrushToColor(brushPOCColor);
                this.colorPOI = WyckoffRenderControl.BrushToColor(brushPOIColor);
            }
          
            public void setFontColor(Brush brushFontColor)
            {
                this.colorFont = WyckoffRenderControl.BrushToColor(brushFontColor);
            }
            public void setCalculationsOpacity(float POCOpacity, float POIOpacity)
            {
                this.POCOpacity = POCOpacity / 100.0f;
                this.POIOpacity = POIOpacity / 100.0f;
            }
            public void setShowInfo(bool showTotalVolumeInfo, bool showDeltaInfo)
            {
                this.showTotalVolumeInfo = showTotalVolumeInfo;
                this.showDeltaInfo = showDeltaInfo;
            }
            public void setShowCalculations(bool showPOC, bool showPOI)
            {
                this.showPOC = showPOC;
                this.showPOI = showPOI;
            }

            public void setLadderWidthPercentage(float percentage)
            {
                this.ladder_width_percentage = percentage;
            }

            public void setRangeSize(_VolumeAnalysisProfileEnums.RangeSize size)
            {
                this.range_size = size;
            }

            public void setVolMainOpacity(_VolumeAnalysisProfileEnums.OpacityType type, double opacity)
            {
                this.opacityType = type;
                this.vol_opacity = opacity;
            }

            #endregion
            // !- debe ser cargada desde DataLoaded
            #region BARS_CALCULATION

            // !- Calculos solo usados para el render de rango de barras en la pantalla
            //			private int _defaultTotalBars(int bars){ return bars; }
            private int _TotalBarsByMinutes(int Minutes) { return Minutes / this.timeFrame; }
            private int _TotalBarsByHours(int Hours) { return (Hours * 60) / this.timeFrame; }
            private int _TotalBarsByDays(int Days) { return (Days * 24 * 60) / this.timeFrame; }

            private int _TotalBarsByWeeks(int Weeks) { return (Weeks * 5 * 24 * 60) / this.timeFrame; }

            private int _TotalBarsByMonths(int Months)
            {
                int totalMinutes = 0;

                for (int i = 0; i < Months; i++)
                {
                    int month = DateTime.Now.AddMonths(i).Month;
                    int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, month);
                    totalMinutes += daysInMonth * 24 * 60;
                }

                return totalMinutes / this.timeFrame;
            }
            public void setBarsPeriodFormula(VolumeAnalysis.PeriodMode periodMode)
            {
				this.periodMode = periodMode;
                switch (periodMode)
                {
                    //					case VolumeAnalysis.PeriodMode.Bars:
                    //					{
                    //						this.calculateBars = this._defaultTotalBars;
                    //						break;
                    //					}
                    case VolumeAnalysis.PeriodMode.Minutes:
                        {
                            this.calculateBars = this._TotalBarsByMinutes;
                            break;
                        }
                    case VolumeAnalysis.PeriodMode.Hours:
                        {
                            this.calculateBars = this._TotalBarsByHours;
                            break;
                        }
                    case VolumeAnalysis.PeriodMode.Days:
                        {
                            this.calculateBars = this._TotalBarsByDays;
                            break;
                        }
                    case VolumeAnalysis.PeriodMode.Weeks:
                        {
                            this.calculateBars = this._TotalBarsByWeeks;
                            break;
                        }
                    case VolumeAnalysis.PeriodMode.Months:
                        {
                            this.calculateBars = this._TotalBarsByMonths;
                            break;
                        }
                }
            }

            public double getRangeSize(_VolumeAnalysisProfileEnums.RangeSize category, _VolumeAnalysisProfileEnums.RangeType rangeType)
            {
                switch (category)
                {
                    case _VolumeAnalysisProfileEnums.RangeSize.Milli:
                        return 0;
                    case _VolumeAnalysisProfileEnums.RangeSize.Micro:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.1 : 0.0000001;
                    case _VolumeAnalysisProfileEnums.RangeSize.Mini:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.2 : 0.0000005;
                    case _VolumeAnalysisProfileEnums.RangeSize.Small:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.3 : 0.000001;
                    case _VolumeAnalysisProfileEnums.RangeSize.Medium:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.4 : 0.000005;
                    case _VolumeAnalysisProfileEnums.RangeSize.Large:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.5 : 0.00001;
                    case _VolumeAnalysisProfileEnums.RangeSize.Larger:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.6 : 0.000025;
                    case _VolumeAnalysisProfileEnums.RangeSize.ExtraLarge:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.7 : 0.00005;
                    case _VolumeAnalysisProfileEnums.RangeSize.ExtraExtraLarge:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.8 : 0.00006;
                    case _VolumeAnalysisProfileEnums.RangeSize.Maximum:
                        return rangeType == _VolumeAnalysisProfileEnums.RangeType.Size ? 1.9 : 0.00007;

                    default:
                        return getRangeSize(_VolumeAnalysisProfileEnums.RangeSize.Milli, rangeType);
                }
            }


            #endregion
            #region RANGE_PROFILE_KEY_ACTIVATION
			
			 
			
            public void OnCustomDrawingToolDataChanged(string id,PointsEventArgs e)
            {
                if (this.rangeVolumeProfile == null)
                    return;
				
				VolumeAnalysis.Profile  rvp = getRangeVolumeProfile(id);
				
				if(e==null){
					rvp.Clear();
					rangeVolumeProfile.Remove(id);
					return;
				}
                Point startPoint = e.Point1;
                Point endPoint = e.Point2;
                int mX1 = ChartingExtensions.ConvertToHorizontalPixels(startPoint.X, CHART_CONTROL.PresentationSource);
                int mX2 = ChartingExtensions.ConvertToHorizontalPixels(endPoint.X, CHART_CONTROL.PresentationSource);
				
                int firstBarX = CHART_BARS.GetBarIdxByX(CHART_CONTROL, (int)mX1);
                int lastBarX = CHART_BARS.GetBarIdxByX(CHART_CONTROL, (int)mX2); 
				rvp.Clear();
				
				
                if (Math.Abs(firstBarX - lastBarX) <= 1)
                {
                    return;
                }
                rvp.AddRangeProfile(Math.Min(firstBarX, lastBarX), Math.Max(firstBarX, lastBarX));
				
            }

            #endregion
            #region RENDER_VOLUME_INFO

            private bool _setVolumeInfo(int barY, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                if (this.showFont && this.W >= this.minFontWidth && this.H >= this.minFontHeight)
                {
                    int totalBars = profileLadder.TotalBars;
                    int startBar = profileLadder.StartBarIndex;

                    this.Rect.X = CHART_CONTROL.GetXByBarIndex(CHART_BARS, startBar) - (this.W / 2);
                    this.Rect.Y = barY - (this.H / 2f);
                    this.Rect.Width = totalBars + this.W;
                    this.Rect.Height = this.H;
                    return true;
                }
                return false;
            }
            private void _renderBidAskVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                if (this._setVolumeInfo(barY, profileLadder))
                {
                    myDrawText(string.Format("{0} x {1}", marketOrder.Bid, marketOrder.Ask), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
                }
            }
            private void _renderTotalDeltaVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                if (this._setVolumeInfo(barY, profileLadder))
                {
                    myDrawText(string.Format("{0} x {1}", marketOrder.Total, marketOrder.Delta), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
                }
            }
            private void _renderDeltaVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                if (this._setVolumeInfo(barY, profileLadder))
                {
                    myDrawText(marketOrder.Delta.ToString(), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
                }
            }
            private void _renderTotalVolumeInfo(int barY, VolumeAnalysis.MarketOrder marketOrder, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                if (this._setVolumeInfo(barY, profileLadder))
                {
                    myDrawText(marketOrder.Total.ToString(), ref Rect, colorFont, -1, -1, volumeTextFormat, 1.0f);
                }
            }
            public void setVolumeRenderInfo(_VolumeAnalysisProfileEnums.RenderInfo renderInfo)
            {
                switch (renderInfo)
                {
                    case _VolumeAnalysisProfileEnums.RenderInfo.BidAsk:
                        {
                            this.renderVolumeInfo = this._renderBidAskVolumeInfo;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.RenderInfo.Total:
                        {
                            this.renderVolumeInfo = this._renderTotalVolumeInfo;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.RenderInfo.Delta:
                        {
                            this.renderVolumeInfo = this._renderDeltaVolumeInfo;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.RenderInfo.TotalAndDelta:
                        {
                            this.renderVolumeInfo = this._renderTotalDeltaVolumeInfo;
                            break;
                        }
                }
            }

            #endregion
            #region RENDER_VOLUME_FORMULA

            private void _renderTotalVolume(
                int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                double volPercent = Math2.Percent(maxVolume, marketOrder.Total);
                this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
                myFillRectangle(ref Rect, colorTotal, opacityType == _VolumeAnalysisProfileEnums.OpacityType.Default ? (float)vol_opacity / 100.0f : (float)volPercent / 100f);
            }
            private void _renderDeltaVolume(
                int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                double delta = marketOrder.Delta;
                double volPercent = Math2.Percent(maxVolume, Math.Abs(delta));

                calculatePriceLadder(currentBar, totalBars, barY, opacityType == _VolumeAnalysisProfileEnums.OpacityType.Default ? (float)vol_opacity / 100.0f : volPercent);
                if (delta < 0)
                {
                    myFillRectangle(ref Rect, colorBid, 1.0f);
                }
                else
                {
                    myFillRectangle(ref Rect, colorAsk, 1.0f);
                }
            }
            private void _renderBidAskVolume(
                int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                double volPercent;

                volPercent = Math2.Percent(maxVolume, marketOrder.Bid);
                this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
                myFillRectangle(ref Rect, colorBid, opacityType == _VolumeAnalysisProfileEnums.OpacityType.Default ? (float)vol_opacity / 100.0f : (float)volPercent / 100f);

                volPercent = Math2.Percent(maxVolume, marketOrder.Ask);
                this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
                myFillRectangle(ref Rect, colorAsk, opacityType == _VolumeAnalysisProfileEnums.OpacityType.Default ? (float)vol_opacity / 100.0f : (float)volPercent / 100f);
            }
            private void _renderTotalAndBidAsk(int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                _renderTotalVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
                _renderBidAskVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
            }
            private void _renderTotalAndDelta(int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                _renderTotalVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
                _renderDeltaVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
            }
            private void _renderTotalAndDeltaAndBidAsk(int currentBar, int totalBars, int barY,
                double maxVolume, VolumeAnalysis.MarketOrder marketOrder)
            {
                _renderTotalVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
                _renderDeltaVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
                _renderBidAskVolume(currentBar, totalBars, barY, maxVolume, marketOrder);
            }

            public void setVolumeFormula(_VolumeAnalysisProfileEnums.Formula volumeFormula)
            {
                switch (volumeFormula)
                {
                    case _VolumeAnalysisProfileEnums.Formula.Total:
                        {
                            this.renderVolumeFormula = this._renderTotalVolume;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.Formula.Delta:
                        {
                            this.renderVolumeFormula = this._renderDeltaVolume;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.Formula.BidAsk:
                        {
                            this.renderVolumeFormula = this._renderBidAskVolume;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.Formula.TotalAndBidAsk:
                        {
                            this.renderVolumeFormula = this._renderTotalAndBidAsk;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.Formula.TotalAndDelta:
                        {
                            this.renderVolumeFormula = this._renderTotalAndDelta;
                            break;
                        }
                    case _VolumeAnalysisProfileEnums.Formula.TotalAndDeltaAndBidAsk:
                        {
                            this.renderVolumeFormula = this._renderTotalAndDeltaAndBidAsk;
                            break;
                        }
                }
            }

            #endregion
            #region RENDER_PROFILE

            public void renderMessageInfo(string textInfo, int X, int Y, SharpDX.Color4 textColor, float fontSize) //SharpDX.Color4 textLayoutColor, int fontSize)//SharpDX.Color.Beige, SharpDX.Color.White
            {
                SharpDX.Vector2 startPoint = new SharpDX.Vector2(X, Y);
                SharpDX.DirectWrite.TextFormat textFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory, "Arial", fontSize);
                SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory,
                    textInfo, textFormat, this.PanelW, this.PanelH);
                SharpDX.RectangleF msgRect = new SharpDX.RectangleF(startPoint.X, startPoint.Y,
                    textLayout.Metrics.Width, textLayout.Metrics.Height);
                SharpDX.Direct2D1.SolidColorBrush textDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RENDER_TARGET, textColor);

                // execute the render target draw rectangle with desired values
                //				if(textLayoutColor != null){
                //					SharpDX.Direct2D1.SolidColorBrush layoutDXBrush = new SharpDX.Direct2D1.SolidColorBrush(Target, textLayoutColor);
                //					Target.DrawRectangle(msgRect, layoutDXBrush);
                //					layoutDXBrush.Dispose();
                //				}
                // execute the render target text layout command with desired values
                RENDER_TARGET.DrawTextLayout(startPoint, textLayout, textDXBrush);

                textLayout.Dispose();
                textFormat.Dispose();
                textDXBrush.Dispose();
            }

            private void calculatePriceLadder(
                int currentBar, int totalBars, int barY,
                double volumePercent)
            {

                // !- Calculamos las barras que hay en el periodo de tiempo elejido
                int barXfrom = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar - totalBars);
                int _barXto = currentBar - totalBars - (int)Math.Round((volumePercent * totalBars) / 100);
                //R.Width = W;
                Rect.X = barXfrom - (this.W / 2f);
                Rect.Y = barY - (this.H / 2f);
                Rect.Width = (barXfrom - CHART_CONTROL.GetXByBarIndex(CHART_BARS, _barXto) + this.W) * (ladder_width_percentage / 100.0f);
                Rect.Height = this.H;
            }
			
		 
			
			
            // !- para POCs y POIs
            private void renderPO(
                int currentBar, int totalBars, int barY,
                double maxLadderVolume, double currentVolume, double totalVol, SharpDX.Color color, float opacity, bool realTime, bool range)
            {
                double volPercent = Math2.Percent(maxLadderVolume, currentVolume);
                // !- El valor maximo de volumen representa el 100%, el cual es el POC
                if (volPercent == 100)
                {
                    this.calculatePriceLadder(currentBar, totalBars, barY, volPercent);
                    myFillRectangle(ref Rect, color, opacity);
					if(!range){ 
						SharedPOCData.recordVolumePoc(pocId,currentMarketPrice,instrument,currentBar,realTime,CHART_SCALE,CHART_CONTROL,CHART_BARS,ref Rect,color,totalVol, periodMode);
					}
					// debugMessage = $"Recording {SharedPOCData.getLinkId(pocId,instrument)} hasLink={SharedPOCData.hasLink(pocId,instrument)} size is {SharedPOCData.pocByInstrument.Count}";
                }
            }
			
			
			 
			
			
            // !- informacion delta y total del profile
            private void renderProfileInfo(int currentBar, VolumeAnalysis.Profile.Ladder profileLadder)
            {
                int totalBars = profileLadder.TotalBars;
                //if( (this.W - 1) > totalBars ) return;
                int barXfrom = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar - totalBars);
                int barXto = CHART_CONTROL.GetXByBarIndex(CHART_BARS, currentBar);

                Rect.Width = barXto - barXfrom;
                Rect.Height = this.H;
                Rect.X = barXfrom;
                // !- Obtenemos el precio mas alto del perfil
                VolumeAnalysis.MarketOrder marketOrder = profileLadder.ProfileVolume;
                // !- Renderizamos la funcion correcta de texto(elejida por el usuario)
                if (this.showTotalVolumeInfo)
                {
                    //Rect.Y = chartScale.GetYByValue(vp.GetLadderHighPrice(currentBar)) - (this.H * 8);
                    Rect.Y = (CHART_SCALE.GetYByValue(profileLadder.HighPrice) - (this.H * 4f));
					string formatted = string.Format("V: {0:N0}", marketOrder.Total);
                    //Target.DrawText("V:", this.volumeTextFormat, Rect, brushFontColor.ToDxBrush(Target));
                    myDrawText(formatted, ref Rect, colorTotal, -1, -1, volumeTextFormat, 1.0f);
                }
                if (this.showDeltaInfo)
                {
                    Rect.Y = CHART_SCALE.GetYByValue(profileLadder.LowPrice) + (this.H * 4f);
                    //Target.DrawText("D:", this.volumeTextFormat, Rect, brushFontColor.ToDxBrush(Target));
                    long delta = marketOrder.Delta;
                    if (delta >= 0)
                    {
                        myDrawText(string.Format("D:{0}", delta), ref Rect, colorAsk, -1, -1, volumeTextFormat, 1.0f);
                    }
                    else
                    {
                        myDrawText(string.Format("D:{0}", delta), ref Rect, colorBid, -1, -1, volumeTextFormat, 1.0f);
                    }
                }
            }
            private void _renderProfile(int barIndex, VolumeAnalysis.Profile.Ladder profileLadder, int totalBars, bool realTime,bool rangeProfile)
            {
                VolumeAnalysis.MarketOrder mo;
                long totalVol;
                int barY;
                double price;
                double maxLadderVol = profileLadder.MaxVolume.Total;
                double minLadderVol = profileLadder.MinVolume.Total;
                double maxLadderPrice = profileLadder.MaxLadderPrice;
                double minLadderPrice = profileLadder.MinLadderPrice;
				double overAllTotalVol = profileLadder.ProfileVolume.Total;
                //Stopwatch sw = new Stopwatch(); sw.Start();
                foreach (var p in profileLadder)
                {
                    // !- Informacion de volumen del precio en el ladder
                    price = p.Key;
                    mo = p.Value;

                    barY = CHART_SCALE.GetYByValue(p.Key);
                    totalVol = mo.Total;
                    // !- Renderizamos el volumen segun la formula(Total, Bid, Ask, Delta, etc..)
                    renderVolumeFormula(barIndex, totalBars, barY, maxLadderVol, mo);
                    // !- Renderizamos el POC(Point of control)
                    /// REVISAR ESTO EN UN FUTURO...
                    if (this.showPOC && price == maxLadderPrice)
                    {
                        renderPO(barIndex, totalBars, barY, maxLadderVol, totalVol,overAllTotalVol, this.colorPOC, POCOpacity,realTime,rangeProfile);
                    }
                    // !- Renderizamos el POI(Point of imbalance)
                    if (this.showPOI && price == minLadderPrice)
                    {
                        renderPO(barIndex, totalBars, barY, minLadderVol, totalVol,overAllTotalVol, this.colorPOI, POIOpacity,realTime,rangeProfile);
                    }
                    // !- Renderizamos en texto la informacion de volumen total
                    renderVolumeInfo(barY, mo, profileLadder);
                }
                //				int barX = chartControl.GetXByBarIndex(chartBars, barIndex);
                //				Target.DrawLine(new SharpDX.Vector2(barX, chartScale.GetYByValue(pl.HighPrice)), new SharpDX.Vector2(barX, chartScale.GetYByValue(pl.LowPrice)), Brushes.Salmon.ToDxBrush(Target, 0.3f), this.W / 2f);
                //sw.Stop(); Print(string.Format("ms:{0}", sw.ElapsedMilliseconds));
                renderProfileInfo(barIndex, profileLadder);
				 
            }

            public void renderProfile(int barIndex)
            {
				
                if (this.marketVolumeProfile == null)
                {
                    return;
                }
                if (marketVolumeProfile.Exists(barIndex))
                {
                  /// !- we remove a bar from the front and one from the back, we don't need them (otherwise there would be a graph overflow)
                    // this happens in the .calculatePriceLadder(...) function since market time volume profile calculations require
                    // that the bar up to which the N-volume profile is calculated is always + 1 (that is, it ends where the next one begins)
                    _renderProfile(barIndex - 1, marketVolumeProfile.GetProfile(barIndex), marketVolumeProfile.TotalBars(barIndex) - 1,false,false);
                }
            }
            public void renderRealtimeProfile()
            {
                VolumeAnalysis.Profile.Ladder tmp = this.marketVolumeProfile.GetRealtimeProfile;
                if (tmp == null)
                {
                    return;
                }
                _renderProfile(this.wyckoffBars.CurrentBarIndex, tmp, tmp.TotalBars,true,false);
            }
            public void renderRangeProfile()
            {
                if (this.rangeVolumeProfile == null)
                {
                    return;
                }
				
                foreach (var rangeProfile in this.rangeVolumeProfile)
                {
					foreach (var vp in rangeProfile.Value)
                {
                    _renderProfile(vp.Key, vp.Value, vp.Value.TotalBars,false,true);
                }
				
                }
            }
			
			

            #endregion
        } // WyckoffVolumeProfile



    }
}