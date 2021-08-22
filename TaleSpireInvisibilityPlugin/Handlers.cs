using BepInEx;

using UnityEngine;

namespace LordAshes
{
    public partial class InvisibilityPlugin : BaseUnityPlugin
    {

        public enum VisibleState
        { 
            visibleAll = 0,
            visibleOwnerOrGM = 1,
            visibleNot = 2
        }

        /// <summary>
        /// Handler for Stat Messaging subscribed messages.
        /// </summary>
        /// <param name="changes"></param>
        public void HandleRequest(StatMessaging.Change[] changes)
        {
            foreach (StatMessaging.Change change in changes)
            {
                Debug.Log("Invisibility Plugin: Change For " + change.cid + ", Type: " + change.action + ", From: " + change.previous + ", To: " + change.value);
                ApplyVisibility(change);            
            }
        }

        private void ApplyVisibility(StatMessaging.Change change)
        {
            CreatureBoardAsset asset;
            CreaturePresenter.TryGetAsset(change.cid, out asset);
            if (asset != null)
            {
                UnityEngine.Debug.Log("Invisibility Plugin: Creature : " + asset.Creature.Name + " : " + asset.Creature.CreatureId +" : " + change.action + " : Controlled? " + LocalClient.HasControlOfCreature(change.cid));

                VisibleState visible = VisibleState.visibleNot;
                if ((change.action == StatMessaging.ChangeType.removed))
                {
                    //
                    // Invisibility turned off - Visible to all
                    //
                    visible = VisibleState.visibleAll;
                    if (invisible.ContainsKey(change.cid))
                    {
                        // Restore base loader texture
                        Debug.Log("Invisibility Plugin: Restoring Base Loader Texture");
                        asset.BaseLoader.LoadedAsset.GetComponent<MeshRenderer>().material.mainTexture = invisible[change.cid];
                        // Remove mini from the invisible list
                        Debug.Log("Invisibility Plugin: Removing Mini From Invisibility List");
                        invisible.Remove(change.cid);
                    }
                }
                else
                {
                    //
                    // Invisibility turned on - Visible to owner and GM 
                    //
                    if (LocalClient.HasControlOfCreature(change.cid))
                    {
                        visible = VisibleState.visibleOwnerOrGM;
                    }
                    if (!invisible.ContainsKey(change.cid))
                    {
                        // Save base loader texture
                        Debug.Log("Invisibility Plugin: Storing Base Loader Texture");
                        try
                        {
                            invisible.Add(change.cid, asset.BaseLoader.LoadedAsset.GetComponent<MeshRenderer>().material.mainTexture);
                        }
                        catch(System.Exception)
                        {
                            Debug.Log("Invisibility Plugin: Mini Not Yet Ready");
                            StatMessaging.Reset(InvisibilityPlugin.Guid);
                            return;
                        }
                    }
                    // Apply invisibility texture to the base
                    Debug.Log("Invisibility Plugin: Changing Base Loader Texture");
                    asset.BaseLoader.LoadedAsset.GetComponent<MeshRenderer>().material.mainTexture = FileAccessPlugin.Image.LoadTexture(InvisibilityPlugin.Guid + "\\Invisibility.png");
                }

                Debug.Log("Invisibility Plugin: Getting Renderer");
                Renderer rend = asset.CreatureLoaders[0].GetComponentInChildren<MeshRenderer>();
                if (rend == null) { rend = asset.CreatureLoaders[0].GetComponentInChildren<SkinnedMeshRenderer>(); }
                if (rend != null)
                {
                    UnityEngine.Debug.Log("Invisibility Plugin: Turned " + rend.GetType().ToString() + " To " + visible);
                    rend.enabled = (visible == VisibleState.visibleNot) ? false : true;
                    rend = asset.BaseLoader.GetComponentInChildren<MeshRenderer>();
                    rend.enabled = (visible == VisibleState.visibleNot) ? false : true;
                }
                else
                {
                    Debug.Log("Invisibility Plugin: Could Not Find Renderer To Turn On For " + asset.Creature.Name);
                }

                Debug.Log("Invisibility Plugin: Getting Indicator Renderer");
                MeshRenderer[] comps = asset.BaseLoader.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer obj in comps)
                {
                    if(obj.name=="Indicator")
                    {
                        UnityEngine.Debug.Log("Invisibility Plugin: Turned Indicator To " + visible);
                        obj.enabled = (visible == VisibleState.visibleNot) ? false : true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handler for Radial Menu selections
        /// </summary>
        /// <param name="cid"></param>
        private void SetRequest(CreatureGuid cid)
        {
            if (!invisible.ContainsKey(cid))
            {
                StatMessaging.SetInfo(cid, InvisibilityPlugin.Guid, "Invisible");
            }
            else
            {
                StatMessaging.ClearInfo(cid, InvisibilityPlugin.Guid);
            }
        }
    }
}
