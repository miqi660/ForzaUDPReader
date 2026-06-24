using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ForzaUDPReader.WPF.Data
{
    /// <summary>
    /// UDP 数据接收器
    /// </summary>
    public class UdpReceiver : IDisposable
    {
        private readonly UdpClient _udpClient;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        private bool _disposed;

        /// <summary>
        /// 接收到数据时触发的事件
        /// </summary>
        public event EventHandler<ForzaTelemetryData> DataReceived;

        /// <summary>
        /// 发生错误时触发的事件
        /// </summary>
        public event EventHandler<Exception> ErrorOccurred;

        /// <summary>
        /// 是否正在接收数据
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 已接收的数据包数量
        /// </summary>
        public long PacketCount { get; private set; }

        /// <summary>
        /// 最后一次接收数据的时间
        /// </summary>
        public DateTime LastReceiveTime { get; private set; }

        /// <summary>
        /// 创建 UDP 接收器
        /// </summary>
        /// <param name="port">监听端口，默认 21337</param>
        public UdpReceiver(int port = 21337)
        {
            Port = port;
            _udpClient = new UdpClient(port);
            _udpClient.Client.ReceiveBufferSize = 1024 * 1024; // 1MB 缓冲区
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            IsRunning = true;
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 停止接收数据
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            _cancellationTokenSource?.Cancel();
            IsRunning = false;

            try
            {
                _receiveTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // 忽略取消异常
            }
        }

        /// <summary>
        /// 接收数据循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();

                    if (result.Buffer.Length >= ForzaTelemetryData.Size)
                    {
                        var data = ForzaTelemetryData.FromBytes(result.Buffer);
                        PacketCount++;
                        LastReceiveTime = DateTime.Now;

                        // 触发数据接收事件
                        DataReceived?.Invoke(this, data);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // UDP 客户端已关闭
                    break;
                }
                catch (SocketException ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke(this, ex);
                    }
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke(this, ex);
                    }
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            _cancellationTokenSource?.Dispose();
            _udpClient?.Dispose();
        }
    }
}
