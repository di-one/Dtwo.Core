using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Core.Sniffer
{
    internal class SocketClient
    {
        private const int LISTEN_TIMEOUT_MS = 1000;

        internal Action<byte[], int>? OnPacketReceived;
        internal Action<CaptureStoppedEventStatus>? OnCaptureStopped;

        internal readonly ICaptureDevice Device;

        private bool m_started;
        private bool m_disposed;

        internal SocketClient(ICaptureDevice device)
        {
            Device = device;
        }

        internal bool StartListening(string ip, int port)
        {
            if (m_started)
            {
                return false;
            }

            m_started = true;

            // Open the device for capturing
            int readTimeoutMilliseconds = LISTEN_TIMEOUT_MS;
            Device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: readTimeoutMilliseconds);

            string filter = $"host {ip} and dst port {port}";
            Device.Filter = filter;

            Device.OnPacketArrival += PacketArrival;
            Device.OnCaptureStopped += CaptureStopped;

            Device.StartCapture();

            return true;
        }

        internal void StopListening()
        {
            Device.Close();
        }

        private void CaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            OnCaptureStopped?.Invoke(status);
        }

        private void PacketArrival(object sender, PacketCapture e)
        {
            var device = (ICaptureDevice)sender;

            var rawPacket = e.GetPacket();

            if (rawPacket.LinkLayerType == PacketDotNet.LinkLayers.Ethernet)
            {
                var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                OnPacketReceived?.Invoke(packet.PayloadPacket.PayloadPacket.PayloadData, packet.PayloadPacket.PayloadPacket.PayloadData.Length);
            }
        }

        internal static string GetListDevicesMessage()
        {
            var devices = LibPcapLiveDeviceList.Instance;
            int i = 0;
            string message = "";

            foreach (var dev in devices)
            {
                message += String.Format("{0}) {1} {2} \n", i, dev.Name, dev.Description);
                i++;
            }

            return message;
        }

        internal static SocketClient? ListenIp(string ip, int port, int deviceIndex)
        {
            var device = (ICaptureDevice)CaptureDeviceList.New()[deviceIndex];

            SocketClient listener = new SocketClient(device);
            listener.StartListening(ip, port);

            return listener;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    Device.Dispose();
                }

                OnPacketReceived = null;
                OnCaptureStopped = null;

                m_disposed = true;
            }
        }

        ~SocketClient()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
