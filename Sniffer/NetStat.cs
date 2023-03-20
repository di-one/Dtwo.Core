using System.Diagnostics;
using System.Text.RegularExpressions;
using Dtwo.API;

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

            public NetstatEntry(string protocole, string localAdress, string distanceAdress, string state, int pid)
            {
                string[] localSplit = localAdress.Split(":");
                string[] distantSplit = distanceAdress.Split(":");

                Protocole = protocole;
                LocalAddress = localSplit[0];
                LocalPort = int.Parse(localSplit[1]);
                DistantAdress = distantSplit[0];
                DistantPort = int.Parse(distantSplit[1]);
                State = state;
                Pid = pid;
            }

            public static NetstatEntry Parse(string line)
            {
                try
                {
                    string[] arr = line.Split(',');
                    if (arr.Length != 5)
                    {
                        LogManager.LogWarning("Cant parse netstatEntry " + line);
                        return null;
                    }

                    return new NetstatEntry(arr[0], arr[1], arr[2], arr[3], int.Parse(arr[4]));
                }
                catch (Exception ex)
                {
                    //LogManager.LogError("Error en parse netstat entry : " + ex.ToString());
                    return null;
                }
            }
        }

        public static IReadOnlyCollection<NetstatEntry> GetEntries()
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
            string line = null;

            List<NetstatEntry> entries = new List<NetstatEntry>();
            try
            {

                while ((line = pro.StandardOutput.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                    {
                        line = reg.Replace(line, ",");
                        NetstatEntry entry = NetstatEntry.Parse(line);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            pro.Close();
            return entries;
        }

        public static NetstatEntry? GetEntryByProcess(IReadOnlyCollection<NetstatEntry> entries, Process process)
        {
            foreach(var e in entries)
            {
                if (e.Pid != process.Id) continue;

                if (e.LocalAddress == e.DistantAdress || e.DistantAdress.StartsWith("172") == false) continue;

                return e;
            }

            return null;
        }

        public static IReadOnlyCollection<NetstatEntry> GetEntriesByProcesses(IReadOnlyCollection<NetstatEntry> entries, Process[] process, Func<Process, bool> foreachProcessCallback)
        {
            List<NetstatEntry?> result = new List<NetstatEntry?>();

            foreach (var p in process)
            {
                NetstatEntry? entry = GetEntryByProcess(entries, p);
                if (entry == null) continue;

                if ((foreachProcessCallback != null && foreachProcessCallback.Invoke(p)) || foreachProcessCallback == null)
                {
                    result.Add(entry);
                }
            }

            return result;
        }
    }
}
