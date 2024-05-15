using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.API;
using SharpPcap;
using System.Diagnostics;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.Core.Plugins;

namespace Dtwo.Core.Sniffer
{
    public abstract class DofusSnifferBase : SnifferBase
    {
        internal Action<DofusSnifferBase>? OnStop;
        internal DofusWindow DofusWindow { get; private set; }

        private MessageParser? m_packetParser;

        internal DofusSnifferBase(DofusWindow dofusWindow, string processName, List<string> noServerIps, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(processName, noServerIps, netStatEntries)
        {
			DofusWindow = dofusWindow;
		}

        internal DofusSnifferBase(DofusWindow dofusWindow, Process process, List<string> noServerIps, IReadOnlyCollection<NetStat.NetstatEntry>? netStatEntries = null) : base(process, noServerIps, netStatEntries)
        {
            DofusWindow = dofusWindow;
		}

        protected override void OnPacketReceived(byte[] bytes, int length)
        {
            base.OnPacketReceived(bytes, length);

            m_packetParser?.OnGetPacket(bytes, length);
        }

        protected override void OnStopped(CaptureStoppedEventStatus status)
        {
            base.OnStopped(status);
        }

        public override EStartSniffResult Start(bool refreshNetstatEntries = false)
        {
            var result = base.Start(refreshNetstatEntries);

            if (result != EStartSniffResult.Success)
            {
                return result;
            }

            m_packetParser = GetMessageParser();

            DofusWindow.Process.EnableRaisingEvents = true;
            DofusWindow.Process.Exited += OnProcessExit;
            m_packetParser.OnGetMessage += InternalOnGetMessage;

            DofusWindow.OnDofusWindowStarted?.Invoke(DofusWindow);

            return EStartSniffResult.Success;
        }

        public override void Stop()
        {
            if (Started == false)
                return;

            base.Stop();

           OnStop?.Invoke(this);
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            LogManager.LogWarning("OnProcessExit");

            Stop();
        }

        protected abstract MessageParser GetMessageParser();

        protected abstract HybrideMessage? GetHybrideMessage(string identifier, DofusMessage message);
        protected abstract void InitHybrideMessage(HybrideMessage hybrideMessage, DofusMessage message);

        private void InternalOnGetMessage(string identifier, DofusMessage message)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (message == null)
                        return;

                    // hybride
                    HybrideMessage? hybrideMessage = GetHybrideMessage(identifier, message);
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
                    //Debug.WriteLine("OnGetMessage " + message.GetType().Name);

                    EventPlaylistManager.CallEvent(DofusWindow, message);
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

                if (selectedMessage.CharacterInformations != null)
                {
                    Character character = new Character(
                        selectedMessage.CharacterInformations.Id, selectedMessage.CharacterInformations.Level,
                        selectedMessage.CharacterInformations.Name, selectedMessage.CharacterInformations.Breed,
                        selectedMessage.CharacterInformations.Sex);

                    DofusWindow.OnCharacterSelection(character);
                }
                else
                {
                    LogManager.LogWarning("CharacterSelectedSuccessMessage.CharacterInformations is null");
                }
            }

            EventPlaylistManager.CallEvent(DofusWindow, message);
        }
    }
}
