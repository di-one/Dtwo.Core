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

namespace Dtwo.Core
{
    public abstract class CoreManager
    {
        public CoreConfiguration Configuration { get; private set; }
        public bool LangIsLoaded { get; private set; }
        public bool PluginsIsLoaded { get; private set; }
        public bool LangIsOnLoading { get; private set; }

        private Dictionary<Process, ThreadProcess> m_threads = new();
        private string m_configPath;
        private bool m_isStarted;
        
        public void Start(EDofusVersion dofusVersion, List<byte[]> plugins, Action<bool> finishCallback = null, Action<int> progressionStep = null)
        {
            if (m_isStarted)
            {
                LogManager.LogWarning("CoreManager already started", 1);
                finishCallback?.Invoke(false);
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    OnStart();


                    API.Paths.Init();
                    m_configPath = Path.Combine(API.Paths.DtwoBasePath, "core_configuration.json");

                    LoadConfiguration();

                    InitHybride(dofusVersion);

                    if (InitPaths() == false)
                    {
                        LogManager.LogError("Error on load paths", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadCoreData() == false)
                    {
                        LogManager.LogError("Error on load core data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadDofusData() == false)
                    {
                        LogManager.LogError("Error on load dofus data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    Console.WriteLine("A");
                    var monsters = Dtwo.API.Dofus2.Data.Database.GetData<MapPosition>();
                    LogManager.Log($"monsters is null ? {(monsters == null).ToString()}, monsters count : {monsters?.Count}");
                    var monster1 = monsters[4];
                    Console.WriteLine("monster 4 is null ? " + (monster1 == null).ToString());
                    var monsterName = monster1.id;

                    LogManager.Log("Monster1 : " + monsterName);

                    if (LoadAdditionalData() == false)
                    {
                        LogManager.LogError("Error on load additional data", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadMessages() == false)
                    {
                        LogManager.LogError("Error on load messages", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    if (LoadPlugins(plugins, progressionStep) == false)
                    {
                        LogManager.LogError("Error on load plugin", 1);
                        finishCallback?.Invoke(false);
                        OnStartEnd(false);
                        return;
                    }

                    PluginsIsLoaded = true;

                    Plugins.PluginsManager.OnCoreLoaded();
                    OnStartEnd(true);
                    finishCallback?.Invoke(true);
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error on start core (see the logs for few informations)", 1);
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
                Configuration = Json.JSonSerializer<CoreConfiguration>.DeSerialize(File.ReadAllText(m_configPath));
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
                File.WriteAllText(m_configPath, Json.JSonSerializer<CoreConfiguration>.Serialize(Configuration));
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

        public virtual Task<bool> LoadLang(Action<int, int> progress = null) => Task.Factory.StartNew(() => { return true; });

        public void StopListenProcess(DofusWindow dofusWindow)
        {
            ThreadProcess? thread;
            bool b = m_threads.TryGetValue(dofusWindow.Process, out thread);
            if (thread != null)
            {
                thread.Stop();
            }
        }

        public void StartupListenProcesses(IReadOnlyCollection<NetStat.NetstatEntry> entries, Action<bool, int> callbackFinish = null)
        {
            LogManager.Log($"Start listen {entries.Count} processes");

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

            for (int i = 0; i < processes.Count; i++)
            {
                var process = processes[i];
                var netStatEntry = entries.ElementAt(i);
                if (m_threads != null && m_threads.FirstOrDefault(x => x.Key.Id == process.Id).Value != null) continue;

                LogManager.Log("Sniffer", $"Processe {i} : Get server ip and client port ...");

                try
                {
                    LogManager.Log("Sniffer", $"Processe {i} : Ip and port founded ! ({netStatEntry.DistantAdress}:{netStatEntry.DistantPort})");
                    LogManager.Log("Sniffer", $"Processe {i} : Start listening packet !");

                    DofusWindow dofusWindow = new DofusWindow(process, HybrideManager.DofusVersion == EDofusVersion.Retro);

                    Console.WriteLine("1");

                    ThreadProcess thread = GetThreadProcess();

                    Console.WriteLine("2");

                    thread.Init(dofusWindow);

                    Console.WriteLine("3");

                    thread.OnStopAction += OnThreadExit;

                    Console.WriteLine("4");

                    m_threads.Add(process, thread);

                    Console.WriteLine("5");

                    DofusWindow.WindowsList.Add(dofusWindow);

                    int interfaceIndex = GetInterfaceIndex(netStatEntry);
                    if (interfaceIndex == -1)
                    {
                        LogManager.LogWarning($"Not interface index found for ip {netStatEntry.LocalAddress}");
                        continue;
                    }


                    if (thread.Start($"{netStatEntry.LocalAddress}:{netStatEntry.LocalPort}", interfaceIndex) == false)
                    {
                        Console.WriteLine("Error on startup listen process");
                        callbackFinish?.Invoke(false, 0);
                        return;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error on startup listen processes " + ex.Message);
                    callbackFinish?.Invoke(false, 0);
                    return;
                }
            }

            LogManager.Log("Sniffer", $"{processes.Count} new processes are now listened to (total : {m_threads.Count})");

            callbackFinish?.Invoke(true, processes.Count);
            //});
        }

        protected abstract ThreadProcess GetThreadProcess();

        private int GetInterfaceIndex(NetStat.NetstatEntry entry)
        {

            var interfaces = LibPcapLiveDeviceList.Instance == null ? LibPcapLiveDeviceList.New().ToList() : LibPcapLiveDeviceList.Instance.ToList();

            if (interfaces == null)
            {
                return -1;
            }

            for (int i = 0; i < interfaces.Count; i++)
            {
                var iface = interfaces[i];

                if (iface == null)
                {
                    return -1;
                }

                if (iface.Addresses == null) continue;
                foreach (var adress in iface.Addresses)
                {
                    if (adress.Addr == null) continue;

                    if (adress.Addr.ipAddress == null) continue;

                    if (adress.Addr.ipAddress.ToString() == entry.LocalAddress)
                    {
                        Console.WriteLine("return " + iface.Description);
                        return i;
                    }
                }
            }

            return -1;
        }
        
        private bool LoadPlugins(List<byte[]> bytes, Action<int> progressionStep = null)
        {
            int loadedPlugins = Plugins.PluginsManager.LoadPlugins(bytes, Paths.PLUGINS_ABOSLUTE_PATH, progressionStep, CanLoadPlugin);

            if (loadedPlugins == -1)
            {
                LogManager.LogError("Une erreur est survenue pendant le chargement des mods", 1);
                return false;
            }
            else if (loadedPlugins > 0)
            {
                LogManager.Log($"{loadedPlugins} mods ont été chargés avec succès", 1);
            }
            else
            {
                LogManager.LogWarning($"Aucun plugin n'a été chargé", 1);
            }

            return true;
        }

        protected abstract bool CanLoadPlugin(PluginInfos infos);

        private void Stop()
        {
            for (int i = 0; i < m_threads.Count; i++)
            {
                m_threads.ElementAt(i).Value.Stop(true);
            }

            m_threads = new();
        }

        private void OnThreadExit(ThreadProcess thread)
        {
            m_threads.Remove(thread.DofusWindow.Process);
            DofusWindow.WindowsList.Remove(thread.DofusWindow);
            DofusWindow.OnDofusWindowStoped?.Invoke(thread.DofusWindow);

            if (DofusWindow.Selected == thread.DofusWindow)
            {
                if (DofusWindow.WindowsList.Count > 0)
                {
                    DofusWindow.SelectDofusWindow(DofusWindow.WindowsList[0]);
                }
            }
        }
        
    }
}
