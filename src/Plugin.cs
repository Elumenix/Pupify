using System;
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
	public static Options options;

    public static SlugcatStats currentSlugcat;
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

            // Setup
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.GameSession.ctor += GameSession_ctor;
            
            // Hook Groups
            PlayerHooks.Init();
            SceneHooks.Init();
            MiscHooks.Init();
            
            // Test code
            //On.Player.Update += Player_Update;

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
        //YourList.Clear();
    }
}
