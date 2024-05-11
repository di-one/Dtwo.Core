using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Core.Sniffer
{
    public enum EStartSniffResult
    {
        Success,
        AlreadyStarted,
        ProcessNotFound,
        InvalidIp,
        NotNetStatEntry,
        ErrorSocketConnection,
        InterfaceIndexNotFound
    }
}
