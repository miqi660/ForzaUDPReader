using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ForzaUDPReader
{
    public partial class MainForm : Form
    {
        private UdpReceiver _receiver;
        private ForzaTelemetryData _currentData;
        private bool _hasReceivedData = false;
        private readonly object _dataLock = new object();
        private System.Windows.Forms.Timer _refreshTimer;
        private System.Windows.Forms.Timer _blinkTimer;
        private bool _blinkState;

        // 图形资源
        private Bitmap _bufferBitmap;
        private Graphics _bufferGraphics;
        private PrivateFontCollection _fontCollection;
        private Font _gearFont;
        private Font _speedFont;
        private Font _speedUnitFont;

        // 历史数据用于轨迹线
        private readonly List<float> _throttleHistory = new List<float>();
        private readonly List<float> _brakeHistory = new List<float>();
        private readonly List<float> _clutchHistory = new List<float>();
        private readonly List<float> _steerHistory = new List<float>();
        private const int MaxHistoryPoints = 200;

        // 缓存最大转速，避免颠簸时不稳定
        private float _cachedMaxRpm;

        // 颜色定义 (参照 HTML)
        private readonly Color _bgMain = Color.FromArgb(20, 27, 34);       // #141b22
        private readonly Color _bgDark = Color.FromArgb(13, 17, 21);       // #0d1115
        private readonly Color _gridColor = Color.FromArgb(59, 67, 74);    // #3b434a
        private readonly Color _clutchColor = Color.FromArgb(74, 110, 224); // #4a6ee0
        private readonly Color _brakeColor = Color.FromArgb(255, 107, 74);  // #ff6b4a
        private readonly Color _throttleColor = Color.FromArgb(76, 217, 100); // #4cd964
        private readonly Color _gearColor = Color.FromArgb(255, 204, 0);    // #ffcc00
        private readonly Color _steerColor = Color.FromArgb(224, 228, 232); // #e0e4e8
        private readonly Color _steerMarkColor = Color.FromArgb(255, 59, 48); // #ff3b30
        private readonly Color _textColor = Color.White;
        private readonly Color _textDimColor = Color.FromArgb(170, 170, 170);
        private readonly Color _barTrackColor = Color.FromArgb(42, 48, 56); // #2a3038
        private readonly Color _traceGray = Color.FromArgb(122, 124, 127);  // #7a7c7f

        public MainForm()
        {
            InitializeComponent();
            SetupForm();
            InitializeReceiver();
            StartRefreshTimer();
        }

        private void SetupForm()
        {
            this.Text = "Forza Horizon 6 - Telemetry HUD";
            this.Size = new Size(680, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = _bgDark;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.TopMost = true; // 窗口置顶
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // 加载自定义字体
            _fontCollection = new PrivateFontCollection();
            string fontPath = Path.Combine(Application.StartupPath, "Fonts", "sui generis free.ttf");
            if (File.Exists(fontPath))
            {
                _fontCollection.AddFontFile(fontPath);
                _gearFont = new Font(_fontCollection.Families[0], 36, FontStyle.Bold);
                _speedFont = new Font(_fontCollection.Families[0], 15, FontStyle.Bold);
                _speedUnitFont = new Font(_fontCollection.Families[0], 10);
            }
            else
            {
                _gearFont = new Font("Arial Black", 42, FontStyle.Bold);
                _speedFont = new Font("Segoe UI", 15, FontStyle.Bold);
                _speedUnitFont = new Font("Segoe UI", 10);
            }

            _bufferBitmap = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            _bufferGraphics = Graphics.FromImage(_bufferBitmap);
            _bufferGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _bufferGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        private void InitializeReceiver()
        {
            _receiver = new UdpReceiver(21337);
            _receiver.DataReceived += OnDataReceived;
            _receiver.ErrorOccurred += OnErrorOccurred;
            _receiver.Start();
        }

        private void StartRefreshTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 16; // ~60 FPS
            _refreshTimer.Tick += (s, e) => Invalidate();
            _refreshTimer.Start();

            // 爆闪定时器
            _blinkTimer = new System.Windows.Forms.Timer();
            _blinkTimer.Interval = 100; // 100ms闪烁
            _blinkTimer.Tick += (s, e) => _blinkState = !_blinkState;
            _blinkTimer.Start();
        }

        private void OnDataReceived(object sender, ForzaTelemetryData data)
        {
            lock (_dataLock)
            {
                _currentData = data;
                _hasReceivedData = true;

                // 记录历史数据
                _throttleHistory.Add(data.ThrottlePercent);
                _brakeHistory.Add(data.BrakePercent);
                _clutchHistory.Add(data.ClutchPercent);
                _steerHistory.Add(data.SteerPercent);

                if (_throttleHistory.Count > MaxHistoryPoints)
                    _throttleHistory.RemoveAt(0);
                if (_brakeHistory.Count > MaxHistoryPoints)
                    _brakeHistory.RemoveAt(0);
                if (_clutchHistory.Count > MaxHistoryPoints)
                    _clutchHistory.RemoveAt(0);
                if (_steerHistory.Count > MaxHistoryPoints)
                    _steerHistory.RemoveAt(0);
            }
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            Console.WriteLine($"UDP Error: {ex.Message}");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_bufferBitmap == null) return;

            _bufferGraphics.Clear(_bgDark);

            ForzaTelemetryData data;
            List<float> throttleHistory, brakeHistory, clutchHistory, steerHistory;
            lock (_dataLock)
            {
                data = _currentData;
                throttleHistory = new List<float>(_throttleHistory);
                brakeHistory = new List<float>(_brakeHistory);
                clutchHistory = new List<float>(_clutchHistory);
                steerHistory = new List<float>(_steerHistory);
            }

            // 主面板区域
            int panelX = 20;
            int panelY = 15;
            int panelWidth = this.ClientSize.Width - 40;
            int panelHeight = this.ClientSize.Height - 30;

            // 绘制主面板背景
            using (var brush = new SolidBrush(_bgMain))
            {
                _bufferGraphics.FillRectangle(brush, panelX, panelY, panelWidth, panelHeight);
            }

            // 布局分配
            int chartWidth = (int)(panelWidth * 0.50f);     // 图表区 50%
            int pedalWidth = 100;                            // 踏板区
            int statusWidth = 100;                           // 车辆状态区
            int steerWidth = 140;                            // 转向区

            int currentX = panelX + 10;

            // 1. 绘制图表区 (轨迹线)
            DrawChartArea(_bufferGraphics, currentX, panelY + 10, chartWidth - 20, panelHeight - 20, throttleHistory, brakeHistory, clutchHistory, steerHistory);
            currentX += chartWidth;

            // 2. 绘制踏板区
            DrawPedals(_bufferGraphics, currentX, panelY + 10, pedalWidth, panelHeight - 20, data);
            currentX += pedalWidth - 10;

            // 3. 绘制车辆状态区 (RPM + 档位 + 速度)
            DrawVehicleStatus(_bufferGraphics, currentX, panelY + 10, statusWidth, panelHeight - 20, data);
            currentX += statusWidth - 20;

            // 4. 绘制转向区
            DrawSteering(_bufferGraphics, currentX, panelY + 10, steerWidth, panelHeight - 20, data);

            e.Graphics.DrawImage(_bufferBitmap, 0, 0);
        }

        private void DrawChartArea(Graphics g, int x, int y, int width, int height,
            List<float> throttleHistory, List<float> brakeHistory, List<float> clutchHistory, List<float> steerHistory)
        {
            // 绘制网格线 (5条横线)
            int gridCount = 5;
            for (int i = 0; i <= gridCount; i++)
            {
                int lineY = y + (height * i / gridCount);
                using (var pen = new Pen(_gridColor, 1))
                {
                    g.DrawLine(pen, x, lineY, x + width, lineY);
                }
            }

            // 绘制轨迹线
            if (throttleHistory.Count > 1)
            {
                DrawTraceLine(g, x, y, width, height, throttleHistory, _throttleColor, 100f);
                DrawTraceLine(g, x, y, width, height, brakeHistory, _brakeColor, 100f);
                DrawTraceLine(g, x, y, width, height, clutchHistory, _clutchColor, 100f);
            }
        }

        private void DrawTraceLine(Graphics g, int x, int y, int width, int height,
            List<float> data, Color color, float maxValue)
        {
            float[] snapshot;
            lock (_dataLock)
            {
                if (data.Count < 2) return;
                snapshot = data.ToArray();
            }

            using (var pen = new Pen(color, 2))
            {
                var points = new PointF[snapshot.Length];
                for (int i = 0; i < snapshot.Length; i++)
                {
                    float px = x + (width * i / (float)(MaxHistoryPoints - 1));
                    float py = y + height - (height * Math.Min(snapshot[i], maxValue) / maxValue);
                    points[i] = new PointF(px, py);
                }
                g.DrawLines(pen, points);
            }
        }

        private void DrawPedals(Graphics g, int x, int y, int width, int height, ForzaTelemetryData data)
        {
            int barWidth = 20;
            int barSpacing = 12;
            int barStartX = x;

            // 绘制三个踏板: 离合、刹车、油门
            DrawSinglePedal(g, barStartX, y, barWidth, height, data.ClutchPercent, _clutchColor, "C");
            DrawSinglePedal(g, barStartX + barWidth + barSpacing, y, barWidth, height, data.BrakePercent, _brakeColor, "B");
            DrawSinglePedal(g, barStartX + (barWidth + barSpacing) * 2, y, barWidth, height, data.ThrottlePercent, _throttleColor, "T");
        }

        private void DrawSinglePedal(Graphics g, int x, int y, int width, int height,
            float percent, Color color, string label)
        {
            int capHeight = 4;
            int valueHeight = 16;
            int labelHeight = 16;
            int barTop = y + valueHeight + capHeight;
            int barHeight = height - valueHeight - capHeight - labelHeight - capHeight;

            // 百分比值
            using (var font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                string text = ((int)percent).ToString();
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, brush, x + (width - size.Width) / 2, y - 10);
            }

            // 顶部色块
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y + valueHeight, width, capHeight);
            }

            // 轨道背景
            using (var brush = new SolidBrush(_barTrackColor))
            {
                g.FillRectangle(brush, x, barTop, width, barHeight);
            }

            // 填充 (从底部向上)
            float fillHeight = barHeight * (percent / 100f);
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, barTop + barHeight - fillHeight, width, fillHeight);
            }

            // 底部色块
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, barTop + barHeight + capHeight, width, capHeight);
            }

            // 标签
            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var brush = new SolidBrush(_textColor))
            {
                var size = g.MeasureString(label, font);
                g.DrawString(label, font, brush, x + (width - size.Width) / 2, barTop + barHeight + capHeight + 2);
            }
        }

        private void DrawVehicleStatus(Graphics g, int x, int y, int width, int height, ForzaTelemetryData data)
        {
            // RPM LED 灯条 (7个圆形LED: 3绿 + 2黄 + 2红)
            int ledCount = 7;
            int ledSize = 10;
            int ledSpacing = 3;
            int ledTotalWidth = ledCount * ledSize + (ledCount - 1) * ledSpacing;
            int ledX = x + (width - ledTotalWidth) / 2;
            int ledY = y;

            // 使用缓存的最大转速，避免颠簸时不稳定
            if (data.EngineMaxRpm > 0 && data.EngineMaxRpm > _cachedMaxRpm)
            {
                _cachedMaxRpm = data.EngineMaxRpm;
            }
            float maxRpm = _cachedMaxRpm > 0 ? _cachedMaxRpm : data.EngineMaxRpm;
            float rpmPercent = maxRpm > 0 ? data.CurrentEngineRpm / maxRpm : 0;
            rpmPercent = Math.Min(rpmPercent, 1f);

            // 转速最高时全红警告
            bool allRed = rpmPercent >= 0.85f;

            for (int i = 0; i < ledCount; i++)
            {
                float threshold = (i + 0.3f) / ledCount;
                bool isOn = rpmPercent >= threshold;

                Color ledColor;
                if (!isOn)
                {
                    ledColor = Color.FromArgb(60, 60, 60); // 暗灰色
                }
                else if (allRed && !_blinkState)
                {
                    ledColor = Color.FromArgb(60, 60, 60); // 爆闪关闭
                }
                else if (allRed)
                {
                    ledColor = Color.FromArgb(255, 50, 50); // 全红警告
                }
                else if (i < 3)
                {
                    ledColor = Color.FromArgb(0, 200, 0); // 绿色
                }
                else if (i < 5)
                {
                    ledColor = Color.FromArgb(255, 200, 0); // 黄色
                }
                else
                {
                    ledColor = Color.FromArgb(255, 50, 50); // 红色
                }

                // 绘制圆形LED
                using (var brush = new SolidBrush(ledColor))
                {
                    int lx = ledX + i * (ledSize + ledSpacing);
                    g.FillEllipse(brush, lx, ledY, ledSize, ledSize);
                }

                // LED 边框
                using (var pen = new Pen(Color.FromArgb(30, 30, 30), 1))
                {
                    int lx = ledX + i * (ledSize + ledSpacing);
                    g.DrawEllipse(pen, lx, ledY, ledSize, ledSize);
                }
            }

            // 档位显示 (大号居中)
            int gearY = ledY + ledSize - 5;
            using (var brush = new SolidBrush(_gearColor))
            {
                string gearText = _hasReceivedData ? data.GearString : "N";
                var size = g.MeasureString(gearText, _gearFont);
                g.DrawString(gearText, _gearFont, brush, x + (width - size.Width) / 2, gearY);
            }

            // 速度显示
            int speedY = gearY + 70;

            // 速度数值
            using (var brush = new SolidBrush(_textColor))
            {
                string speedText = ((int)data.SpeedKmh).ToString();
                var size = g.MeasureString(speedText, _speedFont);
                g.DrawString(speedText, _speedFont, brush, x + (width - size.Width) / 2, speedY);
            }

            // 速度单位
            using (var brush = new SolidBrush(_textDimColor))
            {
                string unitText = "km/h";
                var size = g.MeasureString(unitText, _speedUnitFont);
                g.DrawString(unitText, _speedUnitFont, brush, x + (width - size.Width) / 2, speedY + 28);
            }
        }

        private void DrawSteering(Graphics g, int x, int y, int width, int height, ForzaTelemetryData data)
        {
            int centerX = x + width / 2;
            int centerY = y + height / 2;
            int radius = Math.Min(width, height) / 2 - 10;

            // 方向盘旋转角度
            float steerAngle = data.SteerPercent * 1.35f;

            // SVG viewBox 200x200, 中心 (100,100), 外圈半径77.5
            float scale = radius / 77.5f;
            // SVG坐标转屏幕坐标
            float Sx(float svgX) => centerX + (svgX - 100) * scale;
            float Sy(float svgY) => centerY + (svgY - 100) * scale;

            // 保存当前变换
            var oldTransform = g.Transform.Clone();

            // 旋转
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(steerAngle);
            g.TranslateTransform(-centerX, -centerY);

            // 1. 外圆环 (stroke-width=21, r=77.5)
            using (var pen = new Pen(Color.FromArgb(228, 230, 232), 21 * scale))
            {
                g.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2, radius * 2);
            }

            // 2. 方向盘结构路径
            // SVG path: M86,180 L86,124 Q86,114 76,114 L15,114 L15,86
            //           Q65,86 85,75 A30,30,0,0,1,115,75
            //           Q135,86 185,86 L185,114 L124,114
            //           Q114,114 114,124 L114,180 Z
            using (var path = new GraphicsPath())
            {
                // 左侧立柱
                path.AddLine(Sx(86), Sy(180), Sx(86), Sy(124));
                // Q 86,114 → 76,114 (从86,124)
                float cx1 = 86 + 2f / 3 * (86 - 86), cy1 = 124 + 2f / 3 * (114 - 124);
                float cx2 = 76 + 2f / 3 * (86 - 76), cy2 = 114 + 2f / 3 * (114 - 114);
                path.AddBezier(Sx(86), Sy(124), Sx(cx1), Sy(cy1), Sx(cx2), Sy(cy2), Sx(76), Sy(114));
                // 横杆左侧
                path.AddLine(Sx(76), Sy(114), Sx(15), Sy(114));
                path.AddLine(Sx(15), Sy(114), Sx(15), Sy(86));
                // Q 65,86 → 85,75 (从15,86)
                cx1 = 15 + 2f / 3 * (65 - 15); cy1 = 86 + 2f / 3 * (86 - 86);
                cx2 = 85 + 2f / 3 * (65 - 85); cy2 = 75 + 2f / 3 * (86 - 75);
                path.AddBezier(Sx(15), Sy(86), Sx(cx1), Sy(cy1), Sx(cx2), Sy(cy2), Sx(85), Sy(75));
                // A 30,30,0,0,1,115,75 — 圆弧从(85,75)到(115,75), 上凸
                // 圆心≈(100, 101), 半径30, 起始角≈-120°, 扫过60°
                path.AddArc(Sx(70), Sy(71), 60 * scale, 60 * scale, -120, 60);
                // Q 135,86 → 185,86 (从115,75)
                cx1 = 115 + 2f / 3 * (135 - 115); cy1 = 75 + 2f / 3 * (86 - 75);
                cx2 = 185 + 2f / 3 * (135 - 185); cy2 = 86 + 2f / 3 * (86 - 86);
                path.AddBezier(Sx(115), Sy(75), Sx(cx1), Sy(cy1), Sx(cx2), Sy(cy2), Sx(185), Sy(86));
                // 横杆右侧
                path.AddLine(Sx(185), Sy(86), Sx(185), Sy(114));
                path.AddLine(Sx(185), Sy(114), Sx(124), Sy(114));
                // Q 114,114 → 114,124 (从124,114)
                cx1 = 124 + 2f / 3 * (114 - 124); cy1 = 114 + 2f / 3 * (114 - 114);
                cx2 = 114 + 2f / 3 * (114 - 114); cy2 = 124 + 2f / 3 * (114 - 124);
                path.AddBezier(Sx(124), Sy(114), Sx(cx1), Sy(cy1), Sx(cx2), Sy(cy2), Sx(114), Sy(124));
                // 右侧立柱
                path.AddLine(Sx(114), Sy(124), Sx(114), Sy(180));
                path.CloseFigure();

                using (var brush = new SolidBrush(Color.FromArgb(228, 230, 232)))
                {
                    g.FillPath(brush, path);
                }
            }

            // 3. 顶部红色标记 (rect x=94 y=12 w=12 h=21)
            using (var brush = new SolidBrush(Color.FromArgb(235, 47, 47)))
            {
                g.FillRectangle(brush, Sx(94), Sy(12), 12 * scale, 21 * scale);
            }

            // 恢复变换
            g.Transform = oldTransform;

            // 4. 中心轮毂 (不旋转, r=16)
            float hubR = 16 * scale;
            using (var brush = new SolidBrush(_bgMain))
            {
                g.FillEllipse(brush, centerX - hubR, centerY - hubR, hubR * 2, hubR * 2);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                _bufferBitmap?.Dispose();
                _bufferGraphics?.Dispose();

                _bufferBitmap = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
                _bufferGraphics = Graphics.FromImage(_bufferBitmap);
                _bufferGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                _bufferGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _blinkTimer?.Stop();
            _blinkTimer?.Dispose();
            _receiver?.Dispose();
            _bufferGraphics?.Dispose();
            _bufferBitmap?.Dispose();
            _gearFont?.Dispose();
            _speedFont?.Dispose();
            _speedUnitFont?.Dispose();
            _fontCollection?.Dispose();
        }
    }
}
