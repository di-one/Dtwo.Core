using Dtwo.API.Hybride;
using Dtwo.API;
using Noexia.ApplicationSocketSniffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Core
{
    public class DofusWindowsFinder
    {
        public static IReadOnlyCollection<NetStat.NetstatEntry> GetProcesses()
        {
            List<DofusWindow> dofusWindows = DofusWindow.WindowsList;

            Process[] processes = null;

            if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Two)
            {
                processes = Process.GetProcessesByName("dofus");
            }
            else
            {
                processes = Process.GetProcessesByName("Dofus Retro");
            }

            var allEntries = NetStat.GetEntries();
            var availableEntries = NetStat.GetEntriesByProcesses(
                allEntries, processes,
                (process) =>
                {
                    if (dofusWindows.Find(x => x.Process.Id == process.Id) != null) // already listenned window
                    {
                        return false;
                    }

                    if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Two)
                    {
                        if (process.MainWindowTitle.Contains('-') || process.MainWindowTitle.Contains('2') == false) // already connected player (XXXXXX - Dofus 2.X.X.X.X) or launching window
                        {
                            return false;
                        }
                    }
                    else if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Retro)
                    {
                        if (process.MainWindowTitle.Contains('-')) // already connected player
                        {
                            return false;
                        }
                    }

                    return true;
                }, "172");


            return availableEntries;
        }
        public static int GetAlreadyConnectedWindows(bool findAlreadyAdded = false)
        {
            int count = 0;

            Process[] processes = null;

            if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Two)
            {
                processes = Process.GetProcessesByName("dofus");
            }
            else
            {
                processes = Process.GetProcessesByName("Dofus Retro");
            }

            for (int i = 0; i < processes.Length; i++)
            {
                var process = processes[i];

                if (findAlreadyAdded == false && DofusWindow.WindowsList.Find(x => x.Process.Id == process.Id) != null)
                {
                    continue;
                }

                if (process.MainWindowTitle.Contains('-')) // already connected player (XXXXXX - Dofus 2.X.X.X.X)
                {
                    count++;
                }

                //if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Two)
                //{

                //}
                //else if (HybrideManager.DofusVersion == API.DofusBase.EDofusVersion.Retro)
                //{

                //}
            }

            return count;
        }
    }
}
