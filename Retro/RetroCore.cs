using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.Dofus2.Data;
using Dtwo.API.Dofus2.Reflection;
using Dtwo.API.Dofus2.AnkamaGames.Jerakine.Data;
using Dtwo.API;
using Dtwo.API.Retro.Reflection;
using Dtwo.Core.Retro;
using Dtwo.Plugins;
using Dtwo.API.DofusBase;
using Dtwo.Core.Sniffer;
using Noexia.ApplicationSocketSniffer;
using System.Diagnostics;

namespace Dtwo.Core.Dofus2
{
    public class RetroCore : CoreBase
    {
        protected override DofusSnifferBase? GetSniffer(DofusWindow dofusWindow, IReadOnlyCollection<NetStat.NetstatEntry> netStatEntries, Process process, string ip)
        {
            if (process.MainWindowTitle.Contains('-')) // already connected player (XXXXXX - Dofus 1.X.X.X.X) or launching window
            {
                return null;
            }

            return new Dofus2Sniffer(dofusWindow, process, ip, netStatEntries);
        }
        protected override bool InitPaths()
        {
            // todo
            return true;
        }

        protected override bool LoadAdditionalData()
        {
            return true;
        }

        protected override bool LoadCoreData()
        {
            return true;
        }

        protected override bool LoadDofusData()
        {
            return true;
        }

        protected override bool LoadMessages()
        {
            RetroMessagesLoader loader = new RetroMessagesLoader();

            return loader.InitializeMessages(Paths.RetroBindingPath);
        }

        protected override bool CanLoadPlugin(PluginInfos infos)
        {
            return infos.DofusVersion == (int)EDofusVersion.Retro || infos.DofusVersion == 2;
        }
    }
}
