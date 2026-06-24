using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 折线图轨迹线区 — 纯自绘 (OnRender)
    /// 显示油门/刹车/离合的历史轨迹
    /// </summary>
    public partial class ChartControl : System.Windows.Controls.UserControl
    {
        private const int MaxHistoryPoints = 200;

        private readonly List<float> _throttleHistory = new List<float>();
        private readonly List<float> _brakeHistory = new List<float>();
        private readonly List<float> _clutchHistory = new List<float>();
        private readonly List<float> _handbrakeHistory = new List<float>();
        private readonly object _historyLock = new object();

        // 预冻结画刷（避免每帧分配）
        private static readonly SolidColorBrush GridBrush = CreateFrozenBrush(59, 67, 74);       // #3b434a
        private static readonly SolidColorBrush ThrottleBrush = CreateFrozenBrush(76, 217, 100);  // #4cd964
        private static readonly SolidColorBrush BrakeBrush = CreateFrozenBrush(255, 107, 74);     // #ff6b4a
        private static readonly SolidColorBrush ClutchBrush = CreateFrozenBrush(74, 110, 224);    // #4a6ee0
        private static readonly SolidColorBrush HandbrakeBrush = CreateFrozenBrush(255, 165, 0);  // #FFA500

        public ChartControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 添加一个数据点（可从任意线程调用）
        /// </summary>
        public void AddDataPoint(float throttle, float brake, float clutch, float handbrake)
        {
            lock (_historyLock)
            {
                _throttleHistory.Add(throttle);
                _brakeHistory.Add(brake);
                _clutchHistory.Add(clutch);
                _handbrakeHistory.Add(handbrake);

                if (_throttleHistory.Count > MaxHistoryPoints) _throttleHistory.RemoveAt(0);
                if (_brakeHistory.Count > MaxHistoryPoints) _brakeHistory.RemoveAt(0);
                if (_clutchHistory.Count > MaxHistoryPoints) _clutchHistory.RemoveAt(0);
                if (_handbrakeHistory.Count > MaxHistoryPoints) _handbrakeHistory.RemoveAt(0);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            float[] throttleSnap, brakeSnap, clutchSnap, handbrakeSnap;
            lock (_historyLock)
            {
                throttleSnap = _throttleHistory.ToArray();
                brakeSnap = _brakeHistory.ToArray();
                clutchSnap = _clutchHistory.ToArray();
                handbrakeSnap = _handbrakeHistory.ToArray();
            }

            // 绘制网格线 (5条横线)
            var gridPen = new Pen(GridBrush, 1);
            gridPen.Freeze();
            int gridCount = 5;
            for (int i = 0; i <= gridCount; i++)
            {
                double lineY = h * i / gridCount;
                dc.DrawLine(gridPen, new Point(0, lineY), new Point(w, lineY));
            }

            // 绘制轨迹线
            if (throttleSnap.Length > 1)
            {
                DrawTraceLine(dc, w, h, throttleSnap, ThrottleBrush, 100f);
                DrawTraceLine(dc, w, h, brakeSnap, BrakeBrush, 100f);
                DrawTraceLine(dc, w, h, clutchSnap, ClutchBrush, 100f);
                DrawTraceLine(dc, w, h, handbrakeSnap, HandbrakeBrush, 100f);
            }
        }

        private void DrawTraceLine(DrawingContext dc, double width, double height,
            float[] data, Brush color, float maxValue)
        {
            if (data.Length < 2) return;

            var pen = new Pen(color, 2);
            pen.Freeze();

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                double px0 = width * 0 / (double)(MaxHistoryPoints - 1);
                double py0 = height - (height * Math.Min(data[0], maxValue) / maxValue);
                ctx.BeginFigure(new Point(px0, py0), false, false);

                var points = new Point[data.Length];
                for (int i = 1; i < data.Length; i++)
                {
                    double px = width * i / (double)(MaxHistoryPoints - 1);
                    double py = height - (height * Math.Min(data[i], maxValue) / maxValue);
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

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
