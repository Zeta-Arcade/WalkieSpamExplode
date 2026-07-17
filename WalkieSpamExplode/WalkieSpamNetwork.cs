using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace WalkieSpamExplode
{
    public static class WalkieSpamNetwork
    {
        public static LNetworkMessage<WalkieMessage> WalkieUsedMessage;
        public static LNetworkMessage<WarningMessage> WarningMessage;
        public static LNetworkMessage<ExplosionMessage> ExplosionMessage;
        public static LNetworkMessage<DestroyWalkieMessage> DestroyWalkieMessage;
        public static LNetworkMessage<BatteryDrainMessage> BatteryDrainMessage;

        public static void Init()
        {
            WalkieUsedMessage = LNetworkMessage<WalkieMessage>.Create("WalkieUsed",onServerReceived: OnWalkieUsed);
            WarningMessage = LNetworkMessage<WarningMessage>.Create("Warning",onClientReceived: OnWarningReceived);
            ExplosionMessage = LNetworkMessage<ExplosionMessage>.Create("Explosion", onClientReceived: OnExplosionReceived);
            DestroyWalkieMessage = LNetworkMessage<DestroyWalkieMessage>.Create("DestroyWalkie", onServerReceived: OnDestroyWalkieReceived);
            BatteryDrainMessage = LNetworkMessage<BatteryDrainMessage>.Create("BatteryDrain",onClientReceived: OnBatteryDrainReceived);
        }
        private static void OnWalkieUsed(WalkieMessage message, ulong senderClientId)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            if (WalkieSpamExplodeBase.Instance == null) return;
            WalkieSpamExplodeBase.Instance.IncreaseAnger(message.playerID, message.amount);
        }
        private static void OnWarningReceived(WarningMessage message)
        {
            if (StartOfRound.Instance == null) return;
            if (StartOfRound.Instance.localPlayerController == null) return;
            if (StartOfRound.Instance.localPlayerController.playerClientId != message.playerID) return;
            WalkieSpamExplodeBase.Instance.ShowWarning();
        }

        private static void OnExplosionReceived(ExplosionMessage message)
        {
            WalkieSpamExplodeBase.ReceiveSelfDestruct(new Vector3(message.x, message.y, message.z), message.playerID);
        }

        private static void OnDestroyWalkieReceived(DestroyWalkieMessage message, ulong senderClientId)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            WalkieSpamExplodeBase.Instance.DestroyWalkie(message.playerID);
        }

        private static void OnBatteryDrainReceived(BatteryDrainMessage message)
        {
            if (StartOfRound.Instance == null) return;
            if (StartOfRound.Instance.localPlayerController == null) return;
            if (StartOfRound.Instance.localPlayerController.playerClientId != message.playerID) return;
            WalkieSpamExplodeBase.Instance.DrainBattery(message.playerID);
        }

        public static void SendWalkieUsed(ulong playerID, int amount)
        {
            if (!NetworkManager.Singleton.IsListening) return;
            WalkieUsedMessage.SendServer(new WalkieMessage{playerID = playerID, amount = amount});
        }

        public static void SendWarning(ulong playerID)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            WarningMessage.SendClient(new WarningMessage{playerID = playerID},playerID);
        }

        public static void SendExplosion(Vector3 position, ulong playerID)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            ExplosionMessage.SendClients(new ExplosionMessage { x = position.x, y = position.y, z = position.z, playerID = playerID }, NetworkManager.Singleton.ConnectedClientsIds.ToArray());
        }
        public static void SendDestroyWalkie(ulong playerID)
        {
            DestroyWalkieMessage.SendServer(new DestroyWalkieMessage
            {
                playerID = playerID
            });
        }
        public static void SendBatteryDrain(ulong playerID)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            BatteryDrainMessage.SendClient(new BatteryDrainMessage{playerID = playerID}, playerID);
        }
    }
    public class ExplosionMessage
    {
        public float x;
        public float y;
        public float z;
        public ulong playerID;
    }

    public class WalkieMessage
    {
        public ulong playerID;
        public int amount;
    }
    public class WarningMessage
    {
        public ulong playerID;
    }
    public class BatteryDrainMessage
    {
        public ulong playerID;
    }
    public class DestroyWalkieMessage
    {
        public ulong playerID;
    }
}
