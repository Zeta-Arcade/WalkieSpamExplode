using BepInEx;
using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WalkieSpamExplode
{
    public class ConfigHandler : SyncedConfig2<ConfigHandler>
    {
        public ConfigEntry<bool> debugMode;
        [SyncedEntryField] public SyncedEntry<bool> punishWalkieSpam;
        [SyncedEntryField] public SyncedEntry<bool> punishRemoteSpam;
        [SyncedEntryField] public SyncedEntry<float> walkieExplosionChance;
        [SyncedEntryField] public SyncedEntry<float> remoteExplosionChance;
        [SyncedEntryField] public SyncedEntry<bool> destroyItem;
        [SyncedEntryField] public SyncedEntry<bool> explosionInOrbit;
        [SyncedEntryField] public SyncedEntry<int> maxAnger;
        [SyncedEntryField] public SyncedEntry<int> walkieAngerIncreaseAmount;
        [SyncedEntryField] public SyncedEntry<int> remoteAngerIncreaseAmount;
        [SyncedEntryField] public SyncedEntry<float> angerDecreaseInterval;
        [SyncedEntryField] public SyncedEntry<float> angerDecreaseAmount;
        [SyncedEntryField] public SyncedEntry<int> angerWarningThreshold;
        [SyncedEntryField] public SyncedEntry<float> explosionRadius;
        [SyncedEntryField] public SyncedEntry<float> damageRadius;
        [SyncedEntryField] public SyncedEntry<int> nonLethalDamage;
        public ConfigHandler(ConfigFile config) : base(PluginInfo.modGUID)
        {
            debugMode = config.Bind<bool>("Debugging", "Print Debug Info", false, "If true, prints debug info into the log.");
            punishWalkieSpam = config.BindSyncedEntry<bool>("General", "Punish Walkie Spam", true, "If true, the player will be punished for spamming the Walkie.");
            punishRemoteSpam = config.BindSyncedEntry<bool>("General", "Punish Remote Spam", true, "If true, the player will be punished for spamming the Remote.");
            destroyItem = config.BindSyncedEntry<bool>("General", "Destroy Item", true, "If true, when the max anger is reached and the player is punished, the item (Walkie/Remote) should also be destroyed. Disable if you don't want the item destroyed. Note that since the Remote doesn't have a battery, if the explosion fails it will always be destroyed, regardless of this config.");
            maxAnger = config.BindSyncedEntry<int>("Anger", "Max Anger", 100, new ConfigDescription("The maximum amount of anger that is stored. Anger is added everytime they press the use key on the walkie, or toggle the lights with the Remote. When this value is reached, the player spamming the item will be punished.", new AcceptableValueRange<float>(1f, 1000f)));
            walkieAngerIncreaseAmount = config.BindSyncedEntry<int>("Anger", "Walkie Anger Increase Amount", 25, "How much anger is added to the 'anger meter' everytime they turn a Walkie on.");
            remoteAngerIncreaseAmount = config.BindSyncedEntry<int>("Anger", "Remote Anger Increase Amount", 20, "How much anger is added to the 'anger meter' everytime they toggle the ship lights with the Remote.");
            angerDecreaseInterval = config.BindSyncedEntry<float>("Anger", "Anger Decrease Interval", 1f, "How often the anger meter decreases by the anger decrease rate, in seconds.");
            angerDecreaseAmount = config.BindSyncedEntry<float>("Anger", "Anger Decrease Amount", 5f, "How much anger is decreased every anger decrease interval, stopping at 0. Recommended to be above 0, or else Anger effectively never decreases.");
            angerWarningThreshold = config.BindSyncedEntry<int>("Anger", "Anger Warning Threshold", 70, "When the anger reaches this amount, the player will be given a warning message in the HUD. Set to 0 or above the Max Anger to disable.");
            walkieExplosionChance = config.BindSyncedEntry<float>("Explosion", "Walkie Explosion Chance", 100f, new ConfigDescription("The chance that the walkie will explode when max anger is reached, where 100 = 100%. If it fails, the battery will instead just be drained to 0. Set to 0 to disable explosions.", new AcceptableValueRange<float>(0f, 100f)));
            remoteExplosionChance = config.BindSyncedEntry<float>("Explosion", "Remote Explosion Chance", 100f, new ConfigDescription("The chance that the remote will explode when max anger is reached, where 100 = 100%. If it fails, item will just be destroyed without the explosion. Set to 0 to disable explosions.", new AcceptableValueRange<float>(0f, 100f)));
            explosionInOrbit = config.BindSyncedEntry<bool>("Explosion", "Explosion in Orbit", false, "If true, explosions can occur when in orbit, when the ship is still landing etc.");
            explosionRadius = config.BindSyncedEntry<float>("Explosion", "Explosion Radius", 6.5f, "The radius of the explosion (instakill) ");
            damageRadius = config.BindSyncedEntry<float>("Explosion", "Damage Radius", 7.5f, "The outer radius of the explosion, where it damages the player but doesn't kill them");
            nonLethalDamage = config.BindSyncedEntry<int>("Explosion", "Non-Lethal Damage", 50, new ConfigDescription("The amount of non-lethal damage caused by the outer radius of the explosion", new AcceptableValueRange<int>(1, 100)));
            ConfigManager.Register(this);
        }
    }
}
