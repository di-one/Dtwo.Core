using Dtwo.API;
using Dtwo.API.Retro.Reflection;

using Dtwo.Plugins;
using Dtwo.API.DofusBase;
using Dtwo.Core.Sniffer;
using System.Diagnostics;
using Dtwo.Core.Retro;

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

            return new DofusRetroSniffer(dofusWindow, process, ip, netStatEntries);
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
