using Dtwo.API;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Dtwo.Core.Sniffer
{
    internal class SocketListener
    {
        internal Action<byte[], int> GetPacketAction;
        internal Action<CaptureStoppedEventStatus> OnCaptureStopped;
        internal readonly ICaptureDevice Device;

        private bool _started;
        private bool _disposedValue;

        internal SocketListener(ICaptureDevice device)
        {
            Device = device;
        }

        internal void StartListening(string ip, int port)
        {
            if (_started == false)
            {
                _started = true;

                // Open the device for capturing
                int readTimeoutMilliseconds = 1000;
                Device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: readTimeoutMilliseconds);

                string filter = $"host {ip} and dst port {port}";
                Device.Filter = filter;

                Device.OnPacketArrival += OnPacketArrival;
                Device.OnCaptureStopped += CaptureStopped;
           
                Device.StartCapture();
            }
            else
            {
                LogManager.LogError("Socket Listener", "listener already started !");
            }
        }

        internal void StopListening()
        {
            Device.Close();
        }

        private void CaptureStopped(object sender, CaptureStoppedEventStatus status)
        {
            LogManager.Log("Socket Listener", $"Stop capture {status.ToString()}");

            OnCaptureStopped?.Invoke(status);
        }

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            var device = (ICaptureDevice)sender;

            var rawPacket = e.GetPacket();

            if (rawPacket.LinkLayerType == PacketDotNet.LinkLayers.Ethernet)
            {
                var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                //var ethernetPacket = (EthernetPacket)packet;
                //var tcpPacket = PacketDotNet.TcpPacket.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                GetPacketAction?.Invoke(packet.PayloadPacket.PayloadPacket.PayloadData, packet.PayloadPacket.PayloadPacket.PayloadData.Length);
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

        internal static SocketListener ListenIp(string ip, int deviceIndex)
        {
            SocketListener listener = null;


            var device = (ICaptureDevice)CaptureDeviceList.New()[deviceIndex];
            string[] address = ip.Split(':');
            string host = address[0];
            int port = int.Parse(address[1]);

            if (host == null)
            {
                LogManager.LogError("Socket Listener", "Ip not founded !");
                return null;
            }

            LogManager.Log("Socket Listener", $"Listener created ! {ip}");
            listener = new SocketListener(device);
            listener.StartListening(host, port);


            return listener;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Device.Dispose();
                }

                GetPacketAction = null;

                _disposedValue = true;
            }
        }

        ~SocketListener()
        {
            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
