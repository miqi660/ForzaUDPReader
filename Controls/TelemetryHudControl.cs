using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ForzaUDPReader.WPF.Data;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// Forza 遥测 HUD 自定义控件
    /// 使用 DrawingContext 进行所有绘制，从 WinForms GDI+ 迁移而来
    /// </summary>
    public class TelemetryHudControl : FrameworkElement
    {
        // 数据状态
        private ForzaTelemetryData _currentData;
        private bool _hasReceivedData;
        private readonly object _dataLock = new object();

        // 历史数据用于轨迹线
        private readonly List<float> _throttleHistory = new List<float>();
        private readonly List<float> _brakeHistory = new List<float>();
        private readonly List<float> _clutchHistory = new List<float>();
        private readonly List<float> _steerHistory = new List<float>();
        private const int MaxHistoryPoints = 200;

        // 缓存最大转速，避免颠簸时不稳定
        private float _cachedMaxRpm;

        // 爆闪状态
        private bool _blinkState;
        private DateTime _lastBlinkTime = DateTime.MinValue;

        // 颜色定义 (参照 HTML)
        private static readonly Color ClutchColor = Color.FromRgb(74, 110, 224);   // #4a6ee0
        private static readonly Color BrakeColor = Color.FromRgb(255, 107, 74);    // #ff6b4a
        private static readonly Color ThrottleColor = Color.FromRgb(76, 217, 100); // #4cd964
        private static readonly Color GearColor = Color.FromRgb(255, 204, 0);      // #ffcc00
        private static readonly Color TextColor = Colors.White;
        private static readonly Color TextDimColor = Color.FromRgb(170, 170, 170);
        private static readonly Color BarTrackColor = Color.FromRgb(42, 48, 56);   // #2a3038
        private static readonly Color GridColor = Color.FromRgb(59, 67, 74);       // #3b434a

        // 预创建画刷（避免每帧分配）
        private static readonly SolidColorBrush ClutchBrush = new SolidColorBrush(ClutchColor);
        private static readonly SolidColorBrush BrakeBrush = new SolidColorBrush(BrakeColor);
        private static readonly SolidColorBrush ThrottleBrush = new SolidColorBrush(ThrottleColor);
        private static readonly SolidColorBrush GearBrush = new SolidColorBrush(GearColor);
        private static readonly SolidColorBrush TextBrush = new SolidColorBrush(TextColor);
        private static readonly SolidColorBrush TextDimBrush = new SolidColorBrush(TextDimColor);
        private static readonly SolidColorBrush BarTrackBrush = new SolidColorBrush(BarTrackColor);
        private static readonly SolidColorBrush GridBrush = new SolidColorBrush(GridColor);

        // 字体
        private FontFamily _fontFamily;
        private static readonly FontFamily SegoeFont = new FontFamily("Segoe UI");

        public TelemetryHudControl()
        {
            // 加载自定义字体
            try
            {
                _fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#sui generis free");
            }
            catch
            {
                _fontFamily = new FontFamily("Arial Black");
            }

            // 冻结画刷以提高性能
            ClutchBrush.Freeze();
            BrakeBrush.Freeze();
            ThrottleBrush.Freeze();
            GearBrush.Freeze();
            TextBrush.Freeze();
            TextDimBrush.Freeze();
            BarTrackBrush.Freeze();
            GridBrush.Freeze();

            // 启用硬件加速渲染
            SnapsToDevicePixels = true;
        }

        /// <summary>
        /// 更新遥测数据
        /// </summary>
        public void UpdateData(ForzaTelemetryData data)
        {
            lock (_dataLock)
            {
                _currentData = data;
                _hasReceivedData = true;

                _throttleHistory.Add(data.ThrottlePercent);
                _brakeHistory.Add(data.BrakePercent);
                _clutchHistory.Add(data.ClutchPercent);
                _steerHistory.Add(data.SteerPercent);

                if (_throttleHistory.Count > MaxHistoryPoints) _throttleHistory.RemoveAt(0);
                if (_brakeHistory.Count > MaxHistoryPoints) _brakeHistory.RemoveAt(0);
                if (_clutchHistory.Count > MaxHistoryPoints) _clutchHistory.RemoveAt(0);
                if (_steerHistory.Count > MaxHistoryPoints) _steerHistory.RemoveAt(0);
            }

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            ForzaTelemetryData data;
            float[] throttleSnap, brakeSnap, clutchSnap, steerSnap;
            bool hasData;

            lock (_dataLock)
            {
                data = _currentData;
                hasData = _hasReceivedData;
                throttleSnap = _throttleHistory.ToArray();
                brakeSnap = _brakeHistory.ToArray();
                clutchSnap = _clutchHistory.ToArray();
                steerSnap = _steerHistory.ToArray();
            }

            // 主面板区域
            double panelX = 20;
            double panelY = 15;
            double panelWidth = w - 40;
            double panelHeight = h - 30;

            // 绘制主面板背景
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(20, 27, 34)), null,
                new Rect(panelX, panelY, panelWidth, panelHeight));

            // 布局分配
            double pad = 10;
            double chartWidth = panelWidth * 0.50;
            double pedalWidth = 100;
            double statusWidth = 100;
            double steerWidth = 140;

            double currentX = panelX + pad;

            // 1. 绘制图表区 (轨迹线)
            DrawChartArea(dc, currentX, panelY + pad, chartWidth - pad * 2, panelHeight - pad * 2,
                throttleSnap, brakeSnap, clutchSnap);
            currentX += chartWidth;

            // 2. 绘制踏板区
            DrawPedals(dc, currentX, panelY + pad, pedalWidth, panelHeight - pad * 2, data);
            currentX += pedalWidth - 10;

            // 3. 绘制车辆状态区 (RPM + 档位 + 速度)
            DrawVehicleStatus(dc, currentX, panelY + pad, statusWidth, panelHeight - pad * 2, data, hasData);
            currentX += statusWidth - 20;

            // 4. 绘制转向区
            DrawSteering(dc, currentX, panelY + pad, steerWidth, panelHeight - pad * 2, data);
        }

        #region 图表区

        private void DrawChartArea(DrawingContext dc, double x, double y, double width, double height,
            float[] throttleHistory, float[] brakeHistory, float[] clutchHistory)
        {
            // 绘制网格线 (5条横线)
            var gridPen = new Pen(GridBrush, 1);
            gridPen.Freeze();
            int gridCount = 5;
            for (int i = 0; i <= gridCount; i++)
            {
                double lineY = y + (height * i / gridCount);
                dc.DrawLine(gridPen, new Point(x, lineY), new Point(x + width, lineY));
            }

            // 绘制轨迹线
            if (throttleHistory.Length > 1)
            {
                DrawTraceLine(dc, x, y, width, height, throttleHistory, ThrottleBrush, 100f);
                DrawTraceLine(dc, x, y, width, height, brakeHistory, BrakeBrush, 100f);
                DrawTraceLine(dc, x, y, width, height, clutchHistory, ClutchBrush, 100f);
            }
        }

        private void DrawTraceLine(DrawingContext dc, double x, double y, double width, double height,
            float[] data, Brush color, float maxValue)
        {
            if (data.Length < 2) return;

            var pen = new Pen(color, 2);
            pen.Freeze();

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(
                    new Point(x, y + height - (height * Math.Min(data[0], maxValue) / maxValue)),
                    false, false);

                var points = new Point[data.Length];
                for (int i = 1; i < data.Length; i++)
                {
                    double px = x + (width * i / (double)(MaxHistoryPoints - 1));
                    double py = y + height - (height * Math.Min(data[i], maxValue) / maxValue);
                    points[i] = new Point(px, py);
                }

                var segment = new PolyLineSegment();
                for (int i = 1; i < data.Length; i++)
                {
                    segment.Points.Add(points[i]);
                }
                ctx.PolyLineTo(segment.Points, true, false);
            }
            geometry.Freeze();

            dc.DrawGeometry(null, pen, geometry);
        }

        #endregion

        #region 踏板区

        private void DrawPedals(DrawingContext dc, double x, double y, double width, double height,
            ForzaTelemetryData data)
        {
            double barWidth = 20;
            double barSpacing = 12;

            DrawSinglePedal(dc, x, y, barWidth, height, data.ClutchPercent, ClutchBrush, "C");
            DrawSinglePedal(dc, x + barWidth + barSpacing, y, barWidth, height, data.BrakePercent, BrakeBrush, "B");
            DrawSinglePedal(dc, x + (barWidth + barSpacing) * 2, y, barWidth, height, data.ThrottlePercent, ThrottleBrush, "T");
        }

        private void DrawSinglePedal(DrawingContext dc, double x, double y, double width, double height,
            double percent, Brush color, string label)
        {
            double capHeight = 4;
            double valueHeight = 16;
            double labelHeight = 16;
            double barTop = y + valueHeight + capHeight;
            double barHeight = height - valueHeight - capHeight - labelHeight - capHeight;

            // 百分比值
            var valueText = new FormattedText(
                ((int)percent).ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(SegoeFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                10, TextBrush, 1.0);
            dc.DrawText(valueText, new Point(x + (width - valueText.Width) / 2, y - 10));

            // 顶部色块
            dc.DrawRectangle(color, null, new Rect(x, y + valueHeight, width, capHeight));

            // 轨道背景
            dc.DrawRectangle(BarTrackBrush, null, new Rect(x, barTop, width, barHeight));

            // 填充 (从底部向上)
            double fillHeight = barHeight * (percent / 100.0);
            dc.DrawRectangle(color, null, new Rect(x, barTop + barHeight - fillHeight, width, fillHeight));

            // 底部色块
            dc.DrawRectangle(color, null, new Rect(x, barTop + barHeight + capHeight, width, capHeight));

            // 标签
            var labelText = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(SegoeFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                9, TextBrush, 1.0);
            dc.DrawText(labelText, new Point(x + (width - labelText.Width) / 2, barTop + barHeight + capHeight + 2));
        }

        #endregion

        #region 车辆状态区

        private void DrawVehicleStatus(DrawingContext dc, double x, double y, double width, double height,
            ForzaTelemetryData data, bool hasData)
        {
            // RPM LED 灯条 (7个圆形LED: 3绿 + 2黄 + 2红)
            int ledCount = 7;
            double ledSize = 10;
            double ledSpacing = 3;
            double ledTotalWidth = ledCount * ledSize + (ledCount - 1) * ledSpacing;
            double ledX = x + (width - ledTotalWidth) / 2;
            double ledY = y;

            // 使用缓存的最大转速，避免颠簸时不稳定
            if (data.EngineMaxRpm > 0 && data.EngineMaxRpm > _cachedMaxRpm)
            {
                _cachedMaxRpm = data.EngineMaxRpm;
            }
            float maxRpm = _cachedMaxRpm > 0 ? _cachedMaxRpm : data.EngineMaxRpm;
            float rpmPercent = maxRpm > 0 ? data.CurrentEngineRpm / maxRpm : 0;
            rpmPercent = Math.Min(rpmPercent, 1f);

            // 更新爆闪状态 (100ms间隔)
            var now = DateTime.Now;
            if ((now - _lastBlinkTime).TotalMilliseconds >= 100)
            {
                _blinkState = !_blinkState;
                _lastBlinkTime = now;
            }

            // 转速最高时全红警告
            bool allRed = rpmPercent >= 0.90f;

            for (int i = 0; i < ledCount; i++)
            {
                float threshold = (i + 0.3f) / ledCount;
                bool isOn = rpmPercent >= threshold;

                Color ledColor;
                if (!isOn)
                    ledColor = Color.FromRgb(60, 60, 60);
                else if (allRed && !_blinkState)
                    ledColor = Color.FromRgb(60, 60, 60); // 爆闪关闭
                else if (allRed)
                    ledColor = Color.FromRgb(255, 50, 50); // 全红警告
                else if (i < 3)
                    ledColor = Color.FromRgb(0, 200, 0);
                else if (i < 5)
                    ledColor = Color.FromRgb(255, 200, 0);
                else
                    ledColor = Color.FromRgb(255, 50, 50);

                var ledBrush = new SolidColorBrush(ledColor);
                ledBrush.Freeze();
                double cx = ledX + i * (ledSize + ledSpacing) + ledSize / 2;
                double cy = ledY + ledSize / 2;
                dc.DrawEllipse(ledBrush, null, new Point(cx, cy), ledSize / 2, ledSize / 2);

                // LED 边框
                var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 1);
                borderPen.Freeze();
                dc.DrawEllipse(null, borderPen, new Point(cx, cy), ledSize / 2, ledSize / 2);
            }

            // 档位显示 (大号居中)
            double gearY = ledY + ledSize - 5;
            string gearText = hasData ? data.GearString : "N";
            var gearFormatted = new FormattedText(
                gearText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                36, GearBrush, 1.0);
            dc.DrawText(gearFormatted, new Point(x + (width - gearFormatted.Width) / 2, gearY));

            // 速度显示
            double speedY = gearY + 70;

            // 速度数值
            string speedText = ((int)data.SpeedKmh).ToString();
            var speedFormatted = new FormattedText(
                speedText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                15, TextBrush, 1.0);
            dc.DrawText(speedFormatted, new Point(x + (width - speedFormatted.Width) / 2, speedY));

            // 速度单位
            var unitFormatted = new FormattedText(
                "km/h",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                10, TextDimBrush, 1.0);
            dc.DrawText(unitFormatted, new Point(x + (width - unitFormatted.Width) / 2, speedY + 28));
        }

        #endregion

        #region 转向区

        private void DrawSteering(DrawingContext dc, double x, double y, double width, double height,
            ForzaTelemetryData data)
        {
            double centerX = x + width / 2;
            double centerY = y + height / 2;
            double radius = Math.Min(width, height) / 2 - 10;

            // 方向盘旋转角度
            float steerAngle = data.SteerPercent * 1.35f;

            // SVG viewBox 200x200, 中心 (100,100), 外圈半径77.5
            double scale = radius / 77.5;

            // SVG坐标转屏幕坐标
            double Sx(double svgX) => centerX + (svgX - 100) * scale;
            double Sy(double svgY) => centerY + (svgY - 100) * scale;

            // 推送旋转变换
            dc.PushTransform(new RotateTransform(steerAngle, centerX, centerY));

            // 1. 外圆环 (stroke-width=21, r=77.5)
            var outerPen = new Pen(new SolidColorBrush(Color.FromRgb(228, 230, 232)), 21 * scale);
            outerPen.Freeze();
            dc.DrawEllipse(null, outerPen, new Point(centerX, centerY), radius, radius);

            // 2. 方向盘结构路径
            // SVG path: M86,180 L86,124 Q86,114 76,114 L15,114 L15,86
            //           Q65,86 85,75 A30,30,0,0,1,115,75
            //           Q135,86 185,86 L185,114 L124,114
            //           Q114,114 114,124 L114,180 Z
            var steerBrush = new SolidColorBrush(Color.FromRgb(228, 230, 232));
            steerBrush.Freeze();

            var pathGeo = new PathGeometry();
            var pathFig = new PathFigure();
            pathFig.StartPoint = new Point(Sx(86), Sy(180));
            pathFig.IsClosed = true;

            // 左侧立柱
            pathFig.Segments.Add(new LineSegment(new Point(Sx(86), Sy(124)), true));

            // Q 86,114 → 76,114 (从86,124) — 贝塞尔曲线模拟二次贝塞尔
            pathFig.Segments.Add(new BezierSegment(
                new Point(Sx(86 + 2f / 3 * (86 - 86)), Sy(124 + 2f / 3 * (114 - 124))),
                new Point(Sx(76 + 2f / 3 * (86 - 76)), Sy(114 + 2f / 3 * (114 - 114))),
                new Point(Sx(76), Sy(114)), true));

            // 横杆左侧
            pathFig.Segments.Add(new LineSegment(new Point(Sx(15), Sy(114)), true));
            pathFig.Segments.Add(new LineSegment(new Point(Sx(15), Sy(86)), true));

            // Q 65,86 → 85,75 (从15,86)
            pathFig.Segments.Add(new BezierSegment(
                new Point(Sx(15 + 2f / 3 * (65 - 15)), Sy(86 + 2f / 3 * (86 - 86))),
                new Point(Sx(85 + 2f / 3 * (65 - 85)), Sy(75 + 2f / 3 * (86 - 75))),
                new Point(Sx(85), Sy(75)), true));

            // A 30,30,0,0,1,115,75 — 圆弧
            pathFig.Segments.Add(new ArcSegment(
                new Point(Sx(115), Sy(75)),
                new Size(30 * scale, 30 * scale),
                0, false, SweepDirection.Clockwise, true));

            // Q 135,86 → 185,86 (从115,75)
            pathFig.Segments.Add(new BezierSegment(
                new Point(Sx(115 + 2f / 3 * (135 - 115)), Sy(75 + 2f / 3 * (86 - 75))),
                new Point(Sx(185 + 2f / 3 * (135 - 185)), Sy(86 + 2f / 3 * (86 - 86))),
                new Point(Sx(185), Sy(86)), true));

            // 横杆右侧
            pathFig.Segments.Add(new LineSegment(new Point(Sx(185), Sy(114)), true));
            pathFig.Segments.Add(new LineSegment(new Point(Sx(124), Sy(114)), true));

            // Q 114,114 → 114,124 (从124,114)
            pathFig.Segments.Add(new BezierSegment(
                new Point(Sx(124 + 2f / 3 * (114 - 124)), Sy(114 + 2f / 3 * (114 - 114))),
                new Point(Sx(114 + 2f / 3 * (114 - 114)), Sy(124 + 2f / 3 * (114 - 124))),
                new Point(Sx(114), Sy(124)), true));

            // 右侧立柱
            pathFig.Segments.Add(new LineSegment(new Point(Sx(114), Sy(180)), true));

            pathGeo.Figures.Add(pathFig);
            pathGeo.Freeze();
            dc.DrawGeometry(steerBrush, null, pathGeo);

            // 3. 顶部红色标记 (rect x=94 y=12 w=12 h=21)
            var redBrush = new SolidColorBrush(Color.FromRgb(235, 47, 47));
            redBrush.Freeze();
            dc.DrawRectangle(redBrush, null, new Rect(Sx(94), Sy(12), 12 * scale, 21 * scale));

            // 恢复变换
            dc.Pop();

            // 4. 中心轮毂 (不旋转, r=16)
            double hubR = 16 * scale;
            var hubBrush = new SolidColorBrush(Color.FromRgb(20, 27, 34));
            hubBrush.Freeze();
            dc.DrawEllipse(hubBrush, null, new Point(centerX, centerY), hubR, hubR);
        }

        #endregion
    }
}
