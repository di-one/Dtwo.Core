using Dtwo.API;
using Dtwo.API.Dofus2.AnkamaGames.Network.Messages;
using Dtwo.API.Dofus2.Network.Messages;
using Dtwo.API.DofusBase.Network.Messages;
using Dtwo.API.Hybride.Network.Messages;
using Dtwo.API.Hybride.Reflection;

namespace Dtwo.Core.Dofus2
{
    public class Dofus2ThreadProcess : ThreadProcess
    {
        protected override HybrideMessage GetHybrideMessage(string identifier, DofusMessage message)
        {
            HybrideMessage hybrideMessage = HybrideMessagesLoader.Instance.GetDofus2Message(identifier);

            return hybrideMessage;
        }

        protected override void OnGetMessage(DofusMessage message)
        {
            if (message.GetType() == typeof(ReloginTokenStatusMessage))
            {
                LogMessage.LogWarning("Un personnage a été déconnecté", 1);
                Stop();
                return;
            }

            base.OnGetMessage(message);
        }

        protected override void InitHybrideMessage(HybrideMessage hybrideMessage, DofusMessage message)
        {
            hybrideMessage.Init(dofus2Message: message as Dofus2Message);
        }

        protected override MessageParser GetMessageParser()
        {
            return new Dofus2MessageParser();
        }
    }
}
