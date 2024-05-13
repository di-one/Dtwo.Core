using System.Diagnostics;
using System.Reflection;
using System.Text;
using Dtwo.API;
using Dtwo.Core.Sniffer;
using Dtwo.Core.Configuration;
using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.Plugins;
using Dtwo.API.DofusBase;
using Dtwo.API.Hybride;
using SharpPcap.LibPcap;
using Dtwo.API.Dofus2.AnkamaGames.Dofus.DataCenter;
using Dtwo.Core.Dofus2;
using Dtwo.Core.Retro;
using Dtwo.API.Inputs;

namespace Dtwo.Core
{
    public abstract class CoreBase
    {
        public CoreConfiguration? Configuration { get; private set; }
        public bool LangIsLoaded { get; protected set; }
        public bool PluginsIsLoaded { get; protected set; }
        public bool LangIsOnLoading { get; protected set; }

        private Dictionary<Process, DofusSnifferBase> m_sniffers = new();
        private string? m_configPath;
        private bool m_isStarted;

        public bool IsStarted => m_isStarted;
        
        public void Start(EDofusVersion dofusVersion, List<byte[]>? plugins, Action<bool>? finishCallback = null, Action<int>? progressionStep = null)
        {
            if (m_isStarted)
            {
                LogManager.LogWarning(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "CoreManager already started", 1);
                finishCallback?.Invoke(false);
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    OnStart();

                    m_configPath = Path.Combine(API.Paths.DtwoBasePath, "core_configuration.json");

                    LoadConfiguration();

                    if (InitPaths() == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load paths", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadCoreData() == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load core data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadDofusData() == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load dofus data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadAdditionalData() == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load additional data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadMessages() == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load messages", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    // Todo : options

                    InitHybride(dofusVersion);

                    if (LoadPlugins(plugins, progressionStep) == false)
                    {
                        LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on load plugin", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    PluginsIsLoaded = true;

                    //Plugins.PluginsManager.OnCoreLoaded();
                    OnStartEnd(true);
                    finishCallback?.Invoke(true);
                }
                catch (Exception ex)
                {
                    LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(Start)}", 
                            "Error on start core (see the logs for few informations)", 1);
                    LogManager.LogError(ex.ToString());
                    OnStartEnd(false);
                    finishCallback?.Invoke(false);
                }
            });
        }

        protected virtual void OnStart()
        {

        }

        protected virtual void OnStartEnd(bool result)
        {
            m_isStarted = true;
        }

        protected abstract bool InitPaths();

        protected abstract bool LoadDofusData();

        protected abstract bool LoadCoreData();

        protected abstract bool LoadMessages();

        private void LoadConfiguration()
        {
            if (File.Exists(m_configPath) == false)
            {
                Configuration = new CoreConfiguration();
                SaveConfiguration();
            }
            else
            {
                Configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<CoreConfiguration>(File.ReadAllText(m_configPath));
            }
        }

        private void InitHybride(EDofusVersion dofusVersion)
        {
            HybrideManager.Init(dofusVersion, Path.Combine(Paths.HybrideBindingPath)); // todo : path
        }

        public void SaveConfiguration()
        {
            try
            {
                if (m_configPath == null)
                {
                    LogManager.LogError("Le chemin de la configuration est introuvable", 1);
                    return;
                }

                File.WriteAllText(m_configPath, Newtonsoft.Json.JsonConvert.SerializeObject(Configuration));
            }
            catch (Exception ex)
            {
                LogManager.LogError("Une erreur est survenue pendant la sauvegarde la configuration", 1);
                LogManager.LogError(ex.ToString());
                return;
            }

            LogManager.Log("Configuration sauvegardée", 1);
        }

        protected abstract bool LoadAdditionalData();

        public virtual Task<bool> LoadLang(Action<int, int>? progress = null) => Task.Factory.StartNew(() => { return true; });

        public void StopListenProcess(DofusWindow dofusWindow)
        {
            bool b = m_sniffers.TryGetValue(dofusWindow.Process, out DofusSnifferBase? sniffer);
            if (sniffer != null)
            {
                sniffer.Stop();
            }
        }

        public void StopListenProcesses()
        {
            foreach(var sniffer in m_sniffers)
            {
                sniffer.Value.Stop();
                sniffer.Value.Dispose();
            }

            m_sniffers.Clear();
        }

        public void StartupListenProcesses(string ip, Action<bool, int>? callbackFinish = null)
        {
            Task.Factory.StartNew(() =>
            {
                var entries = DofusWindowsFinder.GetProcesses();
                int added = 0;

                List<Process> processes = new List<Process>();
                foreach (var e in entries)
                {
                    var process = Process.GetProcessById(e.Pid);
                    if (process == null)
                    {
                        LogManager.LogError($"Process not found : {e.Pid}", 1);
                        return;
                    }

                    processes.Add(process);
                }

                if (entries == null || entries.Count == 0)
                {
                    callbackFinish?.Invoke(false, 0);
                    return;
                }

                foreach (var process in processes)
                {
                    DofusWindow dofusWindow = new DofusWindow(process, HybrideManager.DofusVersion == EDofusVersion.Retro);
                    DofusSnifferBase? sniffer = GetSniffer(dofusWindow, entries, process, ip);

                    if (sniffer == null)
                    {
                        continue;
                    }

                    if (sniffer.Process == null)
                    {
                        continue;
                    }
                    if (m_sniffers != null && m_sniffers.FirstOrDefault(x => x.Key.Id == sniffer.Process.Id).Value != null)
                    {
                        continue;
                    }

                    if (m_sniffers == null)
                        m_sniffers = new();


                    m_sniffers.Add(sniffer.Process, sniffer);
                    DofusWindow.WindowsList.Add(dofusWindow);

                    sniffer.OnStop += OnSnifferStopped;

                    try
                    {
                        var result = sniffer.Start(true);
                        if (result != EStartSniffResult.Success)
                        {
                            LogManager.LogError("Impossible de démarrer le sniffer " + result.ToString(), 1);
                            callbackFinish?.Invoke(false, 0);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        callbackFinish?.Invoke(false, 0);
                        LogManager.LogError("Impossible de démarrer le sniffer : " + ex, 1);
                        return;
                    }

                    added++;
                }

                callbackFinish?.Invoke(true, added);
            });
        }

        protected abstract DofusSnifferBase? GetSniffer(DofusWindow dofusWindow, IReadOnlyCollection<NetStat.NetstatEntry> netStatEntries, Process process, string ip);
        
        private bool LoadPlugins(List<byte[]>? bytes, Action<int>? progressionStep = null)
        {
            int loadedPlugins = Plugins.PluginsManager.LoadPlugins(bytes, Paths.PLUGINS_ABOSLUTE_PATH, progressionStep, CanLoadPlugin);

            if (loadedPlugins == -1)
            {
                LogManager.LogError(
                            $"{nameof(CoreBase)}.{nameof(LoadPlugins)}", 
                            "Une erreur est survenue pendant le chargement des mods", 1);
                return false;
            }
            else if (loadedPlugins > 0)
            {
                LogManager.Log(
                            $"{nameof(CoreBase)}.{nameof(LoadPlugins)}", 
                            $"{loadedPlugins} mods ont été chargés avec succès", 1);
            }
            else
            {
                LogManager.LogWarning(
                            $"{nameof(CoreBase)}.{nameof(LoadPlugins)}", 
                            $"Aucun plugin n'a été chargé", 1);
            }

            return true;
        }

        protected abstract bool CanLoadPlugin(PluginInfos infos);

        public void Stop()
        {
            for (int i = 0; i < m_sniffers.Count; i++)
            {
                m_sniffers.ElementAt(i).Value.Stop();
            }

            m_sniffers = new();
            m_isStarted = false;
        }

        private void OnSnifferStopped(DofusSnifferBase sniffer)
        {
            LogManager.Log("OnSnifferStopped");

            m_sniffers.Remove(sniffer.DofusWindow.Process);
            DofusWindow.WindowsList.Remove(sniffer.DofusWindow);
            DofusWindow.OnDofusWindowStoped?.Invoke(sniffer.DofusWindow);

            if (DofusWindow.Selected == sniffer.DofusWindow)
            {
                if (DofusWindow.WindowsList.Count > 0)
                {
                    DofusWindow.SelectDofusWindow(DofusWindow.WindowsList[0]);
                }
            }
        }
        
    }
}
