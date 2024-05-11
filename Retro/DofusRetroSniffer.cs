﻿using Dtwo.API;
using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API.Hybride.Reflection;
using Dtwo.API.Retro.Network.Messages;
using Dtwo.Core.Sniffer;
using Noexia.ApplicationSocketSniffer;
using System.Diagnostics;

namespace Dtwo.Core.Retro
{
    internal class DofusRetroSniffer : DofusSnifferBase
    {
        public DofusRetroSniffer(DofusWindow dofusWindow, string processName, string ip, int deviceIndex, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(dofusWindow,processName, ip, netStatEntries)
        {
        }

        internal DofusRetroSniffer(DofusWindow dofusWindow,Process process, string ip, int deviceIndex, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(dofusWindow, process, ip, netStatEntries)
        {
        }


        protected override HybrideMessage? GetHybrideMessage(string identifier, DofusMessage message)
        {
            HybrideMessage? hybrideMessage = HybrideMessagesLoader.Instance.GetRetroMessage(identifier);

            return hybrideMessage;
        }

        protected override void OnGetMessage(DofusMessage message)
        {
            //if (message.GetType() == typeof(ReloginTokenStatusMessage))
            //{
            //    LogMessage.LogWarning("Un personnage a été déconnecté", 1);
            //    Stop();
            //    return;
            //}

            base.OnGetMessage(message);
        }

        protected override void InitHybrideMessage(HybrideMessage hybrideMessage, DofusMessage message)
        {
            hybrideMessage.Init(retroMessage: message as RetroMessage);
        }

        protected override MessageParser GetMessageParser()
        {
            return new RetroMessageParser();
        }
    }
}