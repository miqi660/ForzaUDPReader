# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Forza Horizon 6 实时遥测 HUD 覆盖层，基于 WPF (.NET 8.0) 构建。通过 UDP 端口 21337 接收游戏遥测数据，以 60 FPS 刷新率驱动六个可视化控件。

## 构建与运行

```bash
# 构建
dotnet build

# 运行
dotnet run

# 发布
dotnet publish -c Release
```

无 `.sln` 文件，直接操作 `.csproj`。无 NuGet 外部依赖，仅依赖 .NET 8.0 BCL 和 WPF 框架。

## 架构

数据流：`Forza Horizon 6 (UDP:21337) → UdpReceiver → MainWindow (60FPS timer) → Controls`

### 数据层 (`Data/`)

- **`ForzaTelemetryData`** — 324 字节 blittable struct，`[StructLayout(Sequential, Pack=1)]` 直接映射 Forza UDP 包格式。通过 `GCHandle` + `Marshal.PtrToStructure` 反序列化。包含计算属性（SpeedKmh、GearString、百分比转换等）。
- **`UdpReceiver`** — 异步 UDP 监听器，`Task`-based receive loop + `CancellationToken`，通过 `DataReceived` 事件抛出解析后的结构体。实现 `IDisposable`。

### 控件层 (`Controls/`)

六个自定义 UserControl，通过依赖属性从 MainWindow 接收数据：

| 控件 | 渲染方式 |
|---|---|
| ChartControl | `OnRender` + `StreamGeometry` 绘制油门/刹车/离合曲线 |
| PedalsControl | XAML `ScaleTransform` 动画驱动的竖条仪表 |
| GearControl | TextBlock 显示档位 (R/N/1-16) |
| SpeedControl | TextBlock 显示速度 + "km/h" |
| RpmLedControl | `OnRender` 绘制 7 个圆形 LED，85%+ RPM 闪烁 |
| SteeringControl | `OnRender` 从 SVG 路径数据绘制矢量方向盘，旋转响应转向 |

### 主窗口 (`MainWindow.xaml.cs`)

应用外壳：创建 UdpReceiver、持有 60 FPS `DispatcherTimer`、向子控件推送数据。支持窗口拖拽、透明度调节、左手模式切换（离合/手刹）。

## 关键设计决策

- **无 MVVM** — `ViewModels/` 目录为空。数据通过 MainWindow code-behind 直接推送到子控件的依赖属性。
- **自定义渲染** — ChartControl、RpmLedControl、SteeringControl 使用 `OnRender` + `DrawingContext` 直接绘制，使用预冻结的 `SolidColorBrush` 避免每帧分配。
- **线程安全** — `lock(_dataLock)` 保护 UDP 接收线程与 UI timer 之间的共享遥测状态。
- **无边框透明覆盖层** — `WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"`。

## 自定义字体

`Fonts/sui generis free.ttf` 在 `App.xaml` 中注册为全局资源 `ForzaFont`，回退字体为 Arial Black。

## 注意事项

- 修改遥测数据结构时，`ForzaTelemetryData` 的字段顺序和 `StructLayout` 必须与 Forza UDP 协议严格匹配，否则反序列化会错位。
- `OnRender` 中的画笔对象必须调用 `Freeze()` 以避免跨线程访问异常和每帧 GC 压力。
- 端口 21337 是 Forza 系列游戏的标准遥测端口，不要随意更改默认值。
- 项目由 WinForms 版本移植而来，参考实现在 `c:\Users\Administrator\Desktop\ui资料\MainForm.cs`。
