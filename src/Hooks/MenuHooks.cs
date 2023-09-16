using System;
using System.Collections.Generic;
using System.Linq;
using HUD;
using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using UnityEngine;
using ArenaSettingsInterface = On.Menu.ArenaSettingsInterface;
using MenuLabel = Menu.MenuLabel;
using MenuObject = Menu.MenuObject;
using ModdingMenu = On.Menu.ModdingMenu;
using MoreSlugcatsEnums = MoreSlugcats.MoreSlugcatsEnums;
using MultiplayerMenu = On.Menu.MultiplayerMenu;
using SlugcatSelectMenu = On.Menu.SlugcatSelectMenu;

namespace Pupify.Hooks;

public static class MenuHooks
{
    public static SymbolButtonTogglePupButton pupButton;
    private static bool previousState;
    public static bool arenaPreviousState;
    public static SymbolButtonTogglePupButton challengePupButton;
    
    public static void Init()
    {
        // The multiplayer menu is handled in Multiplayer.cs due to easier access to variables
        ArenaSettingsInterface.ctor += ArenaSettingsInterface_ctor;
        MultiplayerMenu.InitiateGameTypeSpecificButtons += MultiplayerMenu_InitiateGameTypeSpecificButtons;
        SlugcatSelectMenu.ctor += SlugcatSelectMenu_ctor;
        SlugcatSelectMenu.Update += SlugcatSelectMenu_Update;
        ModdingMenu.Singal += ModdingMenu_Singal;
        SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
    }


    private static void ArenaSettingsInterface_ctor(ArenaSettingsInterface.orig_ctor orig,
        Menu.ArenaSettingsInterface self, Menu.Menu menu, MenuObject owner)
    {
        orig(self, menu, owner);

        MenuLabel tipText = new MenuLabel(menu, owner, "Right click on their profile to Pupify a slugcat:",
            new Vector2(746f, 590f), new Vector2(350f, 20f), true)
        {
            label =
            {
                color = Color.red
            }
        };
        self.subObjects.Add(tipText);
    }


    private static void MultiplayerMenu_InitiateGameTypeSpecificButtons(
        MultiplayerMenu.orig_InitiateGameTypeSpecificButtons orig, Menu.MultiplayerMenu self)
    {
        orig(self);

        if (self.currentGameType == MoreSlugcatsEnums.GameTypeID.Challenge)
        {
            if (challengePupButton == null)
            {
                // Start by copying whatever the player decided on the arena selection screen
                bool startingValue = MultiPlayer.makePup.Count >= 1 && MultiPlayer.makePup[0];

                if (MultiPlayer.makePup.Count == 0)
                {
                    // If they somehow started here, try copying pupButtons value instead
                    if (pupButton != null)
                    {
                        startingValue = pupButton.isToggled;
                    }
                    
                    // Give at least one value, so the player can actually play challenges
                    MultiPlayer.makePup.Add(startingValue);
                }

                if (!ModManager.JollyCoop)
                {
                    challengePupButton = new SymbolButtonTogglePupButton(self, self.backObject, "toggle_pup_0",
                        new Vector2(920f, 500f),
                        new Vector2(45f, 45f), "atlases/pup_on", "atlases/pup_off", startingValue);
                }
                else
                {
                    challengePupButton = new SymbolButtonTogglePupButton(self, self.backObject, "toggle_pup_0",
                        new Vector2(920f, 500f),
                        new Vector2(45f, 45f), "pup_on", "pup_off", startingValue);
                }


                self.backObject.subObjects.Add(challengePupButton);
                arenaPreviousState = challengePupButton.isToggled;
            }
            else
            {
                bool toggleValue = MultiPlayer.makePup.Count >= 1 && MultiPlayer.makePup[0];
                if (challengePupButton.isToggled != toggleValue)
                {
                    challengePupButton.Toggle();
                }
                challengePupButton.pos.x = 920f;
            }
        }
        else
        {
            if (challengePupButton != null)
            {
                challengePupButton.pos.x = 3000f;
            }
        }
    }

    
    private static void SlugcatSelectMenu_ctor(SlugcatSelectMenu.orig_ctor orig, Menu.SlugcatSelectMenu self,
        ProcessManager manager)
    {
        orig(self, manager);
        bool startingValue = pupButton is null || pupButton.isToggled;
        if (!ModManager.JollyCoop)
        {
            pupButton = new SymbolButtonTogglePupButton(self, self.backObject, "toggle_pup_0", new Vector2(890f, -10f),
                new Vector2(45f, 45f), "atlases/pup_on", "atlases/pup_off", startingValue);
        }
        else
        {
            pupButton = new SymbolButtonTogglePupButton(self, self.backObject, "toggle_pup_0", new Vector2(890f, -10f),
                new Vector2(45f, 45f), "pup_on", "pup_off", startingValue);
        }

        self.backObject.subObjects.Add(pupButton);
        previousState = pupButton.isToggled;
    }
    
    
    private static void SlugcatSelectMenu_Update(SlugcatSelectMenu.orig_Update orig, Menu.SlugcatSelectMenu self)
    {
        orig(self);

        if (ModManager.JollyCoop && self.slugcatPageIndex is 0 or 1 or 2)
        {
            pupButton.pos.x = -6000f;
            pupButton.pos.y = 3000f;
        }
        else
        {
            pupButton.pos.x = 890f;
            pupButton.pos.y = -10f;
        }
        
        // Eyes default to either opaque or white (I haven't checked) so I need to make them visible 
        pupButton.faceSymbol.sprite.color = Color.black;

        // Theres no sound when clicked for some reason so I do it myself
        if (pupButton.isToggled != previousState)
        {
            self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

            // This will restructure the food bars on all pages
            for (int i = 0; i < self.slugcatColorOrder.Count; i++)
            {
                if (self.saveGameData[self.slugcatColorOrder[i]] != null)
                {
                    self.slugcatPages[i].RemoveSprites();
                    self.slugcatPages[i] =
                        new Menu.SlugcatSelectMenu.SlugcatPageContinue(self, null, i + 1, self.slugcatColorOrder[i]);
                }
                else
                {
                    self.slugcatPages[i].RemoveSprites();
                    self.slugcatPages[i] =
                        new Menu.SlugcatSelectMenu.SlugcatPageNewGame(self, null, i + 1, self.slugcatColorOrder[i]);
                }

                self.pages[i + 1] = self.slugcatPages[i];
            }
            
            // Make sure the start button doesn't clip behind the slugcat image, this was barely noticeable anyway
            self.pages[0].RemoveSubObject(self.startButton);
            self.startButton.RemoveSprites();
            self.startButton = new HoldButton(self, self.pages[0], "", "START", new Vector2(683f, 85f), 40f);
            self.pages[0].subObjects.Add(self.startButton);
            self.UpdateStartButtonText();
        }

        previousState = pupButton.isToggled;
    }
    

    // Yes, the word signal is spelled incorrectly in the internal code
    // ReSharper disable once IdentifierTypo
    private static void ModdingMenu_Singal(ModdingMenu.orig_Singal orig, Menu.ModdingMenu self, MenuObject sender, string message)
    {
        // Because my options menu uses FSprites and I don't have access to the modding menu from that class,
        // I need a way to turn them off after leaving or switching between remix menus, otherwise they stay on screen
        orig(self, sender, message);
        Options.TurnOffFoodBar();
    }
    
    
    private static void SlugcatPageContinue_ctor(SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig,
        Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex,
        SlugcatStats.Name slugcatNumber)
    {
        if (Plugin.options.onlyCosmetic.Value ||
            pupButton is not null && !pupButton.isToggled && !Plugin.options.overrideFood.Value)
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

            if (Plugin.options.overrideFood.Value) // Food Override
            {
                // Specific edge case that result from artificer being allowed a cycle 0 save
                if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer &&
                    Plugin.options.letEatMeat.Value &&
                    self.saveGameData.cycle == 0)
                {

                    self.saveGameData.food = 4 < Mathf.RoundToInt(Plugin.options.maxFood.Value)
                        ? 4
                        : Mathf.RoundToInt(Plugin.options.maxFood.Value);
                }

                self.hud.AddPart(new FoodMeter(self.hud, Mathf.RoundToInt(Plugin.options.maxFood.Value),
                    Mathf.RoundToInt(Plugin.options.foodToHibernate.Value)));
            }
            else // Food Option
            {
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
                    if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer &&
                        Plugin.options.letEatMeat.Value &&
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
                    if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer &&
                        Plugin.options.letEatMeat.Value &&
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
                    if (slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer &&
                        Plugin.options.letEatMeat.Value &&
                        self.saveGameData.cycle == 0)
                    {
                        self.saveGameData.food = 3;
                    }

                    self.hud.AddPart(new FoodMeter(self.hud, 3,
                        2));
                }
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