// 
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using SharpDX.DirectWrite;
using NinjaTrader.NinjaScript.Indicators.WyckoffZen;
#endregion

namespace NinjaTrader.NinjaScript.DrawingTools
{ 
	 
	 
public class PointsEventArgs : EventArgs
    {
        public Point Point1 { get; }
        public Point Point2 { get; }

        public PointsEventArgs(Point point1, Point point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
    }

	[CLSCompliant(false)]
	public abstract class VolumeProfileRegionBase : DrawingTool
	{
		private int areaOpacity;
		private Brush areaBrush;
		private readonly DeviceBrush areaBrushDevice = new DeviceBrush();
		private const double cursorSensitivity = 15;
		private ChartAnchor editingAnchor;
		private bool hasSetZOrder;
		 [XmlIgnore]
		public PointsEventArgs RecentPointEventArgs;
		 [XmlIgnore]
        private SharpDX.RectangleF headerRect;
        private const float headerHeight = 20f; // Height of the header
		 
		public event EventHandler<PointsEventArgs> DataChanged;
		 [XmlIgnore]
		private SharpDX.RectangleF  closeButtonRect;

		public override bool SupportsAlerts { get { return true; } }

		public override IEnumerable<ChartAnchor> Anchors
		{
			get { return new[] { StartAnchor, EndAnchor }; }
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolRiskRewardAnchorLineStroke", GroupName = "NinjaScriptGeneral", Order = 5)]
		public Stroke AnchorLineStroke { get; set; }

		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
		public Brush AreaBrush
		{
			get { return areaBrush; }
			set
			{
				areaBrush = value;
				if (areaBrush != null)
				{
					if (areaBrush.IsFrozen)
						areaBrush = areaBrush.Clone();
					areaBrush.Opacity = areaOpacity / 100d;
					areaBrush.Freeze();
				}
				areaBrushDevice.Brush = null;
			}
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get { return Serialize.BrushToString(AreaBrush); }
			set { AreaBrush = Serialize.StringToBrush(value); }
		}

		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 4)]
		public int AreaOpacity
		{
			get { return areaOpacity; }
			set
			{
				areaOpacity = Math.Max(0, Math.Min(100, value));
				areaBrushDevice.Brush = null;
			}
		}

		[Display(Order = 2)]
		public ChartAnchor EndAnchor { get; set; }



		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
		public Stroke OutlineStroke { get; set; }

		[Display(Order = 1)]
		public ChartAnchor StartAnchor { get; set; }

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (areaBrushDevice != null)
				areaBrushDevice.RenderTarget = null;
			DataChanged?.Invoke(this,null);
			DataChanged = null;
		}

		public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
		{
			yield return new AlertConditionItem
			{
				Name = Custom.Resource.NinjaScriptDrawingToolRegion,
				ShouldOnlyDisplayName = true,
			};
		}

		public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
		{
			switch (DrawingState)
			{
				case DrawingState.Building: return Cursors.Pen;
				case DrawingState.Editing: return IsLocked ? Cursors.No : Cursors.SizeWE;
				case DrawingState.Moving: return IsLocked ? Cursors.No : Cursors.SizeAll;
				default:
					Point startAnchorPixelPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
					ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (closest != null)
					{
						if (IsLocked)
							return Cursors.Arrow;
						return Cursors.SizeWE;
					}

					Point endAnchorPixelPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
					Vector totalVector = endAnchorPixelPoint - startAnchorPixelPoint;
					if (MathHelper.IsPointAlongVector(point, startAnchorPixelPoint, totalVector, cursorSensitivity))
						return IsLocked ? Cursors.Arrow : Cursors.SizeAll;

					// check if cursor is along region edges
					foreach (Point anchorPoint in new[] { startAnchorPixelPoint, endAnchorPixelPoint })
					{

						if (Math.Abs(anchorPoint.X - point.X) <= cursorSensitivity)
							return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
					}
					
					return null;
			}
		}

		public override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
		{
			ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

			double middleX = chartPanel.X + chartPanel.W / 2;
			double middleY = chartPanel.Y + chartPanel.H / 2;
			Point midPoint = new Point((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
			return new[] { startPoint, midPoint, endPoint }.Select(p => new Point(p.X, middleY)).ToArray();
		}

		public override IEnumerable<Condition> GetValidAlertConditions()
		{
			return new[] { Condition.CrossInside, Condition.CrossOutside };
		}

		public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values,
													ChartControl chartControl, ChartScale chartScale)
		{
			double minPrice = Anchors.Min(a => a.Price);
			double maxPrice = Anchors.Max(a => a.Price);
			DateTime minTime = Anchors.Min(a => a.Time);
			DateTime maxTime = Anchors.Max(a => a.Time);

			// note, time region higlight x will always be a cross from moving linearly. until someone builds a time machine anyway
			// no need for lookback/cross check so just check first (most recent) value

			DateTime vt = values[0].Time;
			return condition == Condition.CrossInside ? vt > minTime && vt <= maxTime : vt > minTime && vt < maxTime;


		}

		public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
		{
			if (DrawingState == DrawingState.Building)
				return true;

			if (Anchors.Any(a => a.Time >= firstTimeOnChart && a.Time <= lastTimeOnChart))
				return true;
			// check crossovers
			if (StartAnchor.Time <= firstTimeOnChart && EndAnchor.Time >= lastTimeOnChart)
				return true;
			if (EndAnchor.Time <= firstTimeOnChart && StartAnchor.Time >= lastTimeOnChart)
				return true;
			return false;


		}

		public override void OnCalculateMinMax()
		{
			MinValue = double.MaxValue;
			MaxValue = double.MinValue;

			if (!IsVisible)
				return;

			foreach (ChartAnchor anchor in Anchors)
			{
				MinValue = Math.Min(anchor.Price, MinValue);
				MaxValue = Math.Max(anchor.Price, MaxValue);
			}
		}

		public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			
            
			
		 FindVolumeAnalysisProfileIndicator(chartControl);
			switch (DrawingState)
			{
				case DrawingState.Building:


					dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2;

					if (StartAnchor.IsEditing)
					{
						dataPoint.CopyDataValues(StartAnchor);
						StartAnchor.IsEditing = false;
						dataPoint.CopyDataValues(EndAnchor);
					}
					else if (EndAnchor.IsEditing)
					{

						dataPoint.Price = StartAnchor.Price;

						dataPoint.CopyDataValues(EndAnchor);
						EndAnchor.IsEditing = false;
					}
					if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
					{
						DrawingState = DrawingState.Normal;
						IsSelected = false;
					}
					break;
				case DrawingState.Normal: 
					Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
					editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
					if (editingAnchor != null)
					{
						editingAnchor.IsEditing = true;
						DrawingState = DrawingState.Editing;
					}
					else
					{
						 if (closeButtonRect.Contains((float)point.X,(float)point.Y))
							System.Windows.Forms.SendKeys.SendWait("{DELETE}");
						 else if (headerRect.Contains((float)point.X,(float)point.Y))
                            DrawingState = DrawingState.Moving;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeAll)
							DrawingState = DrawingState.Moving;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeWE || GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.SizeNS)
							DrawingState = DrawingState.Editing;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == Cursors.Arrow)
							DrawingState = DrawingState.Editing;
						else if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
							IsSelected = false;
					}
					break;
			}
		}

		public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (IsLocked && DrawingState != DrawingState.Building)
				return;
			if (DrawingState == DrawingState.Building && EndAnchor.IsEditing)
			{
				dataPoint.Price = chartScale.MinValue + chartScale.MaxMinusMin / 2;
				dataPoint.CopyDataValues(EndAnchor);
			}
			else if (DrawingState == DrawingState.Editing && editingAnchor != null)
				dataPoint.CopyDataValues(editingAnchor);
			else if (DrawingState == DrawingState.Moving)
				foreach (ChartAnchor anchor in Anchors)
					anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
		}

		public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
		{
			if (DrawingState == DrawingState.Building)
				return;

			DrawingState = DrawingState.Normal;
			editingAnchor = null;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{ 
				Name = "Volume Profile Region"; 
				AnchorLineStroke = new Stroke(Brushes.DarkGray, DashStyleHelper.Dash, 1f);
				AreaBrush = Brushes.Goldenrod;
				AreaOpacity = 25;
				DrawingState = DrawingState.Building;
				EndAnchor = new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorEnd, IsEditing = true, DrawingTool = this };
				OutlineStroke = new Stroke(Brushes.Goldenrod, 2f);
				StartAnchor = new ChartAnchor { DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorStart, IsEditing = true, DrawingTool = this };
				ZOrderType = DrawingToolZOrder.AlwaysDrawnFirst;
				 
			}
			else if (State == State.Terminated)
				Dispose();
		}
		
		 private void FindVolumeAnalysisProfileIndicator(ChartControl chartControl)
        {
			if(DataChanged!=null)
				return;
            // Iterate through all indicators on the chart
            foreach (var indicator in chartControl.Indicators)
            {
                // Check if the indicator is of type VolumeAnalysisProfile
                if (indicator is FlexibleVolumeAnalysisProfile volumeAnalysisProfile)
                { 
					 DataChanged += volumeAnalysisProfile.OnCustomDrawingToolDataChanged;
					 break;
                }
            }
        }

		public override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
	 
			//Allow user to change ZOrder when manually drawn on chart
			if (!hasSetZOrder && !StartAnchor.IsNinjaScriptDrawn)
			{
				ZOrderType = DrawingToolZOrder.Normal;
				ZOrder = ChartPanel.ChartObjects.Min(z => z.ZOrder) - 1;
				hasSetZOrder = true;
			}
			RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
			Stroke outlineStroke = OutlineStroke;
			outlineStroke.RenderTarget = RenderTarget;
			ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];

			// recenter region anchors to always be onscreen/centered
			double middleX = chartPanel.X + chartPanel.W / 2d;
			double middleY = chartPanel.Y + chartPanel.H / 2d;


			StartAnchor.UpdateYFromDevicePoint(new Point(0, middleY), chartScale);
			EndAnchor.UpdateYFromDevicePoint(new Point(0, middleY), chartScale);

			


			Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
			Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
			double width = endPoint.X - startPoint.X;
			
			
			 
			


			
			AnchorLineStroke.RenderTarget = RenderTarget;

			if (!IsInHitTest && AreaBrush != null)
			{
				if (areaBrushDevice.Brush == null)
				{
					Brush brushCopy = areaBrush.Clone();
					brushCopy.Opacity = areaOpacity / 100d;
					areaBrushDevice.Brush = brushCopy;
				}
				areaBrushDevice.RenderTarget = RenderTarget;
			}
			else
			{
				areaBrushDevice.RenderTarget = null;
				areaBrushDevice.Brush = null;
			}

			// align to full pixel to avoid unneeded aliasing
			float strokePixAdjust = Math.Abs(outlineStroke.Width % 2d).ApproxCompare(0) == 0 ? 0.5f : 0f;
		 
			headerRect = new SharpDX.RectangleF((float)startPoint.X + strokePixAdjust, ChartPanel.Y - outlineStroke.Width + strokePixAdjust,
										(float)width, headerHeight); 
            RenderTarget.FillRectangle(headerRect, OutlineStroke.Brush.ToDxBrush(RenderTarget));
			float closeButtonSize = headerHeight/1.5f;
            closeButtonRect = new SharpDX.RectangleF(headerRect.Right - closeButtonSize, headerRect.Top, closeButtonSize, closeButtonSize);
			// Draw the 'X' text as the close button
            var textFormat = new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, 20);
            var textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory, "-", textFormat, closeButtonSize, closeButtonSize);

            // Center the text in the header
            float textX = headerRect.Right - closeButtonSize;
            float textY = headerRect.Top + (headerHeight - textLayout.Metrics.Height) / 2;

            RenderTarget.DrawText("-", textFormat, new SharpDX.RectangleF(textX, textY, closeButtonSize, closeButtonSize), Brushes.White.ToDxBrush(RenderTarget));
        
      
             
			
			SharpDX.RectangleF rect = 
				new SharpDX.RectangleF((float)startPoint.X + strokePixAdjust, ChartPanel.Y - outlineStroke.Width + strokePixAdjust,
										(float)width, chartPanel.Y + chartPanel.H + outlineStroke.Width * 2);

			if (!IsInHitTest && areaBrushDevice.BrushDX != null)
				RenderTarget.FillRectangle(rect, areaBrushDevice.BrushDX);

			SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : outlineStroke.BrushDX;
			RenderTarget.DrawRectangle(rect, tmpBrush, outlineStroke.Width, outlineStroke.StrokeStyle);

			if (IsSelected)
			{
				tmpBrush = IsInHitTest ? chartControl.SelectionBrush : AnchorLineStroke.BrushDX;
				RenderTarget.DrawLine(startPoint.ToVector2(), endPoint.ToVector2(), tmpBrush, AnchorLineStroke.Width, AnchorLineStroke.StrokeStyle);
			}
			RecentPointEventArgs = new PointsEventArgs(startPoint, endPoint);
            DataChanged?.Invoke(this, RecentPointEventArgs);
		}
	}

	/// <summary>
	/// Represents an interface that exposes information regarding a Region Highlight X IDrawingTool.
	/// </summary>
	[CLSCompliant(false)]
	public class VolumeProfileRegion : VolumeProfileRegionBase
	{
		public override object Icon { get { return Gui.Tools.Icons.DrawRegionHighlightX; } }

		protected override void OnStateChange()
		{
			
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				Name = "Volume Profile Region"; 
				StartAnchor.IsYPropertyVisible = false;
				EndAnchor.IsYPropertyVisible = false; 
			}
			 
			 else if (State == State.Configure)
            {
				
            //    if (!Indicators.WyckoffZen.FlexibleVolumeAnalysisProfile.IsIndicatorActive)
          //  {
            //   throw new InvalidOperationException($"{Name} drawing tool requires FlexibleVolumeAnalysisProfile indicator to be active.");
             
           // }
            }
		}
		
		
		 
		
	}


}













