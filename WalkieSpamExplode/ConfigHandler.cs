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
        [SyncedEntryField] public SyncedEntry<float> explosionChance;
        [SyncedEntryField] public SyncedEntry<bool> destroyWalkie;
        [SyncedEntryField] public SyncedEntry<int> maxAnger;
        [SyncedEntryField] public SyncedEntry<int> angerIncreaseAmount;
        [SyncedEntryField] public SyncedEntry<float> angerDecreaseInterval;
        [SyncedEntryField] public SyncedEntry<float> angerDecreaseAmount;
        [SyncedEntryField] public SyncedEntry<int> angerWarningThreshold;
        [SyncedEntryField] public SyncedEntry<float> explosionRadius;
        [SyncedEntryField] public SyncedEntry<float> damageRadius;
        [SyncedEntryField] public SyncedEntry<int> nonLethalDamage;
        public ConfigHandler(ConfigFile config) : base(PluginInfo.modGUID)
        {
            debugMode = config.Bind<bool>("Debugging", "Print Debug Info", false, "If true, prints debug info into the log.");
            explosionChance = config.BindSyncedEntry<float>("Anger", "Explosion Chance", 100f, new ConfigDescription("The chance that the walkie will explode when max anger is reached, where 100 = 100%. If it fails, the battery will instead just be drained to 0. Set to 0 to disable explosions.", new AcceptableValueRange<float>(0f, 100f)));
            destroyWalkie = config.BindSyncedEntry<bool>("Anger", "Destroy Walkie", true, "If true, when the max anger is reached and the player is either exploded or has their battery drained, the walkie should also be destroyed.");
            maxAnger = config.BindSyncedEntry<int>("Anger", "Max Anger", 100, "The maximum amount of anger that is stored. Anger is added everytime they turn on the walkie. When this value is reached, the player spamming the Walkie will be punished.");
            angerIncreaseAmount = config.BindSyncedEntry<int>("Anger", "Anger Increase Amount", 25, "How much anger is added to the 'anger meter' everytime they turn a Walkie on.");
            angerDecreaseInterval = config.BindSyncedEntry<float>("Anger", "Anger Decrease Interval", 1f, "How often the anger meter decreases by the anger decrease rate, in seconds.");
            angerDecreaseAmount = config.BindSyncedEntry<float>("Anger", "Anger Decrease Amount", 5f, "How much anger is decreased every anger decrease interval. Stops at 0.");
            angerWarningThreshold = config.BindSyncedEntry<int>("Anger", "Anger Warning Threshold", 70, "When the anger reaches this amount, the player will be given a warning message in the HUD. Set to 0 or above the Max Anger to disable.");
            explosionRadius = config.BindSyncedEntry<float>("Explosion", "Explosion Radius", 6.5f, "The radius of the explosion (instakill) if the Walkie explodes");
            damageRadius = config.BindSyncedEntry<float>("Explosion", "Damage Radius", 7.5f, "The outer radius of the explosion, where it damages the player but doesn't kill them");
            nonLethalDamage = config.BindSyncedEntry<int>("Explosion", "Non-Lethal Damage", 50, new ConfigDescription("The amount of non-lethal damage caused by the outer radius of the explosion", new AcceptableValueRange<int>(1, 100)));
            ConfigManager.Register(this);
        }
    }
}
