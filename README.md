# ForzaUDPReader.WPF

基于 WPF 的 Forza Horizon 6 遥测数据 HUD 显示器。通过 UDP 接收游戏数据，实时显示各项遥测信息。

## 功能特性

### 已实现的功能

- **实时遥测数据接收** - 通过 UDP 协议接收 Forza Horizon 6 的游戏数据
- **仪表盘 HUD 界面** - 悬浮窗显示，置顶显示，支持透明背景
- **自定义控件系统** - 模块化设计的独立控件：
  - **ChartControl** - 数据图表显示
  - **GearControl** - 档位显示
  - **PedalsControl** - 踏板状态显示（油门/刹车/离合）
  - **RpmLedControl** - RPM 指示灯
  - **SpeedControl** - 速度显示
  - **SteeringControl** - 转向角度显示

### 界面特性

- 无边框透明窗口设计
- 鼠标悬停显示控制按钮
- 支持窗口拖拽移动
- 右上角齿轮按钮打开透明度调节滑块
- 支持窗口缩放（右下角拖拽）
- 关闭按钮

## 系统要求

- Windows 10/11
- .NET 8.0
- Forza Horizon 6（需开启 UDP 数据输出）

## 使用方法

1. 启动 Forza Horizon 6
2. 在游戏设置中开启 UDP 数据输出（端口默认为 5300）
3. 运行 ForzaUDPReader.WPF 程序
4. HUD 将自动显示遥测数据

## 项目结构

```
ForzaUDPReader.WPF/
├── App.xaml                    # 应用程序配置
├── MainWindow.xaml             # 主窗口界面
├── MainWindow.xaml.cs          # 主窗口逻辑
├── Controls/                   # 自定义控件
│   ├── ChartControl.xaml/cs    # 图表控件
│   ├── GearControl.xaml/cs     # 档位控件
│   ├── PedalsControl.xaml/cs   # 踏板控件
│   ├── RpmLedControl.xaml/cs   # RPM 指示灯控件
│   ├── SpeedControl.xaml/cs    # 速度控件
│   └── SteeringControl.xaml/cs # 转向控件
└── README.md
```

## 开发技术

- C# / .NET 8.0
- WPF (Windows Presentation Foundation)
- UDP 网络通信
- 自定义 UserControls

## 许可证

MIT License