using Dtwo.API;
using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API.Hybride.Reflection;
using Dtwo.Core.Plugins;
using Dtwo.Core.Sniffer;
using MathNet.Numerics;
using SharpPcap;

namespace Dtwo.Core
{
    // Todo : Dispose
    public abstract class ThreadProcess
    {
        public DofusWindow DofusWindow { get; private set; }

        public Action<ThreadProcess> OnStopAction;

        public bool Started { get; private set; }

        private SocketListener m_socketListener;
        private MessageParser m_packetParser;

        public void Init(DofusWindow dofusWindow)
        {
            DofusWindow = dofusWindow;
        }

        internal bool Start(string ip, int interfaceIndex)
        {
            if (StartThread(ip, interfaceIndex))
            {
                DofusWindow.OnDofusWindowStarted?.Invoke(DofusWindow);
                return true;
            }

            return false;
        }

        internal void Stop(bool dontCallEvent = false)
        {
            if (Started)
            {
                try
                {
                    Started = false;

                    m_socketListener.StopListening();

                    m_socketListener = null;
                    m_packetParser = null;

                    if (dontCallEvent == false)
                    {
                        OnStopAction?.Invoke(this);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.ToString(), 1);
                }
            }

            LogManager.LogWarning("Une fenêtre de jeu vient d'être arrétée", 1);
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            LogManager.LogWarning("OnProcessExit");

            Stop();
        }

        private void OnCaptureStopped(CaptureStoppedEventStatus status)
        {
            LogManager.LogWarning("OnCaptureStopped");

            Stop();
        }

        private bool StartThread(string ip, int interfaceIndex)
        {
            LogManager.Log("Start thread ...");

            m_socketListener = SocketListener.ListenIp(ip, interfaceIndex);

            if (m_socketListener == null)
            {
                return false;
            }

            m_packetParser = GetMessageParser();

            DofusWindow.Process.EnableRaisingEvents = true;
            DofusWindow.Process.Exited += OnProcessExit;

            m_packetParser.OnGetMessage += InternalOnGetMessage;
            m_socketListener.GetPacketAction += m_packetParser.OnGetPacket;
            m_socketListener.OnCaptureStopped += OnCaptureStopped;

            Started = true;

            LogManager.Log("Thread started");

            return true;
        }

        protected abstract MessageParser GetMessageParser();

        protected abstract HybrideMessage GetHybrideMessage(string identifier, DofusMessage message);
        protected abstract void InitHybrideMessage(HybrideMessage hybrideMessage, DofusMessage message);

        private void InternalOnGetMessage(string identifier, DofusMessage message)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (message != null)
                    {

                        // hybride
                        HybrideMessage hybrideMessage = GetHybrideMessage(identifier, message);
                        if (hybrideMessage != null)
                        {
                            InitHybrideMessage(hybrideMessage, message);
                            if (hybrideMessage.Build() == false)
                            {
                                return;
                            }

                            OnGetHybrideMessage(hybrideMessage);
                        }

                        OnGetMessage(message);
                        //Console.WriteLine(message.GetType());

                        EventPlaylistManager.CallEvent(DofusWindow, message);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError(ex.ToString());
                }
            });
        }

        protected virtual void OnGetMessage(DofusMessage message) { }

        protected virtual void OnGetHybrideMessage(HybrideMessage message)
        {

            if (message.GetType() == typeof(CharacterSelectedSuccessMessage))
            {
                CharacterSelectedSuccessMessage selectedMessage = (CharacterSelectedSuccessMessage)message;
                Character character = new Character(
                    selectedMessage.CharacterInformations.Id, selectedMessage.CharacterInformations.Level,
                    selectedMessage.CharacterInformations.Name, selectedMessage.CharacterInformations.Breed,
                    selectedMessage.CharacterInformations.Sex);

                DofusWindow.OnCharacterSelection(character);
            }

            EventPlaylistManager.CallEvent(DofusWindow, message);
        }
    }
}
