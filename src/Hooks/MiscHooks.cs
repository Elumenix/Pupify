using System.Collections.Generic;
using UnityEngine;

namespace Pupify.Hooks;

public static class MiscHooks
{
    public static List<FAtlas> saved;
    
    public static void Init()
    {
        On.AssetManager.SafeWWWLoadTexture += AssetManager_SafeWWWLoadTexture;
        On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
    }

    private static Texture2D AssetManager_SafeWWWLoadTexture(On.AssetManager.orig_SafeWWWLoadTexture orig,
        ref Texture2D texture2d, string path, bool clampWrapMode, bool crispPixels)
    {
        // Urls are purposefully wrong because of how it's programmed
        if (path.Contains("face_atlases/pup_on"))
        {
            return (Texture2D) saved[1].texture;
        }
        
        if (path.Contains("face_atlases/pup_off"))
        {
            return (Texture2D) saved[0].texture;
        }
        
        
        return orig(ref texture2d, path, clampWrapMode, crispPixels);
    }


    private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig,
        ProcessManager self, ProcessManager.ProcessID ID)
    {
        // Multiplayer menu check is so that players can switch between characters in the arena 
        // otherwise the same recolored character will be used if the player goes back to arena with a different character

        if (!Plugin.options.onlyCosmetic.Value &&
            ((ID == ProcessManager.ProcessID.Game && self.oldProcess.ID != ProcessManager.ProcessID.MultiplayerMenu) ||
             ID == ProcessManager.ProcessID.MultiplayerMenu))
        {
            // Allow game to set up starving values : Should not be nullified if switching into arena mode, or 
            // else the stats reference will be nullified and the game will crash
            Plugin.currentSlugcat = null;
            Plugin.playersCreated = false;
            MultiPlayer.Session = null;
            MultiPlayer.onlyPupsLeft = false;
            MultiPlayer.startingIncrement = 0;
            MultiPlayer.currentIndex = 0;
            MultiPlayer.numAccessed = 0;
            MultiPlayer.playerToLoad = 0;
            MultiPlayer.ClearPlayers();
        }

        orig(self, ID);
    }
}
