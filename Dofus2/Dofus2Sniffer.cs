using Dtwo.API;
using Dtwo.API.Dofus2.AnkamaGames.Network.Messages;
using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API.Hybride.Reflection;
using Dtwo.Core.Sniffer;
using Noexia.ApplicationSocketSniffer;
using System.Diagnostics;

namespace Dtwo.Core.Dofus2
{
    internal class Dofus2Sniffer : DofusSnifferBase
    {
        public Dofus2Sniffer(DofusWindow dofusWindow, string processName, string ip, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(dofusWindow, processName, ip, netStatEntries)
        {
        }

        internal Dofus2Sniffer(DofusWindow dofusWindow, Process process, string ip, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(dofusWindow, process, ip, netStatEntries)
        {
        }

        protected override HybrideMessage? GetHybrideMessage(string identifier, DofusMessage message)
        {
            HybrideMessage? hybrideMessage = HybrideMessagesLoader.Instance?.GetDofus2Message(identifier);

            return hybrideMessage;
        }

        protected override void OnGetMessage(DofusMessage message)
        {
            if (message.GetType() == typeof(ReloginTokenStatusMessage))
            {
                LogMessage.LogWarning("Un personnage a été déconnecté", 1);
                Stop();
                return;
            }

            base.OnGetMessage(message);
        }

        protected override void InitHybrideMessage(HybrideMessage hybrideMessage, DofusMessage message)
        {
            hybrideMessage.Init(dofus2Message: message as Dofus2Message);
        }

        protected override MessageParser GetMessageParser()
        {
            return new Dofus2MessageParser();
        }
    }
}
