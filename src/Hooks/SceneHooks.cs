using MoreSlugcats;
using UnityEngine;
using CutsceneArtificer = On.MoreSlugcats.CutsceneArtificer;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using MSCRoomSpecificScript = On.MoreSlugcats.MSCRoomSpecificScript;
using Random = UnityEngine.Random;

namespace Pupify.Hooks;

public static class SceneHooks
{
    public static void Init()
    {
        // Game start scenes. Food is altered autonomously 
        On.HardmodeStart.SinglePlayerUpdate += HardmodeStart_SinglePlayerUpdate;
        MSCRoomSpecificScript.SpearmasterGateLocation.Update += SpearmasterGateLocation_Update;
        CutsceneArtificer.Update += CutsceneArtificer_Update;

        
        // Problem Scripts : player is forced to use an ability they don't have access to
        MSCRoomSpecificScript.ArtificerDream_1.SceneSetup += ArtificerDream_1_SceneSetup;
        MSCRoomSpecificScript.ArtificerDream_1.GetInput += ArtificerDream_1_GetInput;
        MSCRoomSpecificScript.ArtificerDream_4.SceneSetup += ArtificerDream_4_SceneSetup;
        MSCRoomSpecificScript.ArtificerDream_4.GetInput += ArtificerDream_4_GetInput;
        MSCRoomSpecificScript.ArtificerDream_5.SceneSetup += ArtificerDream_5_SceneSetup;
    }
    
    
    private static void HardmodeStart_SinglePlayerUpdate(On.HardmodeStart.orig_SinglePlayerUpdate orig, HardmodeStart self)
    {
        // Hunter's intro, which attempts to fill his food meter past the bar on cycle 0
        orig(self);

        if (Plugin.options.onlyCosmetic.Value) return;

        if (self.room.game.Players[0].realizedCreature is not Player {playerState: not null} player) return;
        if (Plugin.options.overrideFood.Value) // Food Override
        {
	        player.playerState.foodInStomach = Mathf.RoundToInt(Plugin.options.maxFood.Value) - 1;
        }
        else // Food Option
        {
	        player.playerState.foodInStomach = Plugin.options.foodOption.Value switch
	        {
		        // Hunter is designed to start 1 food short of being able to hibernate first cycle
		        0 => 2,
		        1 => 5,
		        2 => 1,
		        _ => 0
	        };
        }
    }


    private static void SpearmasterGateLocation_Update(MSCRoomSpecificScript.SpearmasterGateLocation.orig_Update orig,
	    MoreSlugcats.MSCRoomSpecificScript.SpearmasterGateLocation self, bool eu)
    {
        orig(self, eu);

        if (Plugin.options.onlyCosmetic.Value) return;
        if (self.room.game.Players[0].realizedCreature is not Player {playerState: not null} player) return;
        
        // Makes sure that spearmaster doesn't spawn with more food than a pup should have
        // which would cause an out of range error upon the first time eating, strictly on cycle 0
        if (Plugin.options.overrideFood.Value) // Food Override
        {
	        player.playerState.foodInStomach = Mathf.RoundToInt(Plugin.options.maxFood.Value) - 1;
        }
        else // Food Option
        {
	        player.playerState.foodInStomach = Plugin.options.foodOption.Value switch
	        {
		        // Spearmaster is designed to start 1 food short of being able to hibernate first cycle
		        0 => 1,
		        1 => 4,
		        2 => 1,
		        _ => 0
	        };
        }
    }


    private static void CutsceneArtificer_Update(CutsceneArtificer.orig_Update orig,
	    MoreSlugcats.CutsceneArtificer self, bool eu)
    {
        orig(self, eu);

        if (Plugin.options.onlyCosmetic.Value) return;
        if (self.room.game.Players[0].realizedCreature is not Player {playerState: not null}) return;
        
        // Artificer is meant to start if 4 food pips, however this is because she eats a scavenger in a cutscene
        // This of course can only happen if she has the ability to eat meat
        if (Plugin.options.letEatMeat.Value)
        {
            // The cutscene needs to handle this part if this is the opening; This check makes sure this isn't the cutscene
            if (!(self.player.myRobot == null || self.player.myRobot != null && self.room.world.rainCycle.timer >= 400))
            {
	            if (Plugin.options.overrideFood.Value) // Food Override
	            {
		            int inStomach = 4; // initial value from eating the scavenger
		            int largestPossible = Mathf.RoundToInt(Plugin.options.maxFood.Value) -
		                                  Mathf.RoundToInt(Plugin.options.foodToHibernate.Value);

		            if (inStomach > largestPossible)
		            {
			            inStomach = largestPossible;
		            }

		            self.player.playerState.foodInStomach = inStomach;
	            }
	            else // Food Option
	            {
		            // Don't make pup meter go past 4 though
		            self.player.playerState.foodInStomach = Plugin.options.foodOption.Value != 2 ? 4 : 3;
	            }
            }
        }
        else
        {
            self.player.playerState.foodInStomach = 0;
        }
    }
    
    
    // This was originally meant to be an IL hook but I couldn't find a possible way to delete the line
    // The entire point of this method is simply so that artificer doesn't place a pup on her back because it crashes the game
    private static void ArtificerDream_1_SceneSetup(MSCRoomSpecificScript.ArtificerDream_1.orig_SceneSetup orig,
	    MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_1 self)
    {
        if (self.artificerPuppet == null)
        {
	        self.artificerPuppet = new AbstractCreature(self.room.world,
		        StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null,
		        new WorldCoordinate(self.room.abstractRoom.index, 87, 8, -1), self.room.game.GetNewID());
	        self.artificerPuppet.state = new PlayerState(self.artificerPuppet, 0,
		        MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
			self.room.abstractRoom.AddEntity(self.artificerPuppet);
			self.pup2Puppet = new AbstractCreature(self.room.world,
				StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null,
				new WorldCoordinate(self.room.abstractRoom.index, 87, 8, -1), self.room.game.GetNewID());
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


    private static Player.InputPackage ArtificerDream_1_GetInput(
	    MSCRoomSpecificScript.ArtificerDream_1.orig_GetInput orig,
	    MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_1 self, int index)
    {
        AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;

        if (self.sceneTimer < 302)
        {
            firstAlivePlayer.realizedCreature.bodyChunks[0].vel.x += 1.75f;
        }
        
		if (self.sceneTimer < 160)
		{
            (firstAlivePlayer?.realizedCreature as Player)?.SuperHardSetPosition(self.artyPlayerPuppet.firstChunk.pos);
            return default(Player.InputPackage);
            //return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, false, false, true, false, false);
		}
		if (self.sceneTimer == 160)
		{
			self.artyPlayerPuppet.bodyChunks[0].vel *= 0f;
			self.artyPlayerPuppet.bodyChunks[1].vel *= 0f;
			self.artyPlayerPuppet.bodyChunks[0].pos = new Vector2(1900f, 340f);
			self.artyPlayerPuppet.bodyChunks[1].pos = new Vector2(1900f, 320f);
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true,
				false, false);
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
				((Player) firstAlivePlayer.realizedCreature).Stun(5);
				((Player) firstAlivePlayer.realizedCreature).firstChunk.vel = new Vector2(5f, 5f);
				((Player) firstAlivePlayer.realizedCreature).standing = true;
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
				y = Random.Range(0, 2);
			}
			if (firstAlivePlayer != null && Mathf.Abs(self.room.cameraPositions[2].x + self.room.game.cameras[0].sSize.x / 2f - ((Player) firstAlivePlayer.realizedCreature).firstChunk.pos.x) > 1000f && self.sceneTimer < 2000)
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
    
    
    private static void ArtificerDream_4_SceneSetup(MSCRoomSpecificScript.ArtificerDream_4.orig_SceneSetup orig, MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_4 self)
    {
        if (self.artificerPuppet == null)
		{
			self.artificerPuppet = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new WorldCoordinate(self.room.abstractRoom.index, 52, 42, -1), self.room.game.GetNewID());
			self.artificerPuppet.state = new PlayerState(self.artificerPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
			self.room.abstractRoom.AddEntity(self.artificerPuppet);
			self.artificerPuppet.RealizeInRoom();
			self.pup2Puppet = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new WorldCoordinate(self.room.abstractRoom.index, 52, 42, -1), self.room.game.GetNewID());
			self.pup2Puppet.ID.setAltSeed(1001);
			self.pup2Puppet.state = new PlayerState(self.pup2Puppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Slugpup, true);
			self.room.abstractRoom.AddEntity(self.pup2Puppet);
			self.pup2Puppet.RealizeInRoom();
		}
		AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;
		if (firstAlivePlayer is {realizedCreature: not null})
		{
			((Player) firstAlivePlayer.realizedCreature).SuperHardSetPosition(self.room.MiddleOfTile(new WorldCoordinate(self.room.abstractRoom.index, 50, 44, -1).Tile));
		}
		if (self.artyPlayerPuppet == null && self.artificerPuppet.realizedCreature != null)
		{
			self.artyPlayerPuppet = (self.artificerPuppet.realizedCreature as Player);
		}
		if (self.pup2PlayerPuppet == null && self.pup2Puppet.realizedCreature != null)
		{
			self.pup2PlayerPuppet = (self.pup2Puppet.realizedCreature as Player);
			self.pup2PlayerPuppet!.controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 1);
		}
		if (firstAlivePlayer != null && self.artyPlayerPuppet != null && self.pup2PlayerPuppet != null && firstAlivePlayer.realizedCreature != null)
		{
			self.artyPlayerPuppet.controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 0);
			self.artyPlayerPuppet.standing = true;
			((Player) firstAlivePlayer.realizedCreature).controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 2);
			//self.artyPlayerPuppet.slugOnBack.SlugToBack(self.pup2PlayerPuppet);
			DataPearl.AbstractDataPearl abstractDataPearl = new DataPearl.AbstractDataPearl(self.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, new WorldCoordinate(self.room.abstractRoom.index, 50, 42, -1), self.room.game.GetNewID(), self.room.abstractRoom.index, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc2);
			abstractDataPearl.RealizeInRoom();
			// ReSharper disable once CommentTypo
			//(firstAlivePlayer.realizedCreature as Player).Grab(abstractDataPearl.realizedObject, 0, 0, Creature.Grasp.Shareability.CanNotShare, 1f, true, false);
			self.sceneStarted = true;
		}
    }
    
    
    private static Player.InputPackage ArtificerDream_4_GetInput(MSCRoomSpecificScript.ArtificerDream_4.orig_GetInput orig, MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_4 self, int index)
    {
        int num = 219;
		int num2 = num + 60;
		int num3 = 400;
		if (index == 2)
		{
			AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;
			if (self.sceneTimer < num - 109)
			{
				return default(Player.InputPackage);
			}
			if (self.sceneTimer != num2 + 6)
			{
				if (firstAlivePlayer != null)
				{
					((Player) firstAlivePlayer.realizedCreature).standing = true;
				}
				return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, false, false, false, false, false);
			}
			if (firstAlivePlayer != null)
			{
				((Player) firstAlivePlayer.realizedCreature).controller = null;
			}
		}
		if (self.sceneTimer < num - 50 && (index == 0 || index == 2))
		{
			int pyroJumpCounter = 1;
			self.artyPlayerPuppet.standing = true;
			self.artyPlayerPuppet.pyroJumpCounter = pyroJumpCounter;
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, false, false, false, false, false);
		}
		if (self.sceneTimer < num + 25)
		{
			self.artyPlayerPuppet.standing = true;
			self.pup2PlayerPuppet.standing = true;
			if (index == 0 && self.sceneTimer > num - 10)
			{
				if (self.sceneTimer >= num && self.sceneTimer < num + 10)
				{
					return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
				}
				if (self.sceneTimer == num + 10)
				{
					return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
				}
				if (self.sceneTimer == num + 11)
				{
					return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
				}
				if (self.sceneTimer > num + 11)
				{
					return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
				}
				return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, false, false, false);
			}
		}
		if (self.sceneTimer < num2 && index == 0)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 0, 1, false, false, false, false, false);
		}
		if (self.sceneTimer == num2 && index == 0)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
		}
		if (self.sceneTimer <= num2 + 15 && index == 0)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, false, false, false);
		}
		if (self.sceneTimer == num2 + 16 && index == 0)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, false, false, true, false, false);
		}
		if (self.sceneTimer <= num2 + 20 && index == 0)
		{
			if (self.sceneTimer == num2 + 17)
			{
				Debug.Log("pup release");
				/*if (self.artyPlayerPuppet.slugOnBack.HasASlug)
				{
					self.artyPlayerPuppet.slugOnBack.DropSlug();
				}*/
			}
			if (self.sceneTimer == num2 + 18)
			{
				Debug.Log("pup launch");
				self.pup2PlayerPuppet.Stun(60);
				self.pup2PlayerPuppet.bodyChunks[0].vel = new Vector2(14f, 16f);
				self.pup2PlayerPuppet.bodyChunks[1].vel = new Vector2(14f, 16f);
				self.artyPlayerPuppet.bodyChunks[0].vel = new Vector2(17f, 38f);
				self.artyPlayerPuppet.bodyChunks[1].vel = new Vector2(17f, 38f);
			}
			self.artyPlayerPuppet.playerState.permanentDamageTracking = 0.5;
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 1, true, false, true, false, false);
		}
		if (self.sceneTimer < num2 + 100 && index == 1)
		{
			self.pup2PlayerPuppet.standing = true;
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, (self.sceneTimer < num2 + 102) ? 1 : 0, true, false, false, false, false);
		}
		if (self.sceneTimer > num3 && self.sceneTimer < 800 && index == 0)
		{
			if (self.artyPlayerPuppet.firstChunk.pos.x < 1939f)
			{
				self.artyPlayerPuppet.firstChunk.vel += new Vector2(0f, 1f);
				self.artyPlayerPuppet.playerState.permanentDamageTracking = 0.0;
			}
			else
			{
				self.artyPlayerPuppet.playerState.permanentDamageTracking = 0.5;
			}
			int y = (self.sceneTimer < num3 + 2) ? 1 : 0;
			if (!self.artyPlayerPuppet.standing && Random.value < 0.1)
			{
				y = 1;
			}
			if (!self.pup2PlayerPuppet.standing && Random.value < 0.1)
			{
				y = 1;
			}
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, y, false, false, false, false, false);
		}
		if (self.sceneTimer > num2 + 100 && self.sceneTimer < 700 && index == 1)
		{
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, true, false, false, false, false);
		}
		if (self.sceneTimer > num2 + 101 && self.sceneTimer < 700 && index == 1)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return new Player.InputPackage(false, global::Options.ControlSetup.Preset.None, 1, 0, Mathf.Floor(Mathf.Pow(Random.value + 0.5f, 4f)) == 1f, false, false, false, false);
		}
		return default(Player.InputPackage);
    }
    
    
    private static void ArtificerDream_5_SceneSetup(MSCRoomSpecificScript.ArtificerDream_5.orig_SceneSetup orig, MoreSlugcats.MSCRoomSpecificScript.ArtificerDream_5 self)
    {
	    self.UpdateCycle(0);
	    if (self.artificerPuppet == null)
	    {
		    self.artificerPuppet = new AbstractCreature(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new WorldCoordinate(self.room.abstractRoom.index, 120, 46, -1), self.room.game.GetNewID());
		    self.artificerPuppet.state = new PlayerState(self.artificerPuppet, 0, MoreSlugcatsEnums.SlugcatStatsName.Artificer, true);
		    self.room.abstractRoom.AddEntity(self.artificerPuppet);
		    self.artificerPuppet.RealizeInRoom();
	    }
	    if (self.artyPlayerPuppet == null && self.artificerPuppet.realizedCreature != null)
	    {
		    self.artyPlayerPuppet = (self.artificerPuppet.realizedCreature as Player);
	    }
	    AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;
	    if (firstAlivePlayer != null && self.artyPlayerPuppet is not null && firstAlivePlayer.realizedCreature != null)
	    {
		    self.artyPlayerPuppet.controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 0);
		    self.artyPlayerPuppet.standing = true;
		    ((Player) firstAlivePlayer.realizedCreature).SuperHardSetPosition(self.artyPlayerPuppet.firstChunk.pos);
		    ((Player) firstAlivePlayer.realizedCreature).controller = new MoreSlugcats.MSCRoomSpecificScript.ArtificerDream.StartController(self, 1);
		    //self.artyPlayerPuppet.slugOnBack.SlugToBack(firstAlivePlayer.realizedCreature as Player);
		    self.sceneStarted = true;
		    self.SpawnLeeches();
	    }
    }
}
