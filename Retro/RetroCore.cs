using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.Dofus2.Data;
using Dtwo.API.Dofus2.Reflection;
using Dtwo.API.Dofus2.AnkamaGames.Jerakine.Data;
using Dtwo.API;
using Dtwo.API.Retro.Reflection;
using Dtwo.Core.Retro;
using Dtwo.Plugins;
using Dtwo.API.DofusBase;

namespace Dtwo.Core.Dofus2
{
    public class RetroCore : CoreManager
    {
        protected override ThreadProcess GetThreadProcess()
        {
            return new RetroThreadProcess();
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
