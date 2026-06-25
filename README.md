# ForzaUDPReader.WPF

基于 WPF (.NET 8.0) 的 Forza Horizon 6 实时遥测 HUD 覆盖层。通过 UDP 端口 21337 接收游戏遥测数据，以 60 FPS 刷新率驱动六个可视化控件，呈现赛车仪表盘级的实时数据。

## 功能特性

### 遥测数据

- **实时 UDP 接收** — 异步 `Task`-based 接收循环，`CancellationToken` 控制生命周期
- **324 字节 blittable 结构体** — `GCHandle` + `Marshal.PtrToStructure` 零拷贝反序列化
- **60 FPS UI 刷新** — `DispatcherTimer` 驱动，`lock` 线程安全共享状态

### 可视化控件

| 控件 | 渲染方式 | 说明 |
|---|---|---|
| **ChartControl** | `OnRender` + `StreamGeometry` | 油门/刹车/离合/手刹实时曲线图 |
| **PedalsControl** | XAML `ScaleTransform` 动画 | 竖条仪表（油门/刹车/离合/手刹） |
| **GearControl** | TextBlock | 档位显示 (R/N/1-16) |
| **SpeedControl** | TextBlock | 速度 + "km/h" |
| **RpmLedControl** | `OnRender` | 7 个圆形 LED，85%+ RPM 闪烁 |
| **SteeringControl** | `OnRender` + SVG 路径 | 矢量方向盘旋转动画 |

### 界面特性

- **无边框透明覆盖层** — `WindowStyle="None"` + `AllowsTransparency="True"` + `Topmost="True"`
- **Viewbox 等比缩放** — 窗口任意缩放，UI 自适应保持比例
- **抽屉式动画切换** — `DoubleAnimation` + `CubicEase(EaseOut)` 驱动的平滑宽度过渡
- **图表显隐控制** — CheckBox 切换 ChartControl，窗口宽度自动动画收缩/展开
- **自定义字体** — `Fonts/sui-generis-free.ttf` 注册为全局资源 `ForzaFont`
- **鼠标悬停控制面板** — 透明度调节、踏板模式切换（离合/手刹）
- **窗口拖拽** — 任意非交互区域按住拖拽移动

## 构建与运行

```bash
# 构建
dotnet build

# 运行
dotnet run

# 发布
dotnet publish -c Release
```

无 `.sln` 文件，直接操作 `.csproj`。无外部 NuGet 依赖，仅依赖 .NET 8.0 BCL 和 WPF 框架。

## 系统要求

- Windows 10/11
- .NET 8.0 SDK
- Forza Horizon 6（需开启 UDP 数据输出，端口 21337）

## 使用方法

1. 启动 Forza Horizon 6
2. 在游戏设置中开启 UDP 数据输出（端口 21337）
3. 运行 ForzaUDPReader.WPF
4. HUD 自动接收并显示遥测数据

**界面操作：**
- 鼠标悬停窗口 → 右上角出现齿轮和关闭按钮
- 齿轮按钮 → 打开设置面板（透明度、踏板模式、曲线图显隐）
- 拖拽窗口任意空白处 → 移动位置
- 拖拽右下角 → 缩放窗口

## 项目结构

```
ForzaUDPReader.WPF/
├── App.xaml / .cs                      # 应用入口，注册全局字体资源
├── MainWindow.xaml / .cs               # 主窗口：UdpReceiver + 60FPS Timer + 控件调度
├── Controls/
│   ├── ChartControl.xaml / .cs         # 油门/刹车/离合曲线（OnRender + StreamGeometry）
│   ├── GearControl.xaml / .cs          # 档位显示
│   ├── PedalsControl.xaml / .cs        # 踏板竖条仪表（ScaleTransform 动画）
│   ├── RpmLedControl.xaml / .cs        # RPM LED 指示灯（OnRender 绘制）
│   ├── SpeedControl.xaml / .cs         # 速度显示
│   └── SteeringControl.xaml / .cs      # 矢量方向盘（SVG 路径渲染）
├── Converters/
│   └── BoolToGridLengthConverter.cs    # Bool → GridLength 转换器
├── Data/
│   ├── ForzaTelemetryData.cs           # 324 字节 blittable 遥测结构体
│   └── UdpReceiver.cs                  # 异步 UDP 监听器
├── Fonts/
│   └── sui-generis-free.ttf            # 自定义 HUD 字体
├── CLAUDE.md                           # Claude Code 项目指引
└── ForzaUDPReader.WPF.csproj           # 项目文件
```

## 架构

```
Forza Horizon 6 (UDP:21337)
        │
        ▼
   UdpReceiver ──异步接收──▶ ForzaTelemetryData (324B struct)
        │
        ▼ DataReceived 事件
   MainWindow ──lock──▶ _currentData
        │
        ▼ 60FPS DispatcherTimer
   ┌────┼───────────────────────┐
   ▼    ▼    ▼    ▼    ▼       ▼
 Chart Pedals Gear Speed RpmLed Steering
```

## 关键设计决策

- **无 MVVM** — 数据通过 MainWindow code-behind 直接推送到子控件的依赖属性
- **自定义渲染** — ChartControl、RpmLedControl、SteeringControl 使用 `OnRender` + `DrawingContext` 直接绘制，预冻结 `SolidColorBrush` 避免每帧 GC 压力
- **线程安全** — `lock(_dataLock)` 保护 UDP 接收线程与 UI timer 之间的共享遥测状态
- **精准缩放** — Viewbox Uniform 模式下，`Scale = Window.Height / InnerGrid.LogicalHeight`，基于高度计算消除累积误差

## 许可证

MIT License
