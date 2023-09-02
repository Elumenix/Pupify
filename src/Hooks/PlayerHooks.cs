using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JollyCoop;
using RWCustom;
using UnityEngine;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using Random = UnityEngine.Random;

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
        On.Player.Grabability += Player_Grabability;
        On.Player.CanEatMeat += Player_CanEatMeat;
        
        // Stats
        On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
        On.SlugcatStats.ctor += SlugcatStats_ctor;
        On.Player.SetMalnourished += Player_SetMalnourished;
        On.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
        
        // Appearance
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.Player.ShortCutColor += Player_ShortCutColor;
        On.SlugcatHand.Update += SlugcatHandOnUpdate;

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
            // natural slugpup
            if (self.isNPC)
            {
                // recalculate natural stats
                // Slugpups npcStats are saved, but because stats runs twice, the actual stats are set to base initially
                self.slugcatStats.runspeedFac *= 0.85f + 0.15f * self.npcStats.Met + 0.15f * (1f - self.npcStats.Bal) +
                                                 0.1f * (1f - self.npcStats.Stealth);
                self.slugcatStats.bodyWeightFac *= 0.85f + 0.15f * self.npcStats.Wideness + 0.1f * self.npcStats.Met;
                self.slugcatStats.generalVisibilityBonus *=
                    0.8f + 0.2f * (1f - self.npcStats.Stealth) + 0.2f * self.npcStats.Met;
                self.slugcatStats.visualStealthInSneakMode *=
                    0.75f + 0.35f * self.npcStats.Stealth + 0.15f * (1f - self.npcStats.Met);
                self.slugcatStats.loudnessFac *=
                    0.8f + 0.2f * self.npcStats.Wideness + 0.2f * (1f - self.npcStats.Stealth);
                self.slugcatStats.lungsFac *=
                    0.8f + 0.2f * (1f - self.npcStats.Met) + 0.2f * (1f - self.npcStats.Stealth);
                self.slugcatStats.poleClimbSpeedFac *= 0.85f + 0.15f * self.npcStats.Met + 0.15f * self.npcStats.Bal +
                                                       0.1f * (1f - self.npcStats.Stealth);
                self.slugcatStats.corridorClimbSpeedFac *= 0.85f + 0.15f * self.npcStats.Met +
                                                           0.15f * (1f - self.npcStats.Bal) +
                                                           0.1f * (1f - self.npcStats.Stealth);
                
                return;
            }
            
            // This is what lets the player put a slugpup on their back
            if (Plugin.options.letPickup.Value)
            {
                self.slugOnBack = new Player.SlugOnBack(self);
            }
            
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
    
    
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        // Figures out grabability for non slugpups
        var initial = orig(self, obj);

        // If the player is allowed to grab other slugpups
        if (!Plugin.options.onlyCosmetic.Value && Plugin.options.holdHands.Value && !self.isNPC)
        {
            if (!(obj is Creature creature && !creature.Template.smallCreature && (creature.dead ||
                    (SlugcatStats.SlugcatCanMaul(self.SlugCatClass) && self.dontGrabStuff < 1 && creature != self &&
                     !creature.Consious))))
            {
                if (obj is Player player && player != self &&
                    player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup &&
                    !player.playerState.forceFullGrown)
                {
                    initial = Player.ObjectGrabability.OneHand;
                }
            }
        }

        return initial;
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

            // This looks ugly, but it is what it is
            return Plugin.options.letEatMeat.Value && (orig(self, crit) || (crit is not IPlayerEdible && crit.dead && 
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
                                                                             crit is not Player) &&
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
            if (MultiPlayer.Session is not StoryGameSession)
            {
                // Usually I would use MultiPlayer.currentIndex, but apparently players are loaded
                // in a random order so I need to use their ID values instead; 
                self.SlugCatClass = MultiPlayer.GetSpecificPlayer(self.abstractCreature.ID.number).name;
                MultiPlayer.playerToLoad = self.abstractCreature.ID.number;
            }
            else
            {
                self.SlugCatClass = Plugin.currentSlugcat.name;
            }
        }
        else
        {
            orig(self);
        }
    }


    private static void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat,
        bool malnourished)
    {
        // The first check is for story mode, the rest are for arena mode, because it needs to be different apparently
        if (MultiPlayer.startingIncrement >= 2 || MultiPlayer.Session != null &&
            MultiPlayer.Session is not StoryGameSession && MultiPlayer.startingIncrement >= 1)
        {
            MultiPlayer.AddPlayer(self);
        }

        SlugcatStats focus;

        // Players are created in a random order in multiplayer so stats are assigned accordingly
        if (MultiPlayer.Session is ArenaGameSession && Plugin.playersCreated && MultiPlayer.startingIncrement != 4)
        {
            focus = MultiPlayer.GetSpecificPlayer(MultiPlayer.playerToLoad);
        }
        else if (ModManager.CoopAvailable || MultiPlayer.Session is ArenaGameSession || MultiPlayer.Session is SandboxGameSession)
        {
            if (MultiPlayer.onlyPupsLeft || MultiPlayer.startingIncrement == 4)
            {
                focus = self;
            }
            else
            {
                focus = MultiPlayer.GetCurrentPlayer() ?? self;
            }
        }
        else
        {
            // This is so that natural slugpups don't start with players stats instead of their own
            focus = Plugin.playersCreated ? self : Plugin.currentSlugcat ?? self;
        }
        
        // Calling the original just corrects body color changes, all important stats get overwritten after
        orig(self, slugcat, self is {malnourished: true} || focus is {malnourished: true} || malnourished);


        // playerCreated is checked solely in case a slugpup spawns, It prevents the slugpup from copying the player stats
        if (Plugin.options.onlyCosmetic.Value ||
            (Plugin.playersCreated && !ModManager.CoopAvailable && MultiPlayer.Session is not ArenaGameSession) ||
            MultiPlayer.startingIncrement == 4) return;

        // This is the last slugcat to determine, make sure the above if block is never true
        // NumAccessed has essentially the same function as onlyPupsLeft but for arena mode instead of story
        if (MultiPlayer.onlyPupsLeft || MultiPlayer.Session is ArenaGameSession {arenaSitting: not null} arena && 
            arena.arenaSitting.players.Count != 0 && MultiPlayer.numAccessed == arena.arenaSitting.players.Count * 2)
        {
            MultiPlayer.startingIncrement = 4;
        }
        
        // After this character, the game can spawn slugpups normally
        if (!ModManager.CoopAvailable && MultiPlayer.Session is not ArenaGameSession && MultiPlayer.Session is not SandboxGameSession && Plugin.currentSlugcat != null)
        {
            Plugin.playersCreated = true;
        }
        
        // This block will run twice (in single-player), once following the initial creation of the campaign character
        // The second after the creation of the pup. The player actually plays as the pup
        // I store a reference to the first character, who will be instantiated with the correct stats the first time through
        // When the NPCStats method runs, after the program realizes I set the player to be treated as a pup, the 
        // method creates a new slugCatStats for the player. The second time through overwrites those stats directly
        // This is how both stat calculation rules work. Natural slugpup rules will instead not overwrite on the second run
        
        if (focus == self)
        {
            // Set currentSlugcat if not already set
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (Plugin.currentSlugcat == null)
            {
                Plugin.currentSlugcat = self;
            }
            
            if (Plugin.options.overrideFood.Value && self == Plugin.currentSlugcat) // Override Option
            {
                // This luckily is all that's needed for this option due to my multiplayer implementation
                self.foodToHibernate = Mathf.RoundToInt(Plugin.options.foodToHibernate.Value);
                self.maxFood = Mathf.RoundToInt(Plugin.options.maxFood.Value);
            }
            else // Food Option
            {
                switch (Plugin.options.foodOption.Value)
                {
                    // Calculated
                    case 0:
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

                        break;
                    }

                    // Pup
                    // This doesn't use previous values, so malnourished needs to be hard-coded
                    case 2 when malnourished:
                        self.foodToHibernate = 3;
                        self.maxFood = 3;
                        break;
                    case 2:
                        self.foodToHibernate = 2;
                        self.maxFood = 3;
                        break;

                    // default: don't override the value : Original
                }
            }
        }
        else
        {
            // Don't override the slugpup : malnourishment already calculated
            if (!ModManager.JollyCoop)
            {
                self.foodToHibernate = focus.foodToHibernate;
                self.maxFood = focus.maxFood;
            }
            else
            {
                // This line shouldn't ever have a reason to execute, but there was a compiler warning so this is for safety
                // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                if (Plugin.currentSlugcat == null)
                {
                    Plugin.currentSlugcat = self;
                }
                
                // For jolly coop, different foodBar values mean that all players have different
                // values for starvation, and some might not even be able to reach their min value
                // if player one has a smaller min value than them. Standardizing it to the campaign
                // slugcat standardizes starving for everyone and simplifies the food screen
                self.foodToHibernate = Plugin.currentSlugcat.foodToHibernate;
                self.maxFood = Plugin.currentSlugcat.maxFood;
            }
        }


        switch (Plugin.options.statsOption.Value)
        {
            // Stat adjustment option
            // Calculated
            case 0:
            {
                if (Plugin.currentSlugcat == null)
                {
                    Plugin.currentSlugcat = self;
                    focus = self;
                }
            
                if (focus == self || !Plugin.playersCreated && ModManager.CoopAvailable)
                {
                    // Stat adjustments
                    self.runspeedFac = focus.runspeedFac * .8f * (.8f / .84f); // NPCStats interferes
                    self.bodyWeightFac = focus.bodyWeightFac * .65f * (.65f / .63375f); // NPCStats interferes
                    self.generalVisibilityBonus = focus.generalVisibilityBonus - .2f; // Very simple adjustment
                    self.visualStealthInSneakMode =
                        focus.visualStealthInSneakMode *
                        1.2f; // Alternative was +.1f, but I thought scaling was better
                    self.loudnessFac = focus.loudnessFac * .5f; // Probably the simplest to think about
                    self.lungsFac =
                        focus.lungsFac *
                        .8f; // This is the only improvement, all slugpups have better lung capacities 
                    self.poleClimbSpeedFac =
                        focus.poleClimbSpeedFac * .8f * (.8f / .836f); // NPCStats interferes
                    self.corridorClimbSpeedFac =
                        focus.corridorClimbSpeedFac * .8f * (.8f / .84f); // NPCStats interferes

                    // This is a weird one because it's such a big difference, but it is only an int and doesn't vary much
                    self.throwingSkill = focus.throwingSkill - 1;
                    if (self.throwingSkill < 0)
                    {
                        self.throwingSkill = 0;
                    }
                }
                else 
                {
                    // Apply all values to pup
                    self.runspeedFac = focus.runspeedFac;
                    self.bodyWeightFac = focus.bodyWeightFac;
                    self.generalVisibilityBonus = focus.generalVisibilityBonus;
                    self.visualStealthInSneakMode = focus.visualStealthInSneakMode;
                    self.loudnessFac = focus.loudnessFac;
                    self.lungsFac = focus.lungsFac;
                    self.poleClimbSpeedFac = focus.poleClimbSpeedFac;
                    self.corridorClimbSpeedFac = focus.corridorClimbSpeedFac;
                    self.throwingSkill = focus.throwingSkill;
                }
                break;
            }
            
            // Original
            case 1 when focus == null || focus == self:
                // Stat adjustments, just offset npcStats adjustment
                self.runspeedFac *= (.8f / .84f); 
                self.bodyWeightFac *= (.65f / .63375f);
                self.poleClimbSpeedFac *= (.8f / .836f); 
                self.corridorClimbSpeedFac *= (.8f / .84f);
                break;
            case 1:
                // Apply all values to pup
                self.runspeedFac = focus.runspeedFac;
                self.bodyWeightFac = focus.bodyWeightFac;
                self.generalVisibilityBonus = focus.generalVisibilityBonus;
                self.visualStealthInSneakMode = focus.visualStealthInSneakMode;
                self.loudnessFac = focus.loudnessFac;
                self.lungsFac = focus.lungsFac;
                self.poleClimbSpeedFac = focus.poleClimbSpeedFac;
                self.corridorClimbSpeedFac = focus.corridorClimbSpeedFac;
                self.throwingSkill = focus.throwingSkill;
                break;
            
            // Pup Route
            default:
                // Essentially default values are used, this combats npcStats constructor changing player values
                self.runspeedFac *= (.8f / .84f); 
                self.bodyWeightFac *= (.65f / .63375f);
                self.poleClimbSpeedFac *= (.8f / .836f); 
                self.corridorClimbSpeedFac *= (.8f / .84f);
                break;
        }
    }
    
    
    private static void Player_SetMalnourished(On.Player.orig_SetMalnourished orig, Player self, bool m)
    {
        // fixes a glitch in jolly coop where slugpups randomly have their weight set down when not starving
        if (self.npcCharacterStats != null && !m && self.isSlugpup)
        {
            float correctBodyWeight = self.npcCharacterStats.bodyWeightFac;

            orig(self, false);
            self.npcCharacterStats.bodyWeightFac = correctBodyWeight;
            self.slugcatStats.bodyWeightFac = correctBodyWeight;

            float num = 0.7f * self.slugcatStats.bodyWeightFac;
            self.bodyChunks[0].mass = num / 2f;
            self.bodyChunks[1].mass = num / 2f;
        }
        else
        {
            orig(self, m);
        }
    }
    
    
    private static void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
    {
        // When the game is in a state where coop is enabled, starvation is calculated differently, which
        // led to a glitch where the player always starves with the introduced food mechanic changes
        if (!Plugin.options.onlyCosmetic.Value && ModManager.CoopAvailable)
        {
            List<PhysicalObject> list = (from x in self.room.physicalObjects.SelectMany(x => x)
                where x is Player
                select x).ToList();
            int num = list.Count;
            int num2 = 0;

            // Compare to altered food value instead of normal one
            int y = Plugin.currentSlugcat.foodToHibernate;
            
            
            var flag = num >= self.room.game.PlayersToProgressOrWin.Count;
            JollyCustom.Log("Player(s) in shelter: " + num + " Survived: " + flag);
            if (flag)
            {
                foreach (PhysicalObject physicalObject in list)
                {
                    num2 = Math.Max(((Player) physicalObject).FoodInRoom(self.room, false), num2);
                }
                JollyCustom.Log("Survived!, food in room " + num2);
                foreach (AbstractCreature abstractCreature in self.room.game.Players)
                {
                    if (abstractCreature.Room != self.room.abstractRoom)
                    {
                        try
                        {
                            JollyCustom.WarpAndRevivePlayer(abstractCreature, self.room.abstractRoom,
                                self.room.LocalCoordinateOfNode(0));
                        }
                        catch (Exception arg)
                        {
                            JollyCustom.Log($"Could not warp and revive player {abstractCreature} [{arg}]");
                        }
                    }
                }
                self.room.game.Win(num2 < y);
                return;
            }
            self.room.game.GoToDeathScreen();
            return;
        }

        orig(self);
    }
    #endregion


    #region Appearance
    private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        // Use natural slugpup tail instead of cosmetic mode slugpup tail to prevent corridor stalling
        if (Plugin.MakeChanges(self.player) || self.player.isNPC ||
            self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear) return;
        
        float num = 0.85f + 0.3f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f, self.player.playerState.isPup ? 0.5f : 0f);
        float num2 = (0.75f + 0.5f * self.player.npcStats.Size) * (self.player.playerState.isPup ? 0.5f : 1f);
        self.tail[0] = new TailSegment(self, 6f * num, 4f * num2, null, 0.85f, 1f, 1f, true);
        self.tail[1] = new TailSegment(self, 4f * num, 7f * num2, self.tail[0], 0.85f, 1f, 0.5f, true);
        self.tail[2] = new TailSegment(self, 2.5f * num, 7f * num2, self.tail[1], 0.85f, 1f, 0.5f, true);
        self.tail[3] = new TailSegment(self, 1f * num, 7f * num2, self.tail[2], 0.85f, 1f, 0.5f, true);
    }
    
    
    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        
        // PART 1
        // This should always happen, because render as pup is the thing that causes it
        // Aside from the final line, All of this code is just to find the rotation of the head
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
                num4 = Custom.IntClamp((int) Mathf.Lerp(num4, 4f, self.player.sleepCurlUp), 0, 8);
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


        // No point in changing anything else in this case
        if (Plugin.MakeChanges(self.player) || self.player.isNPC)
        {
            return;
        }
        
        // PART 2
        // Some body dimensions need to be outright changed as they are checked with 
        // slugcatStats instead of isSlugpup. Spearmaster is the only one completely unaffected by this
        if (self.player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Gourmand &&
            self.player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            float num = 0.5f +
                        0.5f * Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f);
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            if (self.player.aerobicLevel > 0.5f)
            {
                vector += Custom.DirVec(vector2, vector) * Mathf.Lerp(-1f, 1f, num) *
                          Mathf.InverseLerp(0.5f, 1f, self.player.aerobicLevel) * 0.5f;
            }
            float num2 = Mathf.InverseLerp(0.3f, 0.5f, Mathf.Abs(Custom.DirVec(vector2, vector).y));
            
            
            sLeaser.sprites[0].scaleX = 0.9f +
                0.2f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f, self.player.playerState.isPup ? 0.5f : 0f) +
                self.player.sleepCurlUp * 0.2f + 0.05f * num - 0.05f * self.malnourished;

            sLeaser.sprites[1].scaleX = 0.9f +
                                        0.2f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f,
                                            self.player.playerState.isPup ? 0.5f : 0f) +
                                        Mathf.Lerp(
                                            Mathf.Lerp(Mathf.Lerp(-0.05f, -0.15f, self.malnourished), 0.05f,
                                                num) * num2, 0.15f, self.player.sleepCurlUp);
            
            
            // I honestly have no clue what this is checking or why its a property of Player
            // However, I'm checking everything that can change the values of the pup
            if (ModManager.CoopAvailable && self.player.bool1)
            {
                sLeaser.sprites[0].scaleX += 0.35f;
            }
        }

        // This is thankfully a much simpler to check transformation. Only spearmaster behaves differently
        if (self.player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            sLeaser.sprites[3].scaleX *= 0.9f + 0.2f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f,
                self.player.playerState.isPup ? 0.5f : 0f);
        }

    }
    
    
    private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        int ID = self.abstractCreature.ID.RandomSeed;
        if (!ModManager.CoopAvailable && !Plugin.options.onlyCosmetic.Value && !self.isNPC &&
            !self.isNPC && ID != 1000 && ID != 1001 && ID != 1002) // Artificer cutscene IDs
        {
            return PlayerGraphics.SlugcatColor(self.SlugCatClass);
        }

        return orig(self);
    }
    
    
    private static void SlugcatHandOnUpdate(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);

        // Player is not in cosmetic mode and limited to one hand
        if (!((Player) self.owner.owner).isNPC && !Plugin.options.onlyCosmetic.Value && !Plugin.options.bothHands.Value)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if ((((Player) self.owner.owner).Consious && ((Player) self.owner.owner).grabbedBy.Count > 0 &&
                 ((Player) self.owner.owner).grabbedBy[0]?.grabber is Player &&
                 Mathf.Sign(((Player) self.owner.owner).firstChunk.pos.x -
                            ((Player) self.owner.owner).grabbedBy[0].grabber.firstChunk.pos.x) ==
                 (self.limbNumber == 0 ? -1 : 1)) || (((Player) self.owner.owner).grasps[self.limbNumber] != null &&
                                                      ((Player) self.owner.owner).grasps[self.limbNumber]
                                                      .grabbed is Player && ((Player) ((Player) self.owner.owner)
                                                          .grasps[self.limbNumber].grabbed).Consious))
            {
                // This would all lead to a return before being a slugpup is even considered
                return;
            }

            var flag = !self.reachingForObject && self.EngageInMovement();
            if (ModManager.MMF)
            {
                if (((Player) self.owner.owner).grasps[self.limbNumber] != null &&
                    ((Player) self.owner.owner).HeavyCarry(((Player) self.owner.owner).grasps[self.limbNumber]
                        .grabbed))
                {
                    flag = true;
                }
            }
            else if (self.limbNumber == 0 && ((Player) self.owner.owner).grasps[0] != null &&
                     ((Player) self.owner.owner).HeavyCarry(((Player) self.owner.owner).grasps[0].grabbed))
            {
                flag = true;
            }

            if (flag)
            {
                if ((((Player) self.owner.owner).grasps[0] != null &&
                     ((Player) self.owner.owner).HeavyCarry(((Player) self.owner.owner).grasps[0].grabbed)) ||
                    (ModManager.MMF && ((Player) self.owner.owner).grasps[1] != null &&
                     ((Player) self.owner.owner).HeavyCarry(((Player) self.owner.owner).grasps[1].grabbed)))
                {
                    // return early
                    return;
                }

                if (((Player) self.owner.owner).grasps[self.limbNumber] != null)
                {
                    // This is where a comparison to SlugcatStatsName.Slugpup is made, which return false always
                    // However, we want it to return true on single-hand mode so that an item is held with both hands
                    // Unfortunately, every calculation in and after this branch would need to be redone
                    // A few if checks outside this particular branch in the dll are omitted because
                    // this branch has already confirmed that those parameters could never be true
                    self.relativeHuntPos.x = ((Player) self.owner.owner).ThrowDirection * 3;

                    self.relativeHuntPos.y = -12f;
                    if (((Player) self.owner.owner).eatCounter < 40)
                    {
                        int num = -1;
                        int num2 = 0;
                        while (num < 0 && num2 < 2)
                        {
                            if (((Player) self.owner.owner).grasps[num2] != null &&
                                ((Player) self.owner.owner).grasps[num2].grabbed is IPlayerEdible &&
                                ((IPlayerEdible) ((Player) self.owner.owner).grasps[num2].grabbed).Edible)
                            {
                                num = num2;
                            }

                            num2++;
                        }

                        if (num == self.limbNumber)
                        {
                            self.relativeHuntPos *=
                                Custom.LerpMap(((Player) self.owner.owner).eatCounter, 40f, 20f, 0.9f, 0.7f);
                            self.relativeHuntPos.y +=
                                Custom.LerpMap(((Player) self.owner.owner).eatCounter, 40f, 20f, 2f, 4f);
                            self.relativeHuntPos.x *=
                                Custom.LerpMap(((Player) self.owner.owner).eatCounter, 40f, 20f, 1f, 1.2f);
                        }
                    }

                    if ((((Player) self.owner.owner).swallowAndRegurgitateCounter > 10 &&
                         ((Player) self.owner.owner).objectInStomach == null) ||
                        ((Player) self.owner.owner).craftingObject)
                    {
                        int num3 = -1;
                        int num4 = 0;
                        while (num3 < 0 && num4 < 2)
                        {
                            if (((Player) self.owner.owner).grasps[num4] != null &&
                                ((Player) self.owner.owner).CanBeSwallowed(((Player) self.owner.owner).grasps[num4]
                                    .grabbed))
                            {
                                num3 = num4;
                            }

                            num4++;
                        }

                        if (num3 == self.limbNumber || ((Player) self.owner.owner).craftingObject)
                        {
                            float num5 = Mathf.InverseLerp(10f, 90f,
                                ((Player) self.owner.owner).swallowAndRegurgitateCounter);
                            if (num5 < 0.5f)
                            {
                                self.relativeHuntPos *= Mathf.Lerp(0.9f, 0.7f, num5 * 2f);
                                self.relativeHuntPos.y += Mathf.Lerp(2f, 4f, num5 * 2f);
                                self.relativeHuntPos.x *= Mathf.Lerp(1f, 1.2f, num5 * 2f);
                            }
                            else
                            {
                                ((PlayerGraphics) self.owner).blink = 5;
                                self.relativeHuntPos = new Vector2(0f, -4f) +
                                                       Custom.RNV() * 2f * Random.value *
                                                       Mathf.InverseLerp(0.5f, 1f, num5);
                                ((PlayerGraphics) self.owner).head.vel +=
                                    Custom.RNV() * 2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                                self.owner.owner.bodyChunks[0].vel +=
                                    Custom.RNV() * 0.2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                            }
                        }
                    }

                    self.relativeHuntPos.x *=
                        1f - Mathf.Sin(((Player) self.owner.owner).switchHandsProcess * 3.1415927f);
                    if (((PlayerGraphics) self.owner).spearDir != 0f &&
                        ((Player) self.owner.owner).bodyMode == Player.BodyModeIndex.Stand)
                    {
                        Vector2 b = Custom.DegToVec(180f + ((self.limbNumber == 0) ? -1f : 1f) * 8f +
                                                    ((Player) self.owner.owner).input[0].x * 4f) * 12f;
                        b.y += Mathf.Sin(((Player) self.owner.owner).animationFrame / 6f * 2f * 3.1415927f) * 2f;
                        b.x -= Mathf.Cos(
                            (((Player) self.owner.owner).animationFrame +
                             (((Player) self.owner.owner).leftFoot ? 0 : 6)) /
                            12f * 2f * 3.1415927f) * 4f * ((Player) self.owner.owner).input[0].x;
                        b.x += ((Player) self.owner.owner).input[0].x * 2f;
                        self.relativeHuntPos = Vector2.Lerp(self.relativeHuntPos, b,
                            Mathf.Abs(((PlayerGraphics) self.owner).spearDir));
                        if (((Player) self.owner.owner).grasps[self.limbNumber].grabbed is Weapon)
                        {
                            ((Weapon) ((Player) self.owner.owner).grasps[self.limbNumber].grabbed).ChangeOverlap(
                                (((PlayerGraphics) self.owner).spearDir > -0.4f && self.limbNumber == 0) ||
                                (((PlayerGraphics) self.owner).spearDir < 0.4f && self.limbNumber == 1));
                        }
                    }

                    switch (((Creature) self.owner.owner).grasps[self.limbNumber].grabbed)
                    {
                        case Fly when !((Fly) ((Creature) self.owner.owner).grasps[self.limbNumber].grabbed).dead:
                            self.huntSpeed = Random.value * 5f;
                            self.quickness = Random.value * 0.3f;
                            self.vel += Custom.DegToVec(Random.value * 360f) * Random.value * Random.value *
                                        (Custom.DistLess(self.absoluteHuntPos, self.pos, 7f) ? 4f : 1.5f);
                            self.pos += Custom.DegToVec(Random.value * 360f) * Random.value * 4f;
                            ((PlayerGraphics) self.owner).NudgeDrawPosition(0,
                                Custom.DirVec(((Creature) self.owner.owner).mainBodyChunk.pos, self.pos) * 3f *
                                Random.value);
                            ((PlayerGraphics) self.owner).head.vel +=
                                Custom.DirVec(((Creature) self.owner.owner).mainBodyChunk.pos, self.pos) * 2f *
                                Random.value;
                            break;
                        case VultureMask:
                            self.relativeHuntPos *=
                                1f - ((VultureMask) ((Creature) self.owner.owner).grasps[self.limbNumber].grabbed)
                                .donned;
                            break;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        // This part of the code only runs if the code made it past the pup check
        self.retractCounter -= 10;
        if (self.retractCounter < 0)
        {
            self.retractCounter = 0;
        }
    }
    
    
    public static bool Player_isSlugpup(Player __instance, ref bool __result)
    {
        // Use the base method if in cosmetic mode
        if (Plugin.MakeChanges(__instance)) return true;
        // This actually is an npc
        __result = true;

        // Don't use the base method
        return false;
    }
    #endregion
}
