using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.Dofus2.Data;
using Dtwo.API.Dofus2.Reflection;
using Dtwo.API.Dofus2.AnkamaGames.Jerakine.Data;
using Dtwo.API;
using Dtwo.Plugins;
using Dtwo.API.DofusBase;
using Dtwo.API.Dofus2.AnkamaGames.Atouin;
using Dtwo.API.Dofus2.Encoding;

namespace Dtwo.Core.Dofus2
{
    public class Dofus2Core : CoreManager
    {
        public bool LangIsOnLoading { get; private set; }
        public bool LangIsLoaded { get; private set; }

        public override Task<bool> LoadLang(Action<int, int> progress = null) => Task.Factory.StartNew(() =>
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

        protected override ThreadProcess GetThreadProcess()
        {
            return new Dofus2ThreadProcess();
        }

        protected override bool InitPaths()
        {
            API.Dofus2.Paths.Init();

            return true;
        }

        protected override bool LoadAdditionalData()
        {
            //Database.LoadTeleporters(""); // todo : worldpathfinding plugin

            return true;
        }

        protected override bool LoadCoreData()
        {
            return true;
        }

        protected override bool LoadDofusData()
        {
            DataCenterTypeManager.Init(); // reflection loading

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
