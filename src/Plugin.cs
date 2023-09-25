using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Pupify.Hooks;
using UnityEngine;


#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Pupify;

[BepInPlugin("elumenix.pupify", "Pupify", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    // Mod-wide fields
	public static Options options;
    public static SlugcatStats currentSlugcat;
    public static bool playersCreated = false;

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

            MiscHooks.saved = new List<FAtlas>
            {
                Futile.atlasManager.LoadImage("atlases/face_pup_off"),
                Futile.atlasManager.LoadImage("atlases/face_pup_on"),
                Futile.atlasManager.LoadImage("atlases/pup_off"),
                Futile.atlasManager.LoadImage("atlases/pup_on")
            };

            // Setup
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.GameSession.ctor += GameSession_ctor;
            
            // Hook Groups
            PlayerHooks.Init();
            SceneHooks.Init();
            MiscHooks.Init();
            MultiPlayer.Init();
            MenuHooks.Init();
            
            // Test code
            On.Player.Update += Player_Update;

            MachineConnector.SetRegisteredOI("elumenix.pupify", options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        Debug.Log(self.abstractCreature.ID + " RunSpeed: " + self.slugcatStats.runspeedFac);
        Debug.Log(self.abstractCreature.ID + " BodyWeight: " + self.slugcatStats.bodyWeightFac);
        Debug.Log(self.abstractCreature.ID + " GeneralVisibility: " + self.slugcatStats.generalVisibilityBonus);
        Debug.Log(self.abstractCreature.ID + " VisualStealth: " + self.slugcatStats.visualStealthInSneakMode);
        Debug.Log(self.abstractCreature.ID + " Loudness: " + self.slugcatStats.loudnessFac);
        Debug.Log(self.abstractCreature.ID + " LungsFac: " + self.slugcatStats.lungsFac);
        Debug.Log(self.abstractCreature.ID + " ThrowingSkill: " + self.slugcatStats.throwingSkill);
        Debug.Log(self.abstractCreature.ID + " PoleClimbSpeed: " + self.slugcatStats.poleClimbSpeedFac);
        Debug.Log(self.abstractCreature.ID + " CorridorClimbSpeed: " + self.slugcatStats.corridorClimbSpeedFac);
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
    
    
    private void ClearMemory()
    {
        //If you have any collections (lists, dictionaries, etc.)
        //Clear them here to prevent a memory leak
    }


    public static bool MakeChanges(Player self = null, int id = -1)
    {
        // Game not instantiated yet or a npc is being checked
        if (MultiPlayer.makePup == null || self is {isNPC: true})
        {
            return false;
        }
        
        // Part 1: No changes should be made in cosmetic mode
        // Part 2: No changes should be made in Single Player story mode unless the pupButton is toggled
        //         pupButton is checked for null because safari mode will take this route and crash the game 
        // Part 3: No changes should be made in a multiplayer context to this specific player
        // Part 4: Same as Part 3, but works with slugcatStats instead of player id
        // Part 5: No changes for this slugcat in jolly coop story mode, checks jolly's built in options
        // part 6: Same as part 5, but works with slugcatStats instead of player id
        if (options.onlyCosmetic.Value || MultiPlayer.Session is StoryGameSession && !ModManager.CoopAvailable &&
            MenuHooks.pupButton != null && !MenuHooks.pupButton.isToggled || self != null &&
            MultiPlayer.Session is not StoryGameSession &&
            !MultiPlayer.makePup[self.abstractCreature.ID.number] ||
            id != -1 && MultiPlayer.Session is not StoryGameSession && !MultiPlayer.makePup[id] || id != -1 &&
            MultiPlayer.Session is StoryGameSession session && ModManager.CoopAvailable &&
            !session.game.rainWorld.options.jollyPlayerOptionsArray[id].isPup || self != null &&
            MultiPlayer.Session is StoryGameSession multiSession && ModManager.CoopAvailable && !multiSession.game
                .rainWorld.options.jollyPlayerOptionsArray[self.abstractCreature.ID.number].isPup)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
