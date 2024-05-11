using SharpPcap;
using SharpPcap.LibPcap;
using System.Diagnostics;

namespace Dtwo.Core.Sniffer
{
    public abstract class SnifferBase : IDisposable
    {
        private Process? m_process;
        private string? m_ip;

        private SocketClient? m_client;
        private bool m_started;
        private IReadOnlyCollection<NetStat.NetstatEntry>? m_netStatEntries;

        public bool Started => m_started;
        public Process Process => m_process!;

        public SnifferBase(string processName, string ip, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null)
        {
            var processes = Process.GetProcessesByName(processName);

            if (processes != null && processes.Length > 0)
            {
                m_process = processes[0];
            }

            Init(ip, netStatEntries);
        }

        public SnifferBase(Process process, string ip, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null)
        {
            m_process = process;
            Init(ip, netStatEntries);
        }

        private void Init(string ip, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries)
        {
            m_ip = ip;
            m_netStatEntries = netStatEntries;
        }

        public virtual EStartSniffResult Start(bool refreshNetstatEntries = false)
        {
            if (m_started)
            {
                return EStartSniffResult.AlreadyStarted;
            }

            if (m_process == null)
            {
                return EStartSniffResult.ProcessNotFound;
            }

            if (string.IsNullOrEmpty(m_ip))
            {
                return EStartSniffResult.InvalidIp;
            }

            if (refreshNetstatEntries || m_netStatEntries == null)
            {
                m_netStatEntries = NetStat.GetEntries();
            }

            if (m_netStatEntries == null)
            {
                return EStartSniffResult.NotNetStatEntry;
            }

            var neededEntry = NetStat.GetEntryByProcess(m_netStatEntries, m_process, m_ip);

            if (neededEntry == null)
            {
                return EStartSniffResult.NotNetStatEntry;
            }

            var interfaceIndex = GetInterfaceIndex(neededEntry);

            if (interfaceIndex == -1)
            {
                return EStartSniffResult.InterfaceIndexNotFound;
            }

            m_process = Process.GetProcessById(neededEntry.Pid);
            m_client = SocketClient.ListenIp(neededEntry.LocalAddress, neededEntry.LocalPort, interfaceIndex);

            if (m_client == null)
            {
                return EStartSniffResult.ErrorSocketConnection;
            }

            m_client.OnPacketReceived += OnPacketReceived;
            m_client.OnCaptureStopped += OnStopped;

            m_started = true;
            return EStartSniffResult.Success;
        }

        public virtual void Stop()
        {
            if (m_started == false)
                return;

            m_started = false;

            m_client?.StopListening();
        }

        protected virtual void OnStopped(CaptureStoppedEventStatus status)
        {

        }

        protected virtual void OnPacketReceived(byte[] bytes, int length)
        {

        }

        public void Dispose()
        {
            Stop();

            if (m_client != null)
            {
                m_client.OnCaptureStopped -= OnStopped;
                m_client.OnPacketReceived -= OnPacketReceived;

                m_client.Dispose();
            }
        }

        protected int GetInterfaceIndex(NetStat.NetstatEntry entry)
        {

            var interfaces = LibPcapLiveDeviceList.Instance == null ? LibPcapLiveDeviceList.New().ToList() : LibPcapLiveDeviceList.Instance.ToList();

            if (interfaces == null)
            {
                return -1;
            }

            for (int i = 0; i < interfaces.Count; i++)
            {
                var iface = interfaces[i];

                if (iface == null)
                {
                    return -1;
                }

                if (iface.Addresses == null) continue;
                foreach (var adress in iface.Addresses)
                {
                    if (adress.Addr == null) continue;

                    if (adress.Addr.ipAddress == null) continue;

                    if (adress.Addr.ipAddress.ToString() == entry.LocalAddress)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

    }
}
