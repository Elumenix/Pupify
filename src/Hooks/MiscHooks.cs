using System;
using System.Collections.Generic;
using HUD;
using Menu;
using RWCustom;
using UnityEngine;
using MenuLabel = Menu.MenuLabel;
using MenuObject = Menu.MenuObject;
using ModdingMenu = On.Menu.ModdingMenu;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using SlugcatSelectMenu = On.Menu.SlugcatSelectMenu;

namespace Pupify.Hooks;

public static class MiscHooks
{
    public static void Init()
    {
        ModdingMenu.Singal += ModdingMenu_Singal;
        On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
        SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
    }

    
    // Yes, the word signal is spelled incorrectly in the internal code
    private static void ModdingMenu_Singal(ModdingMenu.orig_Singal orig, Menu.ModdingMenu self, MenuObject sender, string message)
    {
        // Because my options menu uses FSprites and I don't have access to the modding menu from that class,
        // I need a way to turn them off after leaving or switching between remix menus, otherwise they stay on screen
        orig(self, sender, message);
        Options.TurnOffFoodBar();
    }


    private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig,
        ProcessManager self, ProcessManager.ProcessID ID)
    {
        // Multiplayer menu check is so that players can switch between characters in the arena 
        // otherwise the same recolored character will be used if the player goes back to arena with a different character

        if (!Plugin.options.onlyCosmetic.Value &&
            ((ID == ProcessManager.ProcessID.Game && self.oldProcess.ID != ProcessManager.ProcessID.MultiplayerMenu) ||
             ID == ProcessManager.ProcessID.MultiplayerMenu))
        {
            // Allow game to set up starving values : Should not be nullified if switching into arena mode, or 
            // else the stats reference will be nullified and the game will crash
            Plugin.currentSlugcat = null;
            Plugin.playersCreated = false;
            MultiPlayer.Session = null;
            MultiPlayer.startingIncrement = 0;
            MultiPlayer.currentIndex = 0;
            MultiPlayer.ClearPlayers();
        }

        orig(self, ID);
    }
    
    
    private static void SlugcatPageContinue_ctor(SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig,
        Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex,
        SlugcatStats.Name slugcatNumber)
    {
        if (Plugin.options.onlyCosmetic.Value)
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
            if (Plugin.options.foodOption.Value == 0) // Calculated values
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
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && Plugin.options.letEatMeat.Value &&
                    self.saveGameData.cycle == 0)
                {
                    self.saveGameData.food = 4;
                }
                
                self.hud.AddPart(new FoodMeter(self.hud, maxFood, foodToHibernate));
            }
            else if (Plugin.options.foodOption.Value == 1) // orig values
            {
                // original method Default food stats

                self.saveGameData.food = Custom.IntClamp(self.saveGameData.food, 0,
                    SlugcatStats.SlugcatFoodMeter(slugcatNumber).y);
                
                // Specific edge case that result from artificer being allowed a cycle 0 save
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && Plugin.options.letEatMeat.Value &&
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
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && Plugin.options.letEatMeat.Value &&
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
            self.regionLabel = new MenuLabel(menu, self, text, new Vector2(-1000f, self.imagePos.y - 249f), 
                new Vector2(200f, 30f), bigText: true)
            {
                label =
                {
                    alignment = FLabelAlignment.Center
                }
            };
            self.subObjects.Add(self.regionLabel);
        }
    }
}
