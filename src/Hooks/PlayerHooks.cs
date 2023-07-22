using HarmonyLib;
using RWCustom;
using UnityEngine;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;

namespace Pupify.Hooks;

public static class PlayerHooks
{
    public static void Init()
    {
        // Constructor
        On.Player.ctor += Player_ctor;
        
        // Function & Abilities
        On.Player.FreeHand += Player_FreeHand;
        On.Player.GrabUpdate += Player_GrabUpdate;
        On.Player.SlugcatGrab += Player_SlugcatGrab;
        On.Player.CanEatMeat += Player_CanEatMeat;
        
        // Stats
        On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
        On.SlugcatStats.ctor += SlugcatStats_ctor;
        
        // Appearance
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.Player.ShortCutColor += Player_ShortCutColor;

        // The crux of the mod : Harmony patch to isSlugpup
        Harmony harmony = new Harmony("PupifyHarmony");
        var slugpupMethod = typeof(Player).GetProperty("isSlugpup")?.GetGetMethod();
        var slugpupCheck = typeof(PlayerHooks).GetMethod("Player_isSlugpup");
        harmony.Patch(slugpupMethod, prefix: new HarmonyMethod(slugpupCheck));
    }


    #region Constructor
    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        
        // Aesthetic Mode : Yes, this is all that is needed
        if (Plugin.options.onlyCosmetic.Value)
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
    #endregion
    

    #region Functions & Abilities
    private static int Player_FreeHand(On.Player.orig_FreeHand orig, Player self)
    {
        // This override lets slugpups realize they have a second hand
        if (!Plugin.options.onlyCosmetic.Value && Plugin.options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
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
    
    
    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        // Part 1 of this method is simply to allow players to switch hands (if they have two hands)
        if (!Plugin.options.onlyCosmetic.Value && Plugin.options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
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
        // Part 2 of this method starts after orig
        orig(self, eu);

        // Prevents player from using the stomach whatsoever
        if (!Plugin.options.onlyCosmetic.Value && !Plugin.options.letStomach.Value && !Plugin.options.letMaul.Value)
        {
            self.swallowAndRegurgitateCounter = 0;
        }
        else if (!Plugin.options.onlyCosmetic.Value)
        {
            // Allows stomach use while maul is active
            if (!Plugin.options.letStomach.Value && self.maulTimer == 0)
            {
                // prevents normal stomach only
                self.swallowAndRegurgitateCounter = 0;
            }
            else if (!Plugin.options.letMaul.Value) // Player only isn't allowed to maul
            {
                // only stops mauling
                self.maulTimer = 0;
            }
        }
    }
    
    
    private static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
    {
        // This override is so that the player may actually pick up an item into their second hand
        if (!Plugin.options.onlyCosmetic.Value && Plugin.options.bothHands.Value && self.SlugCatClass.value != "Slugpup")
        {
            // Moon cloak code was apparently unreachable, so it was removed, hopefully that doesn't cause errors
            if (obj is IPlayerEdible && (!ModManager.MMF || obj is Creature {dead: true} ||
                                         obj is not Centipede centipede || centipede.Small))
            {
                self.Grab(obj, graspUsed, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false,
                    true);
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
    
    
    private static bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        if (!Plugin.options.onlyCosmetic.Value)
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

            // This looks ugly, but it is what it is
            return Plugin.options.letEatMeat.Value && (orig(self, crit) || (!(crit is IPlayerEdible) && crit.dead && 
                                                                            (self.SlugCatClass ==
                                                                             SlugcatStats.Name.Red ||
                                                                             (ModManager.MSC &&
                                                                              (self.SlugCatClass ==
                                                                               MoreSlugcatsEnums
                                                                                   .SlugcatStatsName
                                                                                   .Artificer ||
                                                                               self.SlugCatClass ==
                                                                               MoreSlugcatsEnums
                                                                                   .SlugcatStatsName
                                                                                   .Gourmand ||
                                                                               self.SlugCatClass ==
                                                                               MoreSlugcatsEnums
                                                                                   .SlugcatStatsName
                                                                                   .Sofanthiel))) &&
                                                                            (!ModManager.CoopAvailable ||
                                                                             !(crit is Player)) &&
                                                                            (!ModManager.MSC ||
                                                                             self.pyroJumpCooldown <= 60f)));
        }
        else
        {
            return orig(self, crit);
        }
    }
    #endregion


    #region Stats
    private static void Player_GetInitialSlugcatClass(On.Player.orig_GetInitialSlugcatClass orig, Player self)
    {
        // This entire method, despite seeming important for this mod, only actually has to be used in
        // arena mode, where a players original slugcatStats variable gets deleted when changed to pup
        // This small change updates the player to the correct value upon spawning in
        if (!self.isNPC && !(ModManager.CoopAvailable && self.abstractCreature.Room.world.game.IsStorySession) &&
            !(!ModManager.MSC || self.abstractCreature.Room.world.game.IsStorySession) && self.slugcatStats == null)
        {
            self.SlugCatClass = Plugin.currentSlugcat.name;
        }
        else
        {
            orig(self);
        }
    }


    private static void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat,
        bool malnourished)
    {
        // this will correct body color changes
        orig(self, slugcat, Plugin.currentSlugcat is {malnourished: true} || malnourished);

        if (Plugin.options.onlyCosmetic.Value) return;
        // This block will run twice, once following the initial creation of the campaign character
        // The second after the creation of the pup. The player actually plays as the pup
        // I store a reference to the first character, who will be instantiated with the correct stats the first time through
        // When the NPCStats method runs, after the program realizes I set the player to be treated as a pup, the 
        // method creates a new slugCatStats for the player. The second time through overwrites those stats directly
        // This is how both stat calculation rules work. Natural slugpup rules will instead not overwrite on the second run
            
        // Arena works slightly differently in that the game established everyone as a Survivor first, then changes 
        // their stats & class afterwards, so secondary checks are to allow the stats to get overwritten
        // Otherwise, depending on implementation, the game either crashes or spawns everyone as a survivor pup

        if (Plugin.currentSlugcat == null || (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Slugpup &&
                                              Plugin.currentSlugcat.name == SlugcatStats.Name.White))
        {
            Plugin.currentSlugcat = self;

            if (Plugin.options.foodOption.Value == 0) // Calculated
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
            else if (Plugin.options.foodOption.Value == 2) // Pup
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
            self.foodToHibernate = Plugin.currentSlugcat.foodToHibernate;
            self.maxFood = Plugin.currentSlugcat.maxFood;
        }


        // Stat adjustment option
        if (Plugin.options.statsOption.Value == 0) // Calculated
        {
            // second condition is added in case food was also adjusted
            if (Plugin.currentSlugcat == null || Plugin.currentSlugcat == self ||
                (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Slugpup &&
                 Plugin.currentSlugcat.name == SlugcatStats.Name.White))
            {
                Plugin.currentSlugcat = self;

                // Stat adjustments
                self.runspeedFac = Plugin.currentSlugcat.runspeedFac * .8f * (.8f / .84f); // NPCStats interferes
                self.bodyWeightFac = Plugin.currentSlugcat.bodyWeightFac * .65f * (.65f / .63375f); // NPCStats interferes
                self.generalVisibilityBonus = Plugin.currentSlugcat.generalVisibilityBonus - .2f; // Very simple adjustment
                self.visualStealthInSneakMode =
                    Plugin.currentSlugcat.visualStealthInSneakMode *
                    1.2f; // Alternative was +.1f, but I thought scaling was better
                self.loudnessFac = Plugin.currentSlugcat.loudnessFac * .5f; // Probably the simplest to think about
                self.lungsFac =
                    Plugin.currentSlugcat.lungsFac *
                    .8f; // This is the only improvement, all slugpups have better lung capacities 
                self.poleClimbSpeedFac =
                    Plugin.currentSlugcat.poleClimbSpeedFac * .8f * (.8f / .836f); // NPCStats interferes
                self.corridorClimbSpeedFac =
                    Plugin.currentSlugcat.corridorClimbSpeedFac * .8f * (.8f / .84f); // NPCStats interferes

                // This is a weird one because it's such a big difference, but it is only an int and doesn't vary much
                self.throwingSkill = Plugin.currentSlugcat.throwingSkill - 1;
                if (self.throwingSkill < 0)
                {
                    self.throwingSkill = 0;
                }
            }
            else
            {
                // Apply all values to pup
                self.runspeedFac = Plugin.currentSlugcat.runspeedFac;
                self.bodyWeightFac = Plugin.currentSlugcat.bodyWeightFac;
                self.generalVisibilityBonus = Plugin.currentSlugcat.generalVisibilityBonus;
                self.visualStealthInSneakMode = Plugin.currentSlugcat.visualStealthInSneakMode;
                self.loudnessFac = Plugin.currentSlugcat.loudnessFac;
                self.lungsFac = Plugin.currentSlugcat.lungsFac;
                self.poleClimbSpeedFac = Plugin.currentSlugcat.poleClimbSpeedFac;
                self.corridorClimbSpeedFac = Plugin.currentSlugcat.corridorClimbSpeedFac;
                self.throwingSkill = Plugin.currentSlugcat.throwingSkill;
            }
        }
        else if (Plugin.options.statsOption.Value == 1) // Original
        {
            if (Plugin.currentSlugcat == null || Plugin.currentSlugcat == self ||
                (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Slugpup &&
                 Plugin.currentSlugcat.name == SlugcatStats.Name.White))
            {
                Plugin.currentSlugcat = self;

                // Stat adjustments, just offset npcStats adjustment
                self.runspeedFac *= (.8f / .84f); 
                self.bodyWeightFac *= (.65f / .63375f);
                self.poleClimbSpeedFac *= (.8f / .836f); 
                self.corridorClimbSpeedFac *= (.8f / .84f);
            }
            else
            {
                // Apply all values to pup
                self.runspeedFac = Plugin.currentSlugcat.runspeedFac;
                self.bodyWeightFac = Plugin.currentSlugcat.bodyWeightFac;
                self.generalVisibilityBonus = Plugin.currentSlugcat.generalVisibilityBonus;
                self.visualStealthInSneakMode = Plugin.currentSlugcat.visualStealthInSneakMode;
                self.loudnessFac = Plugin.currentSlugcat.loudnessFac;
                self.lungsFac = Plugin.currentSlugcat.lungsFac;
                self.poleClimbSpeedFac = Plugin.currentSlugcat.poleClimbSpeedFac;
                self.corridorClimbSpeedFac = Plugin.currentSlugcat.corridorClimbSpeedFac;
                self.throwingSkill = Plugin.currentSlugcat.throwingSkill;
            }
        }
        else // Pup Route
        {
            if (Plugin.currentSlugcat == null || Plugin.currentSlugcat == self ||
                (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Slugpup &&
                 Plugin.currentSlugcat.name == SlugcatStats.Name.White))
            {
                Plugin.currentSlugcat = self;
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
    #endregion


    #region Appearance
    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        
        // This should always happen, because render as pup is the thing that causes it
        //  Aside from the final line, All of this code is just to find the rotation of the head
        if (self.player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Saint) return;
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
    
    
    private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        int ID = self.abstractCreature.ID.RandomSeed;
        if (!Plugin.options.onlyCosmetic.Value && Plugin.currentSlugcat != null && self.SlugCatClass == Plugin.currentSlugcat.name &&
            !self.isNPC && ID != 1000 && ID != 1001 && ID != 1002) // Artificer cutscene IDs
        {
            return PlayerGraphics.SlugcatColor(Plugin.currentSlugcat.name);
        }

        return orig(self);
    }
    
    
    public static bool Player_isSlugpup(Player __instance, ref bool __result)
    {
        // Use the base method if in cosmetic mode
        if (Plugin.options.onlyCosmetic.Value) return true;
        // This actually is an npc
        __result = true;

        // Don't use the base method
        return false;
    }
    #endregion
}
