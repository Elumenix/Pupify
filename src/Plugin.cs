using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using On.HUD;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Pupify;

[BepInPlugin("elumenix.pupify", "Pupify", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    private bool IsInit;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            // Create a patch to Player.isSlugpup that marks players as slugpups 
            Harmony harmony = new Harmony("PupifyHarmony");
            var slugpupMethod = typeof(Player).GetProperty("isSlugpup")?.GetGetMethod();
            var slugpupCheck = typeof(Plugin).GetMethod("Player_isSlugpup");
            harmony.Patch(slugpupMethod, prefix: new HarmonyMethod(slugpupCheck));
            
            On.HUD.FoodMeter.ctor += FoodMeter_ctor;
            On.Player.ctor += Player_ctor;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.GameSession.ctor += GameSession_ctor;
            
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractcreature, World world)
    {
        orig(self, abstractcreature, world);
        
        // Crash will be caused if higher value than slugpup
        while (self.playerState.foodInStomach > 1)
        {
            self.SubtractFood(1);
        }
    }

    private void FoodMeter_ctor(FoodMeter.orig_ctor orig, HUD.FoodMeter self, HUD.HUD hud, int maxfood, int survivallimit, Player associatedpup, int pupnumber)
    {
        orig(self, hud, maxfood, survivallimit, associatedpup, pupnumber);

        
        Debug.Log("IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIJIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII");
        Debug.Log(self.hud.owner.CurrentFood);
        Debug.Log(self.circles.Count);
        Debug.Log(self.showCount);
    }

    public static bool Player_isSlugpup(Player __instance, ref bool __result)
    {
        if (__instance.SlugCatClass != null && __instance.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            // This actually is an npc
            __result = true;

            // Don't use the base method
            return false;
        }

        // check initial
        return true;
    }
    
    private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
    {
        orig(self);
        ClearMemory();
    }
    private void GameSession_ctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig(self, game);
        ClearMemory();
    }

    #region Helper Methods

    private void ClearMemory()
    {
        //If you have any collections (lists, dictionaries, etc.)
        //Clear them here to prevent a memory leak
        //YourList.Clear();
    }

    #endregion
}
