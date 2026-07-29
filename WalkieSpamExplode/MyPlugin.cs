using BepInEx;
using CSync;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Windows;

namespace WalkieSpamExplode
{
    [BepInDependency(LethalNetworkAPI.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginInfo.modGUID, PluginInfo.modName, PluginInfo.modVersion)]
    public class WalkieSpamExplodeBase : BaseUnityPlugin
    {
        private Harmony harmony = new Harmony(PluginInfo.modGUID);
        public static BepInEx.Logging.ManualLogSource Logger; //Access this elsewhere via WalkieSpamExplode.Logger.LogDebug($"???");
        public static WalkieSpamExplodeBase Instance;
        private readonly Dictionary<ulong, float> angerLevels = new();
        private Coroutine decreaseAngerCoroutine;
        internal static new ConfigHandler Config;
        private static readonly MethodInfo BroadcastSFXMethod = typeof(WalkieTalkie).GetMethod("BroadcastSFXFromWalkieTalkie",BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerDeathSFXField = typeof(WalkieTalkie).GetField("playerDieOnWalkieTalkieSFX");
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            Logger = base.Logger;
            Config = new ConfigHandler(base.Config);
            WalkieSpamNetwork.Init();
            Logger.LogInfo($"Plugin is loaded!");
            harmony.PatchAll();
        }
        public static void BroadcastWalkieDeathSFX(WalkieTalkie walkie, ulong playerID)
        {
            if (BroadcastSFXMethod == null)
            {
                Logger.LogError("Could not find BroadcastSFXFromWalkieTalkie");
                return;
            }

            if (PlayerDeathSFXField == null)
            {
                Logger.LogError("Could not find playerDieOnWalkieTalkieSFX");
                return;
            }

            AudioClip clip = (AudioClip)PlayerDeathSFXField.GetValue(walkie);

            BroadcastSFXMethod.Invoke(walkie, new object[]
            {clip,(int)playerID});
        }
        public void IncreaseAnger(ulong playerID, int amount, bool isRemote = false)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            if (!angerLevels.ContainsKey(playerID))
            {
                angerLevels[playerID] = 0;
            }
            if (Config.debugMode.Value)
            {
                Logger.LogInfo($"Player {playerID} anger before: {angerLevels[playerID]}");
            }
            angerLevels[playerID] += amount;
            if (Config.debugMode.Value)
            {
                Logger.LogInfo($"Player {playerID} anger after: {angerLevels[playerID]}");
            }
            if (angerLevels[playerID] >= Config.maxAnger.Value)
            {
                PunishPlayer(playerID, isRemote);
            }
            else if (angerLevels[playerID] >= Config.angerWarningThreshold.Value && Config.angerWarningThreshold.Value > 0)
            {
                WarnPlayer(playerID);
            }
            if (decreaseAngerCoroutine == null)
            {
                decreaseAngerCoroutine = StartCoroutine(DecreaseAngerOverTime());
            }
        }
        private IEnumerator DecreaseAngerOverTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(Config.angerDecreaseInterval.Value);

                List<ulong> playersToReset = new();

                foreach (var player in angerLevels.Keys.ToList())
                {
                    angerLevels[player] -= Config.angerDecreaseAmount.Value;
                    if (Config.debugMode.Value)
                    {
                        Logger.LogInfo($"Player {player} anger decreased by {Config.angerDecreaseAmount.Value}. Current anger: {angerLevels[player]}");
                    }
                    if (angerLevels[player] <= 0)
                    {
                        playersToReset.Add(player);
                    }
                }

                foreach (ulong player in playersToReset)
                {
                    angerLevels.Remove(player);
                }
            }
        }
        public void WarnPlayer(ulong playerID)
        {
            WalkieSpamNetwork.SendWarning(playerID);
        }
        public void ShowWarning()
        {
            HUDManager.Instance.DisplayTip("WARNING", $"Spammers will be punished!", true);
        }
        public void PunishPlayer(ulong playerID, bool isRemote) 
        {
            if (StartOfRound.Instance == null) return;
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll <= Config.walkieExplosionChance.Value && !isRemote)
            {
                if ((!Config.explosionInOrbit.Value && (!StartOfRound.Instance.inShipPhase && StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving)) || Config.explosionInOrbit.Value)
                { //If no Explosion in orbit + all criteria is met, OR explosions in orbit are allowed
                    PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.playerClientId == playerID);
                    if (targetPlayer == null) return;
                    WalkieTalkie walkie = null;
                    foreach (var item in targetPlayer.ItemSlots)
                    {
                        if (item == null) continue;
                        if (item is WalkieTalkie wt)
                        {
                            walkie = wt;
                            break;
                        }
                    }
                    if (walkie != null)
                    {
                        BroadcastWalkieDeathSFX(walkie, playerID);
                    }
                    WalkieSpamNetwork.SendExplosion(targetPlayer.transform.position, playerID, isRemote);
                    ResetAnger(playerID);
                }
            }
            else if (roll <= Config.remoteExplosionChance.Value && isRemote)
            {
                if ((!Config.explosionInOrbit.Value && (!StartOfRound.Instance.inShipPhase && StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving)) || Config.explosionInOrbit.Value)
                { //If no Explosion in orbit + all criteria is met, OR explosions in orbit are allowed
                    PlayerControllerB targetPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.playerClientId == playerID);
                    if (targetPlayer == null) return;
                    RemoteProp remote = null;
                    foreach (var item in targetPlayer.ItemSlots)
                    {
                        if (item == null) continue;
                        if (item is RemoteProp rp)
                        {
                            remote = rp;
                            break;
                        }
                    }
                    if (remote != null)
                    {
                        //Unused
                    }
                    WalkieSpamNetwork.SendExplosion(targetPlayer.transform.position, playerID, isRemote);
                    ResetAnger(playerID);
                }
            }
            else if (isRemote) //It is a remote, but the explosion chance failed, so just destroy the remote
            {
                WalkieSpamNetwork.SendDestroyWalkie(playerID, true);
            }
            else //Not a remote, and the roll failed, so drain the battery of the walkie
            {
                WalkieSpamNetwork.SendBatteryDrain(playerID);
            }
        }
        public void ResetAnger(ulong playerID) //Such as on Respawn, + config option
        {
            angerLevels.Remove(playerID);
        }
        public void DrainBattery(ulong playerID)
        {
            if (!StartOfRound.Instance) return;
            if (!StartOfRound.Instance.currentLevel) return;
            PlayerControllerB player = StartOfRound.Instance.localPlayerController;
            if (!player) return;
            if (player.playerClientId != playerID) return;
            foreach (var item in player.ItemSlots)
            {
                if (item == null) continue;
                if (item.insertedBattery == null) continue;
                if (item.insertedBattery.empty) continue;
                if (item.itemProperties.itemName != "Walkie-talkie") continue;
                item.insertedBattery.charge = 0f;
                if (Config.destroyItem.Value)
                {
                    WalkieSpamNetwork.SendDestroyWalkie(playerID, false);
                }
            }
        }
        public void DestroyWalkie(ulong playerID, bool isRemote)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(p => p.playerClientId == playerID);

            if (player == null) return;

            foreach (var item in player.ItemSlots)
            {
                if (item == null) continue;
                string expectedItem = isRemote ? "Remote" : "Walkie-talkie";
                if (item.itemProperties.itemName != expectedItem) continue;
                player.carryWeight = Mathf.Clamp(player.carryWeight - (item.itemProperties.weight - 1f), 1f, 10f);
                player.DestroyItemInSlot(Array.IndexOf(player.ItemSlots, item));
                break;
            }
        }
        public static void ReceiveSelfDestruct(Vector3 position, ulong clientId, bool isRemote)
        {
            if (!StartOfRound.Instance) return;
            if (!StartOfRound.Instance.currentLevel) return;
            PlayerControllerB player = StartOfRound.Instance.localPlayerController;
            if (Config.destroyItem.Value)
            {
                WalkieSpamNetwork.SendDestroyWalkie(clientId, isRemote);
            }
            if (player && player.playerClientId == clientId)
            {
                var launchForce = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1f, UnityEngine.Random.Range(-1f, 1f)).normalized * 30f;
                //player.KillPlayer(launchForce, true, CauseOfDeath.Blast);
            }
            Landmine.SpawnExplosion(position, true, Config.explosionRadius.Value, Config.damageRadius.Value, Config.nonLethalDamage.Value, 0f, (GameObject)null, false);
        }
        [HarmonyPatch(typeof(WalkieTalkie))]
        internal class WalkieTalkiePatch
        {
            [HarmonyPatch("SendWalkieTalkieStartTransmissionSFX")]
            [HarmonyPostfix]
            private static void SwitchWalkieTalkieOnPatch(WalkieTalkie __instance, ref bool ___isBeingUsed)
            {

                if (!___isBeingUsed) return;
                PlayerControllerB player = __instance.playerHeldBy;
                if (player == null) return;
                if (player.playerClientId != NetworkManager.Singleton.LocalClientId) return;
                if (Config.debugMode.Value)
                {
                    WalkieSpamExplodeBase.Logger.LogInfo("Walkie triggered!");
                }
                WalkieSpamNetwork.SendWalkieUsed(player.playerClientId, Config.walkieAngerIncreaseAmount.Value, false);
            }
        }
        [HarmonyPatch(typeof(RemoteProp))]
        internal class RemotePropPatch
        {
            [HarmonyPatch("ItemActivate")]
            [HarmonyPostfix]
            private static void ItemActivatePatch(RemoteProp __instance, bool used, bool buttonDown)
            {
                if (!used || !buttonDown) return;
                PlayerControllerB player = __instance.playerHeldBy;
                if (player == null) return;
                if (player.playerClientId != NetworkManager.Singleton.LocalClientId) return;
                if (Config.debugMode.Value)
                {
                    WalkieSpamExplodeBase.Logger.LogInfo("Remote triggered!");
                }
                WalkieSpamNetwork.SendWalkieUsed(player.playerClientId,Config.remoteAngerIncreaseAmount.Value, true);
            }
        }
        [HarmonyPatch(typeof(StartOfRound))]
        internal class StartOfRoundPatch2
        {
            [HarmonyPatch("ArriveAtLevel")]
            [HarmonyPostfix]
            static void Arrive(StartOfRound __instance)
            {
                //WalkieSpamExplodeBase.Logger.LogInfo("ARRIVE HIT");
            }
        }
    }
}

