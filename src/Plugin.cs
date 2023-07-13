using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using On.HUD;
using On.MoreSlugcats;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Pupify;

[BepInPlugin("elumenix.pupify", "Pupify", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Options options;

    public Plugin()
    {
        try
        {
            options = new Options(this, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    
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
            On.HUD.FoodMeter.Update += FoodMeterOnUpdate;
            On.MoreSlugcats.MSCRoomSpecificScript.SpearmasterGateLocation.Update += SpearmasterGateLocation_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.GameSession.ctor += GameSession_ctor;
            On.Player.ctor += Player_ctor;

            MachineConnector.SetRegisteredOI("elumenix.pupify", options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        
        // Aesthetic only
        if (options.onlyCosmetic.Value)
        {
            // The player will be drawn as a pup, but they won't function differently
            self.setPupStatus(true);
        }
    }

    private void SpearmasterGateLocation_Update(MSCRoomSpecificScript.SpearmasterGateLocation.orig_Update orig, MoreSlugcats.MSCRoomSpecificScript.SpearmasterGateLocation self, bool eu)
    {
        orig(self, eu);

        // Makes sure that spearmaster doesn't spawn with more food than a pup should have
        // which would cause an out of range error upon the first time eating, strictly on cycle 0
        foreach (var t in self.room.game.Players)
        {
            if ((t.realizedCreature as Player)?.SlugCatClass ==
                MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                ((Player) t.realizedCreature).playerState.foodInStomach = 1;
            }
        }
    }

    private void FoodMeterOnUpdate(FoodMeter.orig_Update orig, HUD.FoodMeter self)
    {
        orig(self);
        Debug.Log(" " + self.hud.owner.CurrentFood);
    }

    

    private void FoodMeter_ctor(FoodMeter.orig_ctor orig, HUD.FoodMeter self, HUD.HUD hud, int maxfood, int survivallimit, Player associatedpup, int pupnumber)
    {
        orig(self, hud, maxfood, survivallimit, associatedpup, pupnumber);
    }

    public static bool Player_isSlugpup(Player __instance, ref bool __result)
    {
        if (!options.onlyCosmetic.Value)
        {
            // This actually is an npc
            __result = true;

            // Don't use the base method
            return false;
        }

        // check initial value instead
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
