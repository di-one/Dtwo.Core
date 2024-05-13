using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dtwo.Core.Sniffer
{
    public class NetStat
    {
        public class NetstatEntry
        {
            public readonly string Protocole;
            public readonly string LocalAddress;
            public readonly int LocalPort;
            public readonly string DistantAdress;
            public readonly int DistantPort;
            public readonly string State;
            public readonly int Pid;

            public NetstatEntry(string protocole, string localADdress, int localPort, string distantAdress, int distantPort, string state, int pid)
            {
                Protocole = protocole;
                LocalAddress = localADdress;
                LocalPort = localPort;
                DistantAdress = distantAdress;
                DistantPort = distantPort;
                State = state;
                Pid = pid;
            }

            public static NetstatEntry? Parse(string protocole, string localAdress, string distanceAdress, string state, int pid)
            {
                string[] localSplit = localAdress.Split(":");
                string[] distantSplit = distanceAdress.Split(":");

                if (localSplit.Length < 2 || distantSplit.Length < 2)
                {
                    return null;
                }

                string localAddress = localSplit[0];
                int localPort;
                if (int.TryParse(localSplit[1], out localPort) == false)
                {
                    return null;
                }

                string distantAddress = distantSplit[0];
                int distantPort;
                if (int.TryParse(distantSplit[1], out distantPort) == false)
                {
                    return null;
                }

                return new NetstatEntry(protocole, localAddress, localPort, distantAddress, distantPort, state, pid);
            }

            public static NetstatEntry? Parse(string line)
            {
                try
                {
                    string[] arr = line.Split(',');
                    if (arr.Length != 5)
                    {
                        return null;
                    }

                    string protocole = arr[0];
                    string localAdress = arr[1];
                    string distantAdress = arr[2];
                    string state = arr[3];
                    string pidStr = arr[4];
                    int pid;

                    if (string.IsNullOrEmpty(pidStr) || int.TryParse(pidStr, out pid) == false)
                    {
                        return null;
                    }

                    if (string.IsNullOrEmpty(protocole) || string.IsNullOrEmpty(localAdress) || string.IsNullOrEmpty(distantAdress) || string.IsNullOrEmpty(state))
                    {
                        return null;
                    }
                    var entry = NetstatEntry.Parse(protocole, localAdress, distantAdress, state, pid);

                    return entry;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("error on parse entry : " + ex);
                    return null;
                }
            }
        }

        public static IReadOnlyCollection<NetstatEntry>? GetEntries()
        {
            Process pro = new Process();
            pro.StartInfo.FileName = "cmd.exe";
            pro.StartInfo.UseShellExecute = false;
            pro.StartInfo.RedirectStandardInput = true;
            pro.StartInfo.RedirectStandardOutput = true;
            pro.StartInfo.RedirectStandardError = true;
            pro.StartInfo.CreateNoWindow = true;
            pro.Start();
            pro.StandardInput.WriteLine("netstat -ano");
            pro.StandardInput.WriteLine("exit");
            Regex reg = new Regex("\\s+", RegexOptions.Compiled);
            string? line = null;

            List<NetstatEntry> entries = new List<NetstatEntry>();
            try
            {
                while ((line = pro.StandardOutput.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                    {
                        line = reg.Replace(line, ",");
                        NetstatEntry? entry = NetstatEntry.Parse(line);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("error on get entries : " + ex.Message);
                return null;
            }

            pro.Close();
            return entries;
        }

        public static NetstatEntry? GetEntryByProcess(IReadOnlyCollection<NetstatEntry> entries, Process process, string ip)
        {
            foreach (var e in entries)
            {
                if (e.Pid != process.Id) continue;

                if (e.LocalAddress == e.DistantAdress || e.DistantAdress.StartsWith(ip) == false)
                {
                    Debug.WriteLine("distant addr : " + e.DistantAdress);
                    continue;
                }

                return e;
            }

            return null;
        }

        public static IReadOnlyCollection<NetstatEntry?> GetEntriesByProcesses(IReadOnlyCollection<NetstatEntry> entries, Process[] process, Func<Process, bool> foreachProcessCallback, string ip)
        {
            Debug.WriteLine("GetEntriesByProcesses");

            List<NetstatEntry?> result = new List<NetstatEntry?>();

            foreach (var p in process)
            {
                NetstatEntry? entry = GetEntryByProcess(entries, p, ip);
                if (entry == null) continue;

                if ((foreachProcessCallback != null && foreachProcessCallback.Invoke(p)) || foreachProcessCallback == null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        public static bool NpcapIsInstalled()
        {
            try
            {
                LibPcapLiveDeviceList.Instance.ToList();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
