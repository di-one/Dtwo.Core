using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Core
{
    internal static class ProcessManager
    {
        internal const string DOFUS_PROCESS_NAME = "dofus";
        internal static Process[] GetDofusProcesses()
        {
            return Process.GetProcessesByName(DOFUS_PROCESS_NAME);
        }
    }
}
