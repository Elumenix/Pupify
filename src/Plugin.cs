using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using HUD;
using Menu;
using On.MoreSlugcats;
using RWCustom;
using UnityEngine;
using static MoreSlugcats.SpeedRunTimer;
using MenuLabel = Menu.MenuLabel;
using MenuObject = Menu.MenuObject;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using MouseCursor = On.Menu.MouseCursor;
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
            On.Player.ShortCutColor += PlayerOnShortCutColor;
            SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;

            MachineConnector.SetRegisteredOI("elumenix.pupify", options);
            IsInit = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
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
            self.mouseCursor = new Menu.MouseCursor(menu, self, new Vector2(-100f, -100f));
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
                
                self.hud.AddPart(new FoodMeter(self.hud, maxFood, foodToHibernate));
            }
            else if (options.foodOption.Value == 1) // orig values
            {
                // original method Default food stats

                self.saveGameData.food = Custom.IntClamp(self.saveGameData.food, 0,
                    SlugcatStats.SlugcatFoodMeter(slugcatNumber).y);
                self.hud.AddPart(new FoodMeter(self.hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x,
                    SlugcatStats.SlugcatFoodMeter(slugcatNumber).y));
            }
            else // Pup values
            {
                // Third thing where locked to pup
                self.saveGameData.food = Custom.IntClamp(self.saveGameData.food, 0,
                    1);
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
                    self.foodToHibernate = 2;
                    self.maxFood = 3;
                }
                // Else, don't override the value : Original
            }
            else
            {
                // Don't override the slugpup
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

        // Makes sure that spearmaster doesn't spawn with more food than a pup should have
        // which would cause an out of range error upon the first time eating, strictly on cycle 0
        foreach (var t in self.room.game.Players)
        {
            if ((t.realizedCreature as Player)?.SlugCatClass ==
                MoreSlugcatsEnums.SlugcatStatsName.Spear)
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
