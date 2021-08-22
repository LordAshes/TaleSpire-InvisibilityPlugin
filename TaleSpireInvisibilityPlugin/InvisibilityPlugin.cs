using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using UnityEngine;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class InvisibilityPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Invisibility Plug-In";             
        public const string Guid = "org.lordashes.plugins.invisibility";
        public const string Version = "1.0.1.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerInvisibility { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerRefresh { get; set; }

        // Invisible Assets
        private Dictionary<CreatureGuid,Texture> invisible = new Dictionary<CreatureGuid,Texture>();

        private System.Guid subscription = System.Guid.Empty;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Invisibility Plugin: I'm Ready To Make Minis Disappear.");

            triggerInvisibility = Config.Bind("Hotkeys", "Toggle Invisibility", new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl));
            triggerRefresh = Config.Bind("Hotkeys", "Refresh", new KeyboardShortcut(KeyCode.I, KeyCode.RightControl));

            if (Config.Bind("Settings", "Move Original Hide To GM Menu", false).Value)
            {
                // Remove core TS Hide menu in the root character radial menu
                RadialUI.RadialUIPlugin.AddOnRemoveCharacter(InvisibilityPlugin.Guid + ".hideGMHide", "Hide");

                // Add Hide menu that does original hide functionality to the GM menu
                RadialUI.RadialUIPlugin.AddOnSubmenuGm(InvisibilityPlugin.Guid + ".addGMHide", new MapMenu.ItemArgs
                {
                    Title = "GM Hide",
                    Icon = FileAccessPlugin.Image.LoadSprite("Hide.png"),
                    Action = (mmi2, obj2) =>
                    {
                        CreatureBoardAsset asset;
                        CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
                        Debug.Log("GM Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
                        if (asset != null) { CreatureManager.SetCreatureExplicitHideState(asset.Creature.CreatureId, !asset.Creature.IsExplicitlyHidden); }
                    },
                    CloseMenuOnActivate = true
                }, (m, r) => { return true; });
            }

            // Add replacement Hide menu in the root character radial menu
            RadialUI.RadialUIPlugin.AddOnCharacter(InvisibilityPlugin.Guid + ".addPCHide", new MapMenu.ItemArgs
            {
                Title = "PC Hide",
                Icon = FileAccessPlugin.Image.LoadSprite("Hide.png"),
                Action = (mmi1, obj1) =>
                {
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
                    Debug.Log("Player Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
                    if (asset != null) { SetRequest(asset.Creature.CreatureId); }
                },
                CloseMenuOnActivate = true
            },(m,r)=> { return LocalClient.CanControlCreature(new CreatureGuid(r)); });

            Utility.PostOnMainPage(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            if (Utility.isBoardLoaded())
            {
                if (subscription==System.Guid.Empty)
                {
                    Debug.Log("Invisibility Plugin: Subscribing To Invisibility Plugin Stat Messages...");
                    StatMessaging.Reset(InvisibilityPlugin.Guid);
                    subscription = StatMessaging.Subscribe(InvisibilityPlugin.Guid, HandleRequest);
                }

                if (Utility.StrictKeyCheck(triggerInvisibility.Value))
                {
                    Debug.Log("Invisibility Plugin: Toggling Invisibility State...");
                    if (LocalClient.SelectedCreatureId != null)
                    {
                        SetRequest(LocalClient.SelectedCreatureId);
                    }
                }

                if (Utility.StrictKeyCheck(triggerRefresh.Value))
                {
                    Debug.Log("Invisibility Plugin: Refreshing Invisibility States...");
                    foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                    {
                        ApplyVisibility(new StatMessaging.Change()
                        {
                            cid = asset.Creature.CreatureId,
                            action = (StatMessaging.ReadInfo(asset.Creature.CreatureId, InvisibilityPlugin.Guid)!="") ? StatMessaging.ChangeType.modified : StatMessaging.ChangeType.removed,
                            key = InvisibilityPlugin.Guid,
                            value = "Refresh:" + DateTime.UtcNow
                        }); ;
                    }
                }
            }
            else
            {
                if (subscription!=System.Guid.Empty)
                {
                    StatMessaging.Unsubscribe(subscription);
                    subscription = System.Guid.Empty;
                }
            }
        }

        private void PlayerHide(CreatureGuid cid, string txt, MapMenuItem mmi)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
            Debug.Log("Invisibility Plugin: Player Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
            if (asset != null) { SetRequest(asset.Creature.CreatureId); }
        }

        private void GMHide(CreatureGuid cid, string txt, MapMenuItem mmi)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
            Debug.Log("Invisibility Plugin: GM Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
            if (asset != null) { CreatureManager.SetCreatureExplicitHideState(asset.Creature.CreatureId, !asset.Creature.IsExplicitlyHidden); }
        }
    }
}
