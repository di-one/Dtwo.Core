using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.Dofus2.Data;
using Dtwo.API.Dofus2.Reflection;
using Dtwo.API.Dofus2.AnkamaGames.Jerakine.Data;
using Dtwo.API;
using Dtwo.Plugins;
using Dtwo.API.DofusBase;
using Dtwo.API.Dofus2.AnkamaGames.Atouin;
using Dtwo.API.Dofus2.Encoding;
using Dtwo.Core.Sniffer;
using System.Diagnostics;

namespace Dtwo.Core.Dofus2
{
    public class Dofus2Core : CoreBase
    {
        public override Task<bool> LoadLang(Action<int, int>? progress = null) => Task.Factory.StartNew(() =>
        {
            try
            {
                LangIsOnLoading = true;
                I18NFileAccessor i18Acessor = new I18NFileAccessor();
                if (i18Acessor.Init(Path.Combine(API.Dofus2.Paths.DofusI18NPath, "i18n_fr.d2i"), progress))
                {
                    I18N i18 = new I18N(i18Acessor);
                    LangIsLoaded = true;
                }

                LangIsOnLoading = false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(ex.ToString(), 1);
                return false;
            }

            LogManager.Log("Les fichiers de langues ont été chargés", 1);
            return true;
        });

        protected override bool CanLoadPlugin(PluginInfos infos)
        {
            return infos.DofusVersion == (int)EDofusVersion.Two || infos.DofusVersion == 2;
        }

        protected override DofusSnifferBase? GetSniffer(DofusWindow dofusWindow, IReadOnlyCollection<NetStat.NetstatEntry> netStatEntries, Process process, List<string> noServerIps)
        {
            if (process.MainWindowTitle.Contains('-') || process.MainWindowTitle.Contains('2') == false) // already connected player (XXXXXX - Dofus 2.X.X.X.X) or launching window
            {
                return null;
            }

            return new Dofus2Sniffer(dofusWindow, process, noServerIps, netStatEntries);
        }

        protected override bool InitPaths()
        {
            API.Dofus2.Paths.Init();

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
            if (DataCenterTypeManager.Init() == false) // reflection loading
                return false;

            MapManager.Init(API.Dofus2.Paths.DofusMapsPath); // todo : path

            return Database.Init(API.Dofus2.Paths.DofusCommonPath); // io loading todo : path
        }

        protected override bool LoadMessages()
        {
            ProtocolTypeManager.Init();
            Dofus2MessagesLoader loader = new Dofus2MessagesLoader();

            return loader.InitializeMessages(API.Paths.Dofus2BindingPath); // todo : path
        }
    }
}
