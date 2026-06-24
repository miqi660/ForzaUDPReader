# Forza Horizon 6 UDP Reader

Forza Horizon 6 UDP 遥测数据实时读取工具，提供图形化 HUD 界面显示车辆状态和驾驶输入。

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)
![Windows](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## 功能特性

- **实时遥测数据接收** - 通过 UDP 协议接收 FH6 游戏数据（端口 21337）
- **轨迹线显示** - 油门、刹车、离合、转向的实时曲线
- **踏板状态** - 离合(C)、刹车(B)、油门(T) 的百分比条形图
- **车辆状态** - 7 个圆形 RPM LED 指示灯（3绿+2黄+2红，高转速爆闪）、档位显示（支持 R/N/1-16）、速度显示
- **方向盘** - 实时转向角度可视化（SVG 风格设计）
- **自定义字体** - 使用 "Sui Generis Free" 字体显示档位和速度
- **转速缓存** - 自动缓存最大转速值，避免颠簸路面时转速条不稳定

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
- Forza Horizon 6（需要开启 UDP 数据输出）

## 使用方法

### 从 Release 下载

1. 从 [Releases](../../releases) 下载最新版本
2. 解压到任意目录
3. 运行 `ForzaUDPReader.exe`
4. 启动 Forza Horizon 6，工具会自动接收游戏数据

### 从源码构建

```bash
git clone https://github.com/你的用户名/ForzaUDPReader.git
cd ForzaUDPReader
dotnet build
```

运行：

```bash
dotnet run
```

发布独立版本：

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Forza Horizon 6 设置

在 Forza Horizon 6 游戏中启用 UDP 数据输出：

1. 进入游戏设置
2. 找到 "HUD and Gameplay" 或类似选项
3. 启用 "Data Out" 功能
4. 设置 "Data Out IP Address" 为 `127.0.0.1`（本机）
5. 设置 "Data Out Port" 为 `21337`（默认端口）

## 项目结构

```
ForzaUDPReader/
├── Fonts/
│   └── sui generis free.ttf    # 自定义字体
├── ForzaTelemetryData.cs       # 324 字节遥测数据结构
├── UdpReceiver.cs               # UDP 数据接收器
├── MainForm.cs                  # 主界面和 HUD 绘制
├── MainForm.Designer.cs         # 窗体设计器
└── Program.cs                   # 程序入口
```

## 数据格式

工具使用 Forza Horizon 6 官方 UDP 数据格式（324 字节），主要字段：

| 数据 | 类型 | 说明 |
|------|------|------|
| IsRaceOn | int | 比赛是否进行中 |
| SpeedKmh | float | 速度（km/h） |
| RpmCurrent | float | 当前转速 |
| RpmMax | float | 最大转速 |
| Gear | int | 档位（0=R, 11=N, 1-16=前进挡） |
| ThrottlePercent | float | 油门百分比（0-100） |
| BrakePercent | float | 刹车百分比（0-100） |
| ClutchPercent | float | 离合百分比（0-100） |
| SteerPercent | float | 转向百分比（-100 到 100） |

完整字段说明参见 `FH6遥测数据结构.md`。

## 自定义配置

默认配置：

- UDP 监听端口：`21337`
- 窗体大小：`680 × 220` 像素
- 历史数据点：`200` 个
- RPM LED 数量：`7` 个（3绿+2黄+2红）
- 爆闪阈值：转速 ≥ 90% 时触发

如需修改端口，编辑 `MainForm.cs` 中的 `InitializeReceiver()` 方法：

```csharp
_receiver = new UdpReceiver(21337); // 修改此端口号
```

## 许可证

[MIT License](LICENSE)

## 致谢

- Forza Horizon 6 官方数据输出文档
- benofficial2 的 iRacing Inputs 项目
