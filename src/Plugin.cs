using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using On.MoreSlugcats;
using UnityEngine;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Pupify;

[BepInPlugin("elumenix.pupify", "Pupify", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Options options;

    private static SlugcatStats currentSlugcat;
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
            
            
            MSCRoomSpecificScript.SpearmasterGateLocation.Update += SpearmasterGateLocation_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.GameSession.ctor += GameSession_ctor;
            On.Player.ctor += Player_ctor;
            On.SlugcatStats.ctor += SlugcatStats_ctor;
            On.Player.Update += Player_Update;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
            On.Player.ShortCutColor += PlayerOnShortCutColor;

            MachineConnector.SetRegisteredOI("elumenix.pupify", options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private Color PlayerOnShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        if (currentSlugcat != null && self.SlugCatClass == currentSlugcat.name && !self.isNPC)
        {
            return PlayerGraphics.SlugcatColor(currentSlugcat.name);
        }

        return orig(self);
    }

    private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        if (ID == ProcessManager.ProcessID.Game)
        {
            // Allow game to set up starving values
            currentSlugcat = null;
        }

        orig(self, ID);
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        
        Debug.Log("RunSpeed: " + self.slugcatStats.runspeedFac);
        Debug.Log("BodyWeight: " + self.slugcatStats.bodyWeightFac);
        Debug.Log("GeneralVisibility: " + self.slugcatStats.generalVisibilityBonus);
        Debug.Log("VisualStealth: " + self.slugcatStats.visualStealthInSneakMode);
        Debug.Log("Loudness: " + self.slugcatStats.loudnessFac);
        Debug.Log("LungsFac: " + self.slugcatStats.lungsFac);
        Debug.Log("ThrowingSkill: " + self.slugcatStats.throwingSkill);
        Debug.Log("PoleClimbSpeed: " + self.slugcatStats.poleClimbSpeedFac);
        Debug.Log("CorridorClimbSpeed: " + self.slugcatStats.corridorClimbSpeedFac);
    }

    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {
        // this will correct body color changes
        orig(self, slugcat, currentSlugcat is {malnourished: true} || malnourished);

        if (!options.onlyCosmetic.Value)
        {
            // This block will run twice, once following the initial creation of the campaign character
            // The second after the creation of the pup. The player actually plays as the pup
            // I store a reference to the first character, who will be instantiated with the correct stats the first time through
            // When the NPCStats method runs, after the program realizes I set the player to be treated as a pup, the 
            // method creates a new slugCatStats for the player. The second time through overwrites those stats directly
            // This is how both stat calculation rules work. Natural slugpup rules will instead not overwrite on the second run

            if (currentSlugcat == null)
            {
                currentSlugcat = self;
                
                float percentRequired = (float)self.foodToHibernate / self.maxFood;
                self.maxFood = Mathf.RoundToInt(self.maxFood * (3f / 7f));
                self.foodToHibernate =
                    Mathf.RoundToInt(self.maxFood * percentRequired * (7f / 6f));
            
                // This may happen with a custom slugcat with ludicrously high food values
                if (self.foodToHibernate > self.maxFood)
                {
                    self.foodToHibernate = self.maxFood;
                }
            }
            else
            {
                // Don't override the slugpup
                self.foodToHibernate = currentSlugcat.foodToHibernate;
                self.maxFood = currentSlugcat.maxFood;
            }
        }

        // Stat adjustment option
        if (true)
        {
            // second condition is added in case food was also adjusted
            if (currentSlugcat == null || currentSlugcat == self)
            {
                currentSlugcat = self;
                
                // Stat adjustments
                self.runspeedFac = currentSlugcat.runspeedFac * .8f * (.8f / .84f); // NPCStats interferes
                self.bodyWeightFac = currentSlugcat.bodyWeightFac * .65f * (.65f / .63375f); // NPCStats interferes
                self.generalVisibilityBonus = currentSlugcat.generalVisibilityBonus - .2f; // Very simple adjustment
                self.visualStealthInSneakMode = currentSlugcat.visualStealthInSneakMode * 1.2f; // Alternative was +.1f, but I thought scaling was better
                self.loudnessFac = currentSlugcat.loudnessFac * .5f; // Probably the simplest to think about
                self.lungsFac = currentSlugcat.lungsFac * .8f; // This is the only improvement, all slugpups have better lung capacities 
                self.poleClimbSpeedFac = currentSlugcat.poleClimbSpeedFac * .8f * (.8f / .836f); // NPCStats interferes
                self.corridorClimbSpeedFac = currentSlugcat.corridorClimbSpeedFac * .8f * (.8f / .84f); // NPCStats interferes

                // This is a weird one because it's such a big difference, but it is only an int and doesn't vary much
                self.throwingSkill = currentSlugcat.throwingSkill - 1;
                if (self.throwingSkill < 0)
                {
                    self.throwingSkill = 0;
                }
            }
            else
            {
                // Apply all values to pup
                self.runspeedFac = currentSlugcat.runspeedFac;
                self.bodyWeightFac = currentSlugcat.bodyWeightFac;
                self.generalVisibilityBonus = currentSlugcat.generalVisibilityBonus;
                self.visualStealthInSneakMode = currentSlugcat.visualStealthInSneakMode;
                self.loudnessFac = currentSlugcat.loudnessFac;
                self.lungsFac = currentSlugcat.lungsFac;
                self.poleClimbSpeedFac = currentSlugcat.poleClimbSpeedFac;
                self.corridorClimbSpeedFac = currentSlugcat.corridorClimbSpeedFac;
                self.throwingSkill = currentSlugcat.throwingSkill;
            }
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
                // Override so that spearmaster doesn't start with more food than they can hold, which would crash the game
                ((Player) t.realizedCreature).playerState.foodInStomach = 0;
            }
        }
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
