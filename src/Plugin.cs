using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using HUD;
using Menu;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using CutsceneArtificer = On.MoreSlugcats.CutsceneArtificer;
using MenuLabel = Menu.MenuLabel;
using MenuObject = Menu.MenuObject;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using MSCRoomSpecificScript = On.MoreSlugcats.MSCRoomSpecificScript;
using SlugcatSelectMenu = On.Menu.SlugcatSelectMenu;

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
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
            On.Player.ShortCutColor += Player_ShortCutColor;
            SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
            On.Player.FreeHand += Player_FreeHand;
            On.Player.SlugcatGrab += Player_SlugcatGrab;
            On.Player.GrabUpdate += Player_GrabUpdate;
            CutsceneArtificer.Update += CutsceneArtificer_Update;
            On.Player.CanEatMeat += Player_CanEatMeat;
            On.HardmodeStart.SinglePlayerUpdate += HardmodeStart_SinglePlayerUpdate;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;
            MSCRoomSpecificScript.ArtificerDream_1.SceneSetup += ArtificerDream_1_SceneSetup;
            //IL.MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_1.SceneSetup += ArtificerDream_1_SceneSetup;
            MSCRoomSpecificScript.ArtificerDream_1.GetInput += ArtificerDream_1_GetInput;

            MachineConnector.SetRegisteredOI("elumenix.pupify", options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void ArtificerDream_1_SceneSetup(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        int targetIndex = -1;
        for (int i = 0; i < cursor.Instrs.Count; i++)
        {
            if (cursor.Instrs[i].Offset == 0x0283)
            {
                targetIndex = i;
                break;
            }
        }
        
        cursor.Index = targetIndex;
        cursor.RemoveRange(7);
    }

    private Player.InputPackage ArtificerDream_1_GetInput(MSCRoomSpecificScript.ArtificerDream_1.orig_GetInput orig, MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_1 self, int index)
    {
        AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;

        if (self.sceneTimer < 302)
        {
            firstAlivePlayer.realizedCreature.bodyChunks[0].vel.x += 1.75f;
        }
        
		if (self.sceneTimer < 160)
		{
			if (firstAlivePlayer != null)
            {
                (firstAlivePlayer.realizedCreature as Player).SuperHardSetPosition(self.artyPlayerPuppet.firstChunk.pos);
            }
			return default(Player.InputPackage);
            //return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, false, false, true, false, false);
		}
		if (self.sceneTimer == 160)
		{
			self.artyPlayerPuppet.bodyChunks[0].vel *= 0f;
			self.artyPlayerPuppet.bodyChunks[1].vel *= 0f;
			self.artyPlayerPuppet.bodyChunks[0].pos = new Vector2(1900f, 340f);
			self.artyPlayerPuppet.bodyChunks[1].pos = new Vector2(1900f, 320f);
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
		}
		if (self.sceneTimer == 165)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
		}
		if (self.sceneTimer < 166)
        {
            return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
		}
		if (self.sceneTimer == 166)
		{
			self.artyPlayerPuppet.bodyChunks[0].vel += new Vector2(10f, 13f);
			self.artyPlayerPuppet.bodyChunks[1].vel += new Vector2(10f, 13f);
			self.room.AddObject(new ExplosionSpikes(self.room, self.artyPlayerPuppet.bodyChunks[0].pos + new Vector2(0f, -self.artyPlayerPuppet.bodyChunks[0].rad), 8, 7f, 5f, 5.5f, 40f, new Color(1f, 1f, 1f, 0.5f)));
			self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.artyPlayerPuppet.bodyChunks[0], false, 1f, 1f);
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
		}
		if (self.sceneTimer <= 190)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
		}
		if (self.sceneTimer <= 210)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 1, false, false, false, false, false);
		}
		if (self.sceneTimer == 211)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
		}
		if (self.sceneTimer == 212)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 1, false, false, false, false, false);
		}
		if (self.sceneTimer <= 239)
		{
			return default(Player.InputPackage);
		}
		if (self.sceneTimer <= 300)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, false, false, false);
		}
		if (self.sceneTimer == 301)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
		}
		if (self.sceneTimer == 302)
		{
			//self.artyPlayerPuppet.slugOnBack.DropSlug();
			if (firstAlivePlayer != null)
			{
				(firstAlivePlayer.realizedCreature as Player).Stun(5);
				(firstAlivePlayer.realizedCreature as Player).firstChunk.vel = new Vector2(5f, 5f);
				(firstAlivePlayer.realizedCreature as Player).standing = true;
			}
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
		}
		if (self.sceneTimer <= 304)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, false, false, false, false, false);
		}
		if (self.sceneTimer <= 325)
		{
			self.ArtyGoalPos = self.room.MiddleOfTile(116, 18);
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
		}
		if (self.sceneTimer > 325)
		{
			bool jmp = false;
			int x = 0;
			if (self.artyPlayerPuppet.firstChunk.pos.x < self.ArtyGoalPos.x - 9f)
			{
				x = 1;
			}
			else if (self.artyPlayerPuppet.firstChunk.pos.x > self.ArtyGoalPos.x + 9f)
			{
				x = -1;
				jmp = (self.sceneTimer % 20 <= 5 && self.artyPlayerPuppet.bodyMode != Player.BodyModeIndex.ClimbingOnBeam);
			}
			int y = 0;
			if (self.artyPlayerPuppet.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
			{
				if (self.artyPlayerPuppet.firstChunk.pos.y < self.ArtyGoalPos.y - 5f)
				{
					if (self.artyPlayerPuppet.firstChunk.pos.x > self.ArtyGoalPos.x + 9f)
					{
						y = ((self.sceneTimer % 20 <= 5) ? 0 : 1);
					}
					else
					{
						y = 1;
					}
				}
				else if (self.artyPlayerPuppet.firstChunk.pos.y > self.ArtyGoalPos.y + 5f)
				{
					y = -1;
				}
			}
			else
			{
				y = UnityEngine.Random.Range(0, 2);
			}
			if (firstAlivePlayer != null && Mathf.Abs(self.room.cameraPositions[2].x + self.room.game.cameras[0].sSize.x / 2f - (firstAlivePlayer.realizedCreature as Player).firstChunk.pos.x) > 1000f && self.sceneTimer < 2000)
			{
				Debug.Log("Pup out of camera, cut early");
				self.sceneTimer = 1999;
			}
			else if (self.pup2Puppet.state.dead && self.sceneTimer < 2000)
			{
				Debug.Log("Other pup died! cut early");
				self.sceneTimer = 1999;
			}
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, x, y, jmp, false, false, false, false);
		}
		return default(Player.InputPackage);
	}

    private void DreamsState_StaticEndOfCycleProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfTopLayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
    {
        orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfTopLayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
        if (saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
        {
            //disabled artificer dreams
            upcomingDream = MoreSlugcatsEnums.DreamID.ArtificerFamilyA;
            
        }
    }

    // This was originally meant to be an IL hook but I couldn't find a possible way to delete the line
    // The entire point of this method is simply so that artificer doesn't place a pup on her back because it crashes the game
    private void ArtificerDream_1_SceneSetup(MSCRoomSpecificScript.ArtificerDream_1.orig_SceneSetup orig, MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_1 self)
    {
        if (self.artificerPuppet == null)
		{
			self.artificerPuppet = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new WorldCoordinate(self.room.abstractRoom.index, 87, 8, -1), self.room.game.GetNewID());
			self.artificerPuppet.state = new PlayerState(self.artificerPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
			self.room.abstractRoom.AddEntity(self.artificerPuppet);
			self.pup2Puppet = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, new WorldCoordinate(self.room.abstractRoom.index, 87, 8, -1), self.room.game.GetNewID());
			self.pup2Puppet.ID.setAltSeed(1001);
			self.pup2Puppet.state = new PlayerNPCState(self.pup2Puppet, 0);
			self.room.abstractRoom.AddEntity(self.pup2Puppet);
			self.artificerPuppet.RealizeInRoom();
			self.pup2Puppet.RealizeInRoom();
		}
		if (self.artyPlayerPuppet == null && self.artificerPuppet.realizedCreature != null)
		{
			self.artyPlayerPuppet = (self.artificerPuppet.realizedCreature as Player);
		}
		if (self.pupPlayerPuppet == null && self.pup2Puppet.realizedCreature != null)
		{
			self.pupPlayerPuppet = (self.pup2Puppet.realizedCreature as Player);
		}
		AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;
        if (firstAlivePlayer == null || self.artyPlayerPuppet == null || self.pupPlayerPuppet == null ||
            firstAlivePlayer.realizedCreature == null) return;
        Debug.Log("scene start");
        self.SpawnAmbientCritters();
        self.pup2Puppet.state.socialMemory.GetOrInitiateRelationship(firstAlivePlayer.ID).InfluenceLike(1f);
        self.pup2Puppet.state.socialMemory.GetOrInitiateRelationship(firstAlivePlayer.ID).InfluenceTempLike(1f);
        self.artyPlayerPuppet.controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 0);
        self.artyPlayerPuppet.standing = true;
        self.artyPlayerPuppet.slugcatStats.visualStealthInSneakMode = 2f;
        if (firstAlivePlayer.realizedCreature != null)
        {
            (firstAlivePlayer.realizedCreature as Player)?.SuperHardSetPosition(self.artyPlayerPuppet.firstChunk.pos);
            firstAlivePlayer.pos = self.artyPlayerPuppet.abstractCreature.pos;
        }
        self.sceneStarted = true;
    }


    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        
        // This should always happen, because render as pup is the thing that causes it
        // I'm using the same variable names as the decompiled dll to give an idea of position and how much code I'm skipping
        if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
        {
            float num = 0.5f +
                        0.5f * Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f);

            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 vector3 = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            
            if (self.player.aerobicLevel > 0.5f)
            {
                vector += Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) *
                          Mathf.InverseLerp(0.5f, 1f, self.player.aerobicLevel) * 0.5f;
                vector3 -= Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) *
                           Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, self.player.aerobicLevel), 1.5f) * 0.75f;
            }

            float num3 = Custom.AimFromOneVectorToAnother(Vector2.Lerp(vector2, vector, 0.5f), vector3);
            int num4 = Mathf.RoundToInt((Mathf.Abs(num3 / 360f * 34f)));
            
            if (self.player.sleepCurlUp > 0f)
            {
                num4 = 7;
                num4 = Custom.IntClamp((int)Mathf.Lerp(num4, 4f, self.player.sleepCurlUp), 0, 8);
            }
            
            // each if statement from hereon is checking the conditions of several blocks to figure out if num4 changes
            if (self.player.sleepCurlUp <= 0 && self.owner.room != null && self.owner.EffectiveRoomGravity == 0f)
            {
                num4 = 0;
            }
            else if (self.player.Consious)
            {
                if ((self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x != 0) ||
                    self.player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    num4 = self.player.bodyMode == Player.BodyModeIndex.Crawl ? 7 : 6;
                }
            }
            else
            {
                num4 = 0;
            }

            sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName("HeadB" + num4);
        }
    }

    private void HardmodeStart_SinglePlayerUpdate(On.HardmodeStart.orig_SinglePlayerUpdate orig, HardmodeStart self)
    {
        // Hunter's intro, which attempts to fill his food meter past the bar on cycle 0
        orig(self);

        if (options.onlyCosmetic.Value) return;
        
        if (self.room.game.Players[0].realizedCreature is Player {playerState: not null} player)
        {
            player.playerState.foodInStomach = options.foodOption.Value switch
            {
                // Hunter is designed to start 1 food short of being able to hibernate first cycle
                0 => 2,
                1 => 5,
                2 => 1,
                _ => 0
            };
        }
    }

    private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        if (!options.onlyCosmetic.Value)
        {
            if (ModManager.MSC && (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint ||
                                   self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear))
            {
                return false;
            }

            if (self.EatMeatOmnivoreGreenList(crit) && crit.dead)
            {
                return !ModManager.MSC || self.pyroJumpCooldown <= 60f;
            }

            Debug.Log(!(crit is IPlayerEdible));
            Debug.Log(crit.dead);
            Debug.Log(self.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer);
            Debug.Log(!ModManager.CoopAvailable || crit is not Player);
            Debug.Log((!ModManager.MSC || self.pyroJumpCooldown <= 60f));

            return options.letEatMeat.Value && (orig(self, crit) || (!(crit is IPlayerEdible) && crit.dead &&
                                                                     (self.SlugCatClass == SlugcatStats.Name.Red ||
                                                                      (ModManager.MSC &&
                                                                       (self.SlugCatClass ==
                                                                        MoreSlugcatsEnums.SlugcatStatsName
                                                                            .Artificer ||
                                                                        self.SlugCatClass ==
                                                                        MoreSlugcatsEnums.SlugcatStatsName
                                                                            .Gourmand ||
                                                                        self.SlugCatClass ==
                                                                        MoreSlugcatsEnums.SlugcatStatsName
                                                                            .Sofanthiel))) &&
                                                                     (!ModManager.CoopAvailable || !(crit is Player)) &&
                                                                     (!ModManager.MSC ||
                                                                      self.pyroJumpCooldown <= 60f)));
        }
        else
        {
            return orig(self, crit);
        }
    }

    private void CutsceneArtificer_Update(CutsceneArtificer.orig_Update orig, MoreSlugcats.CutsceneArtificer self, bool eu)
    {
        orig(self, eu);

        if (options.onlyCosmetic.Value) return;
        if (self.room.game.Players[0].realizedCreature is not Player {playerState: not null}) return;
        
        // Artificer is meant to start if 4 food pips, however this is because she eats a scavenger in a cutscene
        // This of course can only happen if she has the ability to eat meat
        if (options.letEatMeat.Value)
        {
            // The cutscene needs to handle this part if this is the opening; This check makes sure this isn't the cutscene
            if (!(self.player.myRobot == null || self.player.myRobot != null && self.room.world.rainCycle.timer >= 400))
            {
                // Don't make pup meter go past 4 though
                self.player.playerState.foodInStomach = options.foodOption.Value != 2 ? 4 : 3;
            }
        }
        else
        {
            self.player.playerState.foodInStomach = 0;
        }
    }

    private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        // Despite how important and long this base method is, this override is just to allow switching hands
        if (!options.onlyCosmetic.Value && options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
        {
            if (self.input[0].pckp && !self.input[1].pckp && self.switchHandsProcess == 0f)
            {
                bool handAvailable = self.grasps[0] != null || self.grasps[1] != null;
                if (self.grasps[0] != null &&
                    (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands ||
                     self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag))
                {
                    handAvailable = false;
                }

                if (handAvailable)
                {
                    if (self.switchHandsCounter == 0)
                    {
                        self.switchHandsCounter = 15;
                    }
                    else
                    {
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, self.mainBodyChunk);
                        self.switchHandsProcess = 0.01f;
                        self.wantToPickUp = 0;
                        self.noPickUpOnRelease = 20;
                    }
                }
                else
                {
                    self.switchHandsProcess = 0;
                }
            }
        }
        
        // This should run regardless, for both mod compatibility and to not plagiarize 900 lines of code
        orig(self, eu);

        // Prevents player from using the stomach whatsoever
        if (!options.onlyCosmetic.Value && !options.letStomach.Value && !options.letMaul.Value)
        {
            self.swallowAndRegurgitateCounter = 0;
        }
        else if (!options.onlyCosmetic.Value)
        {
            // Allows stomach use while maul is active
            if (!options.letStomach.Value && self.maulTimer == 0)
            {
                // prevents normal stomach only
                self.swallowAndRegurgitateCounter = 0;
            }
            else if (!options.letMaul.Value) // Player only isn't allowed to maul
            {
                // only stops mauling
                self.maulTimer = 0;
            }
        }
    }

    private void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
    {
        // This override is so that the player may actually pick up an item into their second hand
        if (!options.onlyCosmetic.Value && options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
        {
            // Moon cloak code was apparently unreachable, so it was removed, hopefully that doesn't cause errors
		    if (obj is IPlayerEdible && (!ModManager.MMF || obj is Creature {dead: true} || obj is not Centipede centipede || (centipede.Small)))
		    {
			    self.Grab(obj, graspUsed, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
		    }
		    int chunkGrabbed = 0;
		    if (self.Grabability(obj) == Player.ObjectGrabability.Drag)
		    {
			    float dst = float.MaxValue;
			    for (int i = 0; i < obj.bodyChunks.Length; i++)
			    {
				    if (Custom.DistLess(self.mainBodyChunk.pos, obj.bodyChunks[i].pos, dst))
				    {
					    dst = Vector2.Distance(self.mainBodyChunk.pos, obj.bodyChunks[i].pos);
					    chunkGrabbed = i;
				    }
			    }
		    }
		    self.switchHandsCounter = 0;
		    self.wantToPickUp = 0;
		    self.noPickUpOnRelease = 20;
		    
            // Removed slugpup grab limiter
            
		    bool flag = true;
            if (obj is Creature creature)
		    {
			    if (self.IsCreatureImmuneToPlayerGrabStun(creature))
			    {
				    flag = false;
			    }
			    else if (!creature.dead && !self.IsCreatureLegalToHoldWithoutStun(creature))
			    {
				    flag = false;
			    }
		    }

            self.Grab(obj, graspUsed, chunkGrabbed, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true,
                (ModManager.MMF || ModManager.CoopAvailable) ? flag : obj is not Cicada && obj is not JetFish);
        }
        else
        {
            orig(self, obj, graspUsed);
        }
    }

    private int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
    {
        // This override lets slugpups realize they have a second hand
        if (!options.onlyCosmetic.Value && options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
        {
            if (self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed))
            {
                return -1;
            }

            for (int i = 0; i < self.grasps.Length; i++)
            {
                // Normally this would be prevented for slugpups
                if (self.grasps[i] == null)
                {
                    return i;
                }
            }

            // Both hands occupied
            return -1;
        }
        else
        {
            return orig(self);
        }
    }


    private void SlugcatPageContinue_ctor(SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig,
        Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex,
        SlugcatStats.Name slugcatNumber)
    {
        if (options.onlyCosmetic.Value)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);
        }
        else // Theres no easy way to do this without copying code, all credit to original rain world developers
        {
            // The first several lines of code here is going down a rabbit hole of
            // constructors because I need to skip the orig method
            self.menu = menu;
            self.owner = owner;
            self.subObjects = new List<MenuObject>();
            self.nextSelectable = new MenuObject[4];
            
            self.pos = new Vector2(0.33333334f, 0.33333334f);
            self.lastPos = new Vector2(0.33333334f, 0.33333334f);

            self.name = "Slugcat_Page_" + ((slugcatNumber != null) ? slugcatNumber.ToString() : null);
            self.index = pageIndex;
            self.selectables = new List<SelectableMenuObject>();
            self.mouseCursor = new MouseCursor(menu, self, new Vector2(-100f, -100f));
            self.subObjects.Add(self.mouseCursor);
            
            self.slugcatNumber = slugcatNumber;
            self.effectColor = PlayerGraphics.DefaultSlugcatColor(slugcatNumber);
            if (slugcatNumber == SlugcatStats.Name.Red)
            {
                self.effectColor = Color.Lerp(self.effectColor, Color.red, 0.2f);
            }
            
            if (ModManager.MSC && self.saveGameData.altEnding &&
                ((slugcatNumber == SlugcatStats.Name.White && 
                  menu.manager.rainWorld.progression.miscProgressionData.survivorEndingID > 1) ||
                 (slugcatNumber == SlugcatStats.Name.Yellow &&
                  menu.manager.rainWorld.progression.miscProgressionData.monkEndingID > 1) ||
                 (slugcatNumber != SlugcatStats.Name.White && slugcatNumber != SlugcatStats.Name.Yellow &&
                  slugcatNumber != SlugcatStats.Name.Red)))
            {
                self.AddAltEndingImage();
            }
            else
            {
                self.AddImage(self.saveGameData.ascended);
            }
            self.hudContainers = new FContainer[2];
            for (int i = 0; i < self.hudContainers.Length; i++)
            {
                self.hudContainers[i] = new FContainer();
                self.Container.AddChild(self.hudContainers[i]);
            }
            self.hud = new global::HUD.HUD(self.hudContainers, menu.manager.rainWorld, self);
            self.saveGameData.karma = Custom.IntClamp(self.saveGameData.karma, 0, self.saveGameData.karmaCap);
            if (ModManager.MSC && slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer &&
                self.saveGameData.altEnding &&
                menu.manager.rainWorld.progression.miscProgressionData.artificerEndingID != 1)
            {
                self.saveGameData.karma = 0;
                self.saveGameData.karmaCap = 0;
            }
            if (ModManager.MSC && slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint && self.saveGameData.ascended)
            {
                self.saveGameData.karma = 1;
                self.saveGameData.karmaCap = 1;
            }
            
            self.hud.AddPart(new KarmaMeter(self.hud, self.hudContainers[1],
                new IntVector2(self.saveGameData.karma, self.saveGameData.karmaCap),
                self.saveGameData.karmaReinforced));

            // The strategy here is to just completely redo the calculation each time because it's inexpensive
            // and also prevents me from needing to deal with several edge cases related to changing save files
            // whenever the food option on the mod is changed
            if (options.foodOption.Value == 0) // Calculated values
            {
                float percentRequired = (float) SlugcatStats.SlugcatFoodMeter(slugcatNumber).y /
                                        SlugcatStats.SlugcatFoodMeter(slugcatNumber).x;
                int maxFood = Mathf.RoundToInt(SlugcatStats.SlugcatFoodMeter(slugcatNumber).x * (3f / 7f));
                int foodToHibernate = Mathf.RoundToInt(maxFood * percentRequired * (7f / 6f));
            
                // This may happen with a custom slugcat with ludicrously high food values
                if (foodToHibernate > maxFood)
                {
                    foodToHibernate = maxFood;
                }
                
                // Hopefully this prevents circles from filling in past the bar
                self.saveGameData.food =
                    Custom.IntClamp(self.saveGameData.food, 0, maxFood - foodToHibernate);
                
                // Specific edge case that result from artificer being allowed a cycle 0 save
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && options.letEatMeat.Value &&
                    self.saveGameData.cycle == 0)
                {
                    self.saveGameData.food = 4;
                }
                
                self.hud.AddPart(new FoodMeter(self.hud, maxFood, foodToHibernate));
            }
            else if (options.foodOption.Value == 1) // orig values
            {
                // original method Default food stats

                self.saveGameData.food = Custom.IntClamp(self.saveGameData.food, 0,
                    SlugcatStats.SlugcatFoodMeter(slugcatNumber).y);
                
                // Specific edge case that result from artificer being allowed a cycle 0 save
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && options.letEatMeat.Value &&
                    self.saveGameData.cycle == 0)
                {
                    self.saveGameData.food = 4;
                }
                
                
                self.hud.AddPart(new FoodMeter(self.hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x,
                    SlugcatStats.SlugcatFoodMeter(slugcatNumber).y));
            }
            else // Pup values
            {
                // Third thing where locked to pup
                self.saveGameData.food = Custom.IntClamp(self.saveGameData.food, 0,
                    1);
                
                // Specific edge case that result from artificer being allowed a cycle 0 save
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && options.letEatMeat.Value &&
                    self.saveGameData.cycle == 0)
                {
                    self.saveGameData.food = 3;
                }
                
                self.hud.AddPart(new FoodMeter(self.hud, 3,
                    2));
            }
            
            

            string text = "";
            if (self.saveGameData.shelterName is {Length: > 2})
            {
                text = Region.GetRegionFullName(self.saveGameData.shelterName.Substring(0, 2), slugcatNumber);
                if (text.Length > 0)
                {
                    text = menu.Translate(text);
                    text = text + " - " + menu.Translate("Cycle") + " " + ((slugcatNumber == SlugcatStats.Name.Red)
                        ? (RedsIllness.RedsCycles(self.saveGameData.redsExtraCycles) - self.saveGameData.cycle)
                        : self.saveGameData.cycle);
                    if (ModManager.MMF)
                    {
                        TimeSpan timeSpan = TimeSpan.FromSeconds(self.saveGameData.gameTimeAlive +
                                                                 (double) self.saveGameData.gameTimeDead);
                        text = text + " (" + timeSpan + ")";
                    }
                }
            }
            self.regionLabel = new MenuLabel(menu, self, text, new Vector2(-1000f, self.imagePos.y - 249f), new Vector2(200f, 30f), bigText: true)
            {
                label =
                {
                    alignment = FLabelAlignment.Center
                }
            };
            self.subObjects.Add(self.regionLabel);
        }
    }

    private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        int ID = self.abstractCreature.ID.RandomSeed;
        if (!options.onlyCosmetic.Value && currentSlugcat != null && self.SlugCatClass == currentSlugcat.name &&
            !self.isNPC && ID != 1000 && ID != 1001 && ID != 1002) // Artificer cutscene IDs
        {
            return PlayerGraphics.SlugcatColor(currentSlugcat.name);
        }

        return orig(self);
    }

    private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID) 
    {
        if (!options.onlyCosmetic.Value && ID == ProcessManager.ProcessID.Game)
        {
            // Allow game to set up starving values
            currentSlugcat = null;
        }

        orig(self, ID);
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

                if (options.foodOption.Value == 0) // Calculated
                {
                    // Malnourishment has already been accounted for at this point
                    
                    float percentRequired = (float) self.foodToHibernate / self.maxFood;
                    self.maxFood = Mathf.RoundToInt(self.maxFood * (3f / 7f));
                    self.foodToHibernate =
                        Mathf.RoundToInt(self.maxFood * percentRequired * (7f / 6f));


                    // This may happen with a custom slugcat with ludicrously high food values
                    if (self.foodToHibernate > self.maxFood)
                    {
                        self.foodToHibernate = self.maxFood;
                    }
                }
                else if (options.foodOption.Value == 2) // Pup
                {
                    // This doesn't use previous values, so malnourished 
                    // needs to be hard-coded
                    if (malnourished)
                    {
                        self.foodToHibernate = 3;
                        self.maxFood = 3;
                    }
                    else
                    {
                        self.foodToHibernate = 2;
                        self.maxFood = 3;
                    }
                }
                // Else, don't override the value : Original
            }
            else
            {
                // Don't override the slugpup : malnourishment already calculated
                self.foodToHibernate = currentSlugcat.foodToHibernate;
                self.maxFood = currentSlugcat.maxFood;
            }


            // Stat adjustment option
            if (options.statsOption.Value == 0) // Calculated
            {
                // second condition is added in case food was also adjusted
                if (currentSlugcat == null || currentSlugcat == self)
                {
                    currentSlugcat = self;

                    // Stat adjustments
                    self.runspeedFac = currentSlugcat.runspeedFac * .8f * (.8f / .84f); // NPCStats interferes
                    self.bodyWeightFac = currentSlugcat.bodyWeightFac * .65f * (.65f / .63375f); // NPCStats interferes
                    self.generalVisibilityBonus = currentSlugcat.generalVisibilityBonus - .2f; // Very simple adjustment
                    self.visualStealthInSneakMode =
                        currentSlugcat.visualStealthInSneakMode *
                        1.2f; // Alternative was +.1f, but I thought scaling was better
                    self.loudnessFac = currentSlugcat.loudnessFac * .5f; // Probably the simplest to think about
                    self.lungsFac =
                        currentSlugcat.lungsFac *
                        .8f; // This is the only improvement, all slugpups have better lung capacities 
                    self.poleClimbSpeedFac =
                        currentSlugcat.poleClimbSpeedFac * .8f * (.8f / .836f); // NPCStats interferes
                    self.corridorClimbSpeedFac =
                        currentSlugcat.corridorClimbSpeedFac * .8f * (.8f / .84f); // NPCStats interferes

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
            else if (options.statsOption.Value == 1) // Original
            {
                if (currentSlugcat == null || currentSlugcat == self)
                {
                    currentSlugcat = self;

                    // Stat adjustments, just offset npcStats adjustment
                    self.runspeedFac *= (.8f / .84f); 
                    self.bodyWeightFac *= (.65f / .63375f);
                    self.poleClimbSpeedFac *= (.8f / .836f); 
                    self.corridorClimbSpeedFac *= (.8f / .84f);
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
            else // Pup Route
            {
                if (currentSlugcat == null || currentSlugcat == self)
                {
                    currentSlugcat = self;
                }
                else
                {
                    // Essentially default values are used, this combats npcStats constructor changing player values
                    self.runspeedFac *= (.8f / .84f); 
                    self.bodyWeightFac *= (.65f / .63375f);
                    self.poleClimbSpeedFac *= (.8f / .836f); 
                    self.corridorClimbSpeedFac *= (.8f / .84f);
                }
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
        else
        {
            // An exception will be thrown and the player can't eat if they start with more food than the food bar can hold
            // This can only actually happen if they activate or switch the mod settings mid campaign
            if (self.playerState.foodInStomach > self.slugcatStats.maxFood - self.slugcatStats.foodToHibernate)
            {
                self.playerState.foodInStomach = self.slugcatStats.maxFood - self.slugcatStats.foodToHibernate;
            }
        }
    }

    private void SpearmasterGateLocation_Update(MSCRoomSpecificScript.SpearmasterGateLocation.orig_Update orig, MoreSlugcats.MSCRoomSpecificScript.SpearmasterGateLocation self, bool eu)
    {
        orig(self, eu);

        if (options.onlyCosmetic.Value) return;
        
        // Makes sure that spearmaster doesn't spawn with more food than a pup should have
        // which would cause an out of range error upon the first time eating, strictly on cycle 0
        if (self.room.game.Players[0].realizedCreature is Player {playerState: not null} player)
        {
            player.playerState.foodInStomach = options.foodOption.Value switch
            {
                // Spearmaster is designed to start 1 food short of being able to hibernate first cycle
                0 => 1,
                1 => 4,
                2 => 1,
                _ => 0
            };
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
