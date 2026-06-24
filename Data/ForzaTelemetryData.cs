using System;
using System.Runtime.InteropServices;

namespace ForzaUDPReader.WPF.Data
{
    /// <summary>
    /// Forza Horizon 6 UDP 遥测数据结构
    /// 总数据包大小：324字节
    /// 参考: FH6遥测数据结构.md
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ForzaTelemetryData
    {
        // 比赛开始时=1，菜单/比赛中停止时为0
        public int IsRaceOn; // S32

        // 时间戳(毫秒)，最终可能会溢出到0
        public uint TimestampMS; // U32

        // 发动机转速值
        public float EngineMaxRpm; // F32
        public float EngineIdleRpm; // F32
        public float CurrentEngineRpm; // F32

        // 在车辆的本地空间;X = 右，Y = 向上，Z = 前进
        public float AccelerationX; // F32
        public float AccelerationY; // F32
        public float AccelerationZ; // F32

        // 在车辆的本地空间;X = 右，Y = 向上，Z = 前进
        public float VelocityX; // F32
        public float VelocityY; // F32
        public float VelocityZ; // F32

        // 车辆局部空间的角速度（rad/s）;X = 俯仰，Y = 偏航，Z = 滚转
        public float AngularVelocityX; // F32
        public float AngularVelocityY; // F32
        public float AngularVelocityZ; // F32

        // 车体方向（弧度）
        public float Yaw; // F32
        public float Pitch; // F32
        public float Roll; // F32

        // 悬挂行程归一化：0.0f = 最大拉伸;1.0 = 最大压缩比
        public float NormalizedSuspensionTravelFrontLeft; // F32
        public float NormalizedSuspensionTravelFrontRight; // F32
        public float NormalizedSuspensionTravelRearLeft; // F32
        public float NormalizedSuspensionTravelRearRight; // F32

        // 轮胎归一化滑移比，= 0 表示抓地力为100%，|比率|>1.0意味着抓地力下降
        public float TireSlipRatioFrontLeft; // F32
        public float TireSlipRatioFrontRight; // F32
        public float TireSlipRatioRearLeft; // F32
        public float TireSlipRatioRearRight; // F32

        // 车轮转速弧度/秒
        public float WheelRotationSpeedFrontLeft; // F32
        public float WheelRotationSpeedFrontRight; // F32
        public float WheelRotationSpeedRearLeft; // F32
        public float WheelRotationSpeedRearRight; // F32

        // 当轮子在震动条上时 = 1，关闭时 = 0
        public int WheelOnRumbleStripFrontLeft; // S32
        public int WheelOnRumbleStripFrontRight; // S32
        public int WheelOnRumbleStripRearLeft; // S32
        public int WheelOnRumbleStripRearRight; // S32

        // 当车轮在水坑中时 = 1，未在水坑中时 = 0
        public int WheelInPuddleFrontLeft; // S32
        public int WheelInPuddleFrontRight; // S32
        public int WheelInPuddleRearLeft; // S32
        public int WheelInPuddleRearRight; // S32

        // 传递给控制器力反馈的非维表面震动值
        public float SurfaceRumbleFrontLeft; // F32
        public float SurfaceRumbleFrontRight; // F32
        public float SurfaceRumbleRearLeft; // F32
        public float SurfaceRumbleRearRight; // F32

        // 轮胎归一化滑移角，= 0 表示100%抓地力，|角度|>1.0意味着抓地力下降
        public float TireSlipAngleFrontLeft; // F32
        public float TireSlipAngleFrontRight; // F32
        public float TireSlipAngleRearLeft; // F32
        public float TireSlipAngleRearRight; // F32

        // 轮胎归一化合并打滑，= 0表示100%抓地力，|打滑|> 1.0表示抓地力丧失
        public float TireCombinedSlipFrontLeft; // F32
        public float TireCombinedSlipFrontRight; // F32
        public float TireCombinedSlipRearLeft; // F32
        public float TireCombinedSlipRearRight; // F32

        // 实际悬挂行程（米）
        public float SuspensionTravelMetersFrontLeft; // F32
        public float SuspensionTravelMetersFrontRight; // F32
        public float SuspensionTravelMetersRearLeft; // F32
        public float SuspensionTravelMetersRearRight; // F32

        // 汽车品牌/型号的唯一ID
        public int CarOrdinal; // S32

        // 在0（D组——最差车型）到7（X组——最佳车辆）之间
        public int CarClass; // S32

        // 介于100（最差车）到999（最好车）之间
        public int CarPerformanceIndex; // S32

        // 0 = 前驱，1 = 后驱，2 = 全驱
        public int DrivetrainType; // S32

        // 发动机缸数
        public int NumCylinders; // S32

        // 车辆组标识符 (FH6特有)
        public uint CarGroup; // U32

        // 可碰撞物体碰撞的速度损失（m/s）(FH6特有)
        public float SmashableVelDiff; // F32

        // 最近撞击的可击碎物体质量（公斤）(FH6特有)
        public float SmashableMass; // F32

        // 世界空间中的位置（米）
        public float PositionX; // F32
        public float PositionY; // F32
        public float PositionZ; // F32

        // 速度单位为米每秒
        public float Speed; // F32

        // 功率单位：瓦特
        public float Power; // F32

        // 扭矩（牛顿米）
        public float Torque; // F32

        // 轮胎温度
        public float TireTempFrontLeft; // F32
        public float TireTempFrontRight; // F32
        public float TireTempRearLeft; // F32
        public float TireTempRearRight; // F32

        // 涡轮增压/机械增压器增压（PSI高于大气压）
        public float Boost; // F32

        // 油量（0.0 = 空，1.0 = 满）
        public float Fuel; // F32

        // 总行驶距离（米）
        public float DistanceTraveled; // F32

        // 圈速（秒）;如果不适用，则为0.0
        public float BestLap; // F32
        public float LastLap; // F32
        public float CurrentLap; // F32

        // 总比赛时间（自起跑秒数）
        public float CurrentRaceTime; // F32

        // 完成的圈数
        public ushort Lap; // U16

        // 当前比赛排名
        public byte RacePosition; // U8

        // 玩家输入（0到255）
        public byte Accelerator; // U8
        public byte Brake; // U8
        public byte Clutch; // U8
        public byte Handbrake; // U8

        // 现用齿轮
        public byte Gear; // U8

        // 转向输入（-127 = 全左，0 = 中心，127 = 全右）
        public sbyte Steer; // S8

        // 归一化驱动线位置（-127到127）
        public sbyte NormalizedDrivingLine; // S8

        // 归一化AI刹车差（-127到127）
        public sbyte NormalizedAIBrakeDifference; // S8

        /// <summary>
        /// 获取速度 (km/h)
        /// </summary>
        public float SpeedKmh => Speed * 3.6f;

        /// <summary>
        /// 获取速度 (mph)
        /// </summary>
        public float SpeedMph => Speed * 2.237f;

        /// <summary>
        /// 获取油门百分比 (0-100)
        /// </summary>
        public float ThrottlePercent => Accelerator / 255f * 100f;

        /// <summary>
        /// 获取刹车百分比 (0-100)
        /// </summary>
        public float BrakePercent => Brake / 255f * 100f;

        /// <summary>
        /// 获取离合器百分比 (0-100)
        /// </summary>
        public float ClutchPercent => Clutch / 255f * 100f;

        /// <summary>
        /// 获取手刹百分比 (0-100)
        /// </summary>
        public float HandbrakePercent => Handbrake / 255f * 100f;

        /// <summary>
        /// 获取燃油百分比 (0-100)
        /// </summary>
        public float FuelPercent => Fuel * 100f;

        /// <summary>
        /// 获取档位字符串
        /// Forza Horizon 6 档位值: 0=R, 1-16=前进档, 11=N
        /// </summary>
        public string GearString
        {
            get
            {
                if (Gear == 0) return "R";
                if (Gear == 11) return "N";
                if (Gear >= 1 && Gear <= 16) return Gear.ToString();
                return "N";
            }
        }

        /// <summary>
        /// 获取转向角度（-100 到 100）
        /// </summary>
        public float SteerPercent => Steer / 127f * 100f;

        /// <summary>
        /// 获取传动系统类型字符串
        /// </summary>
        public string DrivetrainTypeString
        {
            get
            {
                switch (DrivetrainType)
                {
                    case 0: return "FWD";
                    case 1: return "RWD";
                    case 2: return "AWD";
                    default: return "Unknown";
                }
            }
        }

        /// <summary>
        /// 获取当前圈时间字符串
        /// </summary>
        public string CurrentLapTimeString => TimeSpan.FromSeconds(CurrentLap).ToString(@"mm\:ss\.fff");

        /// <summary>
        /// 获取最佳圈速时间字符串
        /// </summary>
        public string BestLapTimeString => BestLap > 0 ? TimeSpan.FromSeconds(BestLap).ToString(@"mm\:ss\.fff") : "--:--.---";

        /// <summary>
        /// 获取上一圈时间字符串
        /// </summary>
        public string LastLapTimeString => LastLap > 0 ? TimeSpan.FromSeconds(LastLap).ToString(@"mm\:ss\.fff") : "--:--.---";

        /// <summary>
        /// 获取当前比赛时间字符串
        /// </summary>
        public string CurrentRaceTimeString => TimeSpan.FromSeconds(CurrentRaceTime).ToString(@"mm\:ss\.fff");

        /// <summary>
        /// 从字节数组解析数据
        /// </summary>
        public static ForzaTelemetryData FromBytes(byte[] data)
        {
            if (data == null || data.Length < Size)
            {
                throw new ArgumentException($"数据长度不足，需要 {Size} 字节，实际 {data?.Length ?? 0} 字节");
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<ForzaTelemetryData>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// 获取数据结构大小 (324字节)
        /// </summary>
        public static int Size => Marshal.SizeOf<ForzaTelemetryData>();
    }
}
