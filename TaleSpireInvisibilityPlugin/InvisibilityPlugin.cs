using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using UnityEngine;

/// <summary>
/// 
/// Notes:
/// 
/// 1. Try to keep this main page simple with just the Unity/TS methods like Awake() and Update() methods
/// 2. Place your processing code in class specific files or a general class file like the sample Handler.cs file
/// 3. You can make your additional class file part of the main plugin class by making them "public partial class TemplatePlugin : BaseUnityPlugin"
///    (See the sample Handler.cs and/or Unity.cs files for an example)
/// 
/// </summary>

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
        public const string Version = "1.0.0.0";                        

        // Configuration
        private ConfigEntry<KeyboardShortcut> triggerInvisibility { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerRefresh { get; set; }

        // Invisible Assets
        private Dictionary<CreatureGuid,Texture> invisible = new Dictionary<CreatureGuid,Texture>();

        private NGuid _radialTarget = NGuid.Empty;

        private bool subscribed = false;

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
                        CreaturePresenter.TryGetAsset(new CreatureGuid(_radialTarget), out asset);
                        Debug.Log("GM Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
                        if (asset != null) { CreatureManager.SetCreatureExplicitHideState(asset.Creature.CreatureId, !asset.Creature.IsExplicitlyHidden); }
                    },
                    CloseMenuOnActivate = true
                }, (m, r) => { _radialTarget = r; return true; });
            }

            // Add replacement Hide menu in the root character radial menu
            RadialUI.RadialUIPlugin.AddOnCharacter(InvisibilityPlugin.Guid + ".addPCHide", new MapMenu.ItemArgs
            {
                Title = "PC Hide",
                Icon = FileAccessPlugin.Image.LoadSprite("Hide.png"),
                Action = (mmi1, obj1) =>
                {
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(new CreatureGuid(_radialTarget), out asset);
                    Debug.Log("Player Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
                    if (asset != null) { SetRequest(asset.Creature.CreatureId); }
                },
                CloseMenuOnActivate = true
            },(m,r)=> { _radialTarget = r; return true; });

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
                if (!subscribed)
                {
                    Debug.Log("Invisibility Plugin: Subscribing To Invisibility Plugin Stat Messages...");
                    subscribed = true;
                    StatMessaging.Subscribe(InvisibilityPlugin.Guid, HandleRequest);
                }

                if (Utility.StrictKeyCheck(triggerInvisibility.Value))
                {
                    Debug.Log("Toggling Invisibility State...");
                    if (LocalClient.SelectedCreatureId != null)
                    {
                        SetRequest(LocalClient.SelectedCreatureId);
                    }
                }

                if (Utility.StrictKeyCheck(triggerRefresh.Value))
                {
                    Debug.Log("Refreshing Invisibility States...");
                    foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                    {
                        ApplyVisibility(new StatMessaging.Change()
                        {
                            cid = asset.Creature.CreatureId,
                            action = (invisible.ContainsKey(asset.Creature.CreatureId)) ? StatMessaging.ChangeType.modified : StatMessaging.ChangeType.removed,
                            key = InvisibilityPlugin.Guid,
                            value = "Refresh:" + DateTime.UtcNow
                        }); ;
                    }
                }
            }
        }

        private void PlayerHide(CreatureGuid cid, string txt, MapMenuItem mmi)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
            Debug.Log("Player Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
            if (asset != null) { SetRequest(asset.Creature.CreatureId); }
        }

        private void GMHide(CreatureGuid cid, string txt, MapMenuItem mmi)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out asset);
            Debug.Log("GM Hiding " + asset.Creature.Name + " (" + asset.Creature.CreatureId + ")");
            if (asset != null) { CreatureManager.SetCreatureExplicitHideState(asset.Creature.CreatureId, !asset.Creature.IsExplicitlyHidden); }
        }
    }
}
