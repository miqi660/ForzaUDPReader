using System;
using System.Windows;
using System.Windows.Media;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 方向盘矢量绘制与旋转区 — 纯自绘 (OnRender)
    /// 基于 SVG 路径数据精确还原方向盘形状
    /// </summary>
    public partial class SteeringControl : System.Windows.Controls.UserControl
    {
        #region 依赖属性

        public double SteerPercent
        {
            get => (double)GetValue(SteerPercentProperty);
            set => SetValue(SteerPercentProperty, value);
        }
        public static readonly DependencyProperty SteerPercentProperty =
            DependencyProperty.Register(nameof(SteerPercent), typeof(double), typeof(SteeringControl),
                new PropertyMetadata(0.0));

        #endregion

        // 预冻结画刷
        private static readonly SolidColorBrush SteerBrush = CreateFrozenBrush(228, 230, 232);   // #e0e4e8
        private static readonly SolidColorBrush RedMarkBrush = CreateFrozenBrush(235, 47, 47);    // #eb2f2f
        private static readonly SolidColorBrush HubBrush = CreateFrozenBrush(20, 27, 34);          // #141b22

        public SteeringControl()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double centerX = w / 2;
            double centerY = h / 2;
            double radius = Math.Min(w, h) / 2 - 10;

            // 方向盘旋转角度
            float steerAngle = (float)(SteerPercent * 1.35);

            // SVG viewBox 200x200, 中心 (100,100), 外圈半径77.5
            double scale = radius / 77.5;

            // SVG坐标转屏幕坐标
            double Sx(double svgX) => centerX + (svgX - 100) * scale;
            double Sy(double svgY) => centerY + (svgY - 100) * scale;

            // 推送旋转变换
            dc.PushTransform(new RotateTransform(steerAngle, centerX, centerY));

            // 1. 外圆环 (stroke-width=21, r=77.5)
            var outerPen = new Pen(SteerBrush, 21 * scale);
            outerPen.Freeze();
            dc.DrawEllipse(null, outerPen, new Point(centerX, centerY), radius, radius);

            // 2. 方向盘结构路径
            // SVG path: M86,180 L86,124 Q86,114 76,114 L15,114 L15,86
            //           Q65,86 85,75 A30,30,0,0,1,115,75
            //           Q135,86 185,86 L185,114 L124,114
            //           Q114,114 114,124 L114,180 Z
            var pathGeo = new PathGeometry();
            var pathFig = new PathFigure { StartPoint = new Point(Sx(86), Sy(180)), IsClosed = true };

            // 左侧立柱: L86,124
            pathFig.Segments.Add(new LineSegment(new Point(Sx(86), Sy(124)), true));

            // Q86,114 76,114 (二次贝塞尔: 从86,124, 控制点86,114, 到76,114)
            pathFig.Segments.Add(new QuadraticBezierSegment(
                new Point(Sx(86), Sy(114)),
                new Point(Sx(76), Sy(114)), true));

            // 横杆左侧: L15,114 L15,86
            pathFig.Segments.Add(new LineSegment(new Point(Sx(15), Sy(114)), true));
            pathFig.Segments.Add(new LineSegment(new Point(Sx(15), Sy(86)), true));

            // Q65,86 85,75 (二次贝塞尔: 从15,86, 控制点65,86, 到85,75)
            pathFig.Segments.Add(new QuadraticBezierSegment(
                new Point(Sx(65), Sy(86)),
                new Point(Sx(85), Sy(75)), true));

            // A30,30,0,0,1,115,75 (圆弧: 从85,75到115,75, 半径30, 顺时针)
            pathFig.Segments.Add(new ArcSegment(
                new Point(Sx(115), Sy(75)),
                new Size(30 * scale, 30 * scale),
                0, false, SweepDirection.Clockwise, true));

            // Q135,86 185,86 (二次贝塞尔: 从115,75, 控制点135,86, 到185,86)
            pathFig.Segments.Add(new QuadraticBezierSegment(
                new Point(Sx(135), Sy(86)),
                new Point(Sx(185), Sy(86)), true));

            // 横杆右侧: L185,114 L124,114
            pathFig.Segments.Add(new LineSegment(new Point(Sx(185), Sy(114)), true));
            pathFig.Segments.Add(new LineSegment(new Point(Sx(124), Sy(114)), true));

            // Q114,114 114,124 (二次贝塞尔: 从124,114, 控制点114,114, 到114,124)
            pathFig.Segments.Add(new QuadraticBezierSegment(
                new Point(Sx(114), Sy(114)),
                new Point(Sx(114), Sy(124)), true));

            // 右侧立柱: L114,180
            pathFig.Segments.Add(new LineSegment(new Point(Sx(114), Sy(180)), true));

            pathGeo.Figures.Add(pathFig);
            pathGeo.Freeze();
            dc.DrawGeometry(SteerBrush, null, pathGeo);

            // 3. 顶部红色标记 (rect x=94 y=12 w=12 h=21)
            dc.DrawRectangle(RedMarkBrush, null, new Rect(Sx(94), Sy(12), 12 * scale, 21 * scale));

            // 恢复变换
            dc.Pop();

            // 4. 中心轮毂 (不旋转, r=16)
            double hubR = 16 * scale;
            dc.DrawEllipse(HubBrush, null, new Point(centerX, centerY), hubR, hubR);
        }

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
