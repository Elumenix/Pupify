using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace Pupify;

public class Options : OptionInterface
{
    // ReSharper disable once NotAccessedField.Local
    private readonly ManualLogSource Logger;

    // ReSharper disable once UnusedParameter.Local
    public Options(Plugin modInstance, ManualLogSource loggerSource)
    {
        Logger = loggerSource;
        onlyCosmetic = config.Bind("onlyCosmetic", false);
        foodOption = config.Bind("foodOption", 0);
        statsOption = config.Bind("statsOption", 0);
        bothHands = config.Bind("bothHands", false);
        letStomach = config.Bind("letStomach", false);
        letMaul = config.Bind("letMaul", false);
        letEatMeat = config.Bind("letEatMeat", false);
        holdHands = config.Bind("holdHands", false);
        letPickup = config.Bind("letPickup", false);
        overrideFood = config.Bind("overrideFood", false);
        foodToHibernate = config.Bind("foodToHibernate", 2f);
        maxFood = config.Bind("maxFood", 3f);
    }

    public readonly Configurable<bool> onlyCosmetic;
    public readonly Configurable<int> foodOption;
    public readonly Configurable<int> statsOption;
    public readonly Configurable<bool> bothHands;
    public readonly Configurable<bool> letStomach;
    public readonly Configurable<bool> letEatMeat;
    public readonly Configurable<bool> letMaul;
    public readonly Configurable<bool> holdHands;
    public readonly Configurable<bool> letPickup;
    public readonly Configurable<bool> overrideFood;
    public readonly Configurable<float> foodToHibernate;
    public readonly Configurable<float> maxFood;


    private UIelement[] UIArrPlayerOptions;
    private UIelement[] UIArrPlayerModify;
    private OpRadioButtonGroup foodGroup;
    private OpRadioButtonGroup statGroup;
    private static readonly FContainer foodBar = new FContainer();
    private FSprite[] circleSprites;
    private FSprite[] pipSprites;
    private FSprite staff;


    public override void Initialize()
    {
        // Create the parts for the foodBar graphic to appear on screen
        Futile.stage.AddChild(foodBar);

        circleSprites = new FSprite[20];
        pipSprites = new FSprite[20];
        staff = new FSprite("pixel")
        {
            scaleX = 2f,
            scaleY = 34.5f,
            y = 230f
        };

        for (int i = 0; i < 20; i++)
        {
            pipSprites[i] = new FSprite(Futile.atlasManager.GetElementWithName("FoodCircleB"))
            {
                y = 230f
            };

            circleSprites[i] = new FSprite(Futile.atlasManager.GetElementWithName("FoodCircleA"))
            {
                y = 230f
            };

            foodBar.AddChild(pipSprites[i]);
            foodBar.AddChild(circleSprites[i]);
        }
        foodBar.AddChild(staff);

        // Instantiate all other major parts
        var opTab = new OpTab(this, "Options");
        var modifyTab = new OpTab(this, "Modify");
        Tabs = new[]
        {
            opTab,
            modifyTab
        };

        // I would have loved any sort of documentation to figure this out
        foodGroup = new OpRadioButtonGroup(foodOption);
        foodGroup.SetButtons(new OpRadioButton[]
        {
            new(100f, 403f)
            {
                description =
                    "All food meters are scaled down to what might be expected from that slugcat as a pup." +
                    "\nAs the basis of the scaling, survivor has the same food meter as a base slugpup."
            },
            new(300f, 403f) {description = "The food meter for the current slugcat will remain unaltered."},
            new(500f, 403f)
            {
                description =
                    "Use the base slugpup food meter.\nThe player will need their bar at least two thirds full to hibernate."
            }
        });
        statGroup = new OpRadioButtonGroup(statsOption);
        statGroup.SetButtons(new OpRadioButton[]
        {
            new(100f, 303f)
            {
                description =
                    "All stats are scaled down to what might be expected from that slugcat as a pup.\n" +
                    "As the basis of the scaling, survivor has the same stats as an non-randomized base slugpup."
            },
            new(300f, 303f)
            {
                description =
                    "The stats of the current slugcat will remain unaltered.\n" +
                    "Note that, as it isn't a stat, jumping will still be worse. This can only be changed using cosmetic mode."
            },
            new(500f, 303f)
            {
                description =
                    "All slugcats will have the same stats as a a base non-randomized slugpup.\n" +
                    "Non-randomized stats (put very simply) are the middle value of each stat in the range that a " +
                    "slugpup could have for it. Not being randomized is not a penalty."
            }
        });
        
        // OpUpdown has an int constructor that I should be using, however its completely broken and will crash the program
        OpUpdown minFood = new OpUpdown(foodToHibernate, new Vector2(160f,152f), 60f, 0)
        {
            description = "How much must be consumed before hibernation is allowed?",
            _fMax = 20,
            _fMin = 1
        };

        OpUpdown maximumFood = new OpUpdown(maxFood, new Vector2(390f,152f), 60f, 0)
        {
            description = "How much may be consumed in total?",
            _fMax = 20,
            _fMin = 1
        };
        
        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Options", true),
            new OpLabel(10f, 520f, "Cosmetic Mode ")
            {
                description = "There will be no gameplay alterations. You just look like a slugpup."
            },
            new OpCheckBox(onlyCosmetic, 105f, 520f),
            
            new OpLabel(10f, 450f, "Food Values:", true),
            
            new OpRect(new Vector2(30f, 390f), new Vector2(525f, 50f)),
            new OpLabel(50f, 403f, "Scaled"),
            foodGroup, 
            foodGroup.buttons[0],
            new OpLabel(225f, 403f, "Base Game"),
            foodGroup.buttons[1],
            new OpLabel(440f, 403f, "Slugpup"),
            foodGroup.buttons[2],
            
            
            new OpLabel(10f, 350f, "Stat Values:", true),
            
            new OpRect(new Vector2(30f, 290f), new Vector2(525f, 50f)),
            statGroup,
            statGroup.buttons[0],
            statGroup.buttons[1],
            statGroup.buttons[2],
            new OpLabel(50f, 303f, "Scaled"),
            new OpLabel(225f, 303f, "Base Game"),
            new OpLabel(440f, 303f, "Slugpup"),
            
            
            new OpLabel(10f, 200f, "Override Food Values:", true),
            new OpCheckBox(overrideFood, 230f, 200f)
            {
                description = "Recommended for Coop \n Allows you to customize the limits of the black hole in a slugcats stomach." +
                              "\nNote: This will apply even if you are an adult."
            },
            
            minFood,
            maximumFood,
            new OpLabel(50f, 160f, "Hibernation Limit"),
            new OpLabel(310f, 160f, "Meter Limit"),
                
            new OpLabel(10, 400, "Great! You don't need any other options.", true){color = new Color(0.85f,0.2f,0.4f)}
        };
        opTab.AddItems(UIArrPlayerOptions);

        UIArrPlayerModify = new UIelement[]
        {
            new OpLabel(295f, 420f, "All options will only affect slugcats who naturally \nhave access to " +
                                    "these abilities already.\n\nThese will not give other slugcats new abilities."),
            new OpLabel(295f, 420f, "You are currently in cosmetic mode. \n\nNone of these options will " +
                                    "have any effect on \nthe game."){color = new Color(0.85f,0.2f,0.4f)},
            
            new OpLabel(10f, 570f, "Modify", true),
			new OpRect(new Vector2(0f, 223f), new Vector2(257f, 340f)),
			new OpLabel(15f, 500f, "Stomach Items"),
            new OpCheckBox(letStomach, 120f, 500f)
            {
                description = "Access Stomach for storage & crafting. \nWarning: Will trap current item in stomach if turned off."
            },
            
			new OpLabel(15f, 440f, "Carnivorous Diet"),
            new OpCheckBox(letEatMeat, 120f, 440f)
            {
                description = "What kind of child are you? You aren't meant to eat vultures."
            },
			
			new OpLabel(15f, 380f, "Mauling"),
            new OpCheckBox(letMaul, 120f, 380f)
            {
              description  = "This kitty(slug) has claws."
            },
            
            
            new OpLabel(15f, 320f, "Hold Hands"),
            new OpCheckBox(holdHands, 120f, 320f)
            {
                description = "Seek companionship in this harsh world.\nWarning: Looks weird, but worth it for those invested."
            },
            
            new OpLabel(15f, 260f, "Pickup Slugpups"),
            new OpCheckBox(letPickup, 120f, 260f)
            {
                description = "Time for some piggyback rides.\nNote: Requires \"Hold Hands\" to work."
            },
			
			
			new OpRect(new Vector2(282f, 375f), new Vector2(300f, 188f)),
			new OpLabel(295f, 510f, "Slugpups generally lose access to major abilities. \n\n\nThey can be re-enabled here."),

            
			new OpRect(new Vector2(282f, 223f), new Vector2(196f, 105f)),
			new OpCheckBox(bothHands, new Vector2(368f, 260f))
			{
				description = "Unlock the use of your second set of digits."
			},
			new OpLabel(298f, 295f, "Allow Two Hands", true),
        };
        modifyTab.AddItems(UIArrPlayerModify);
    }

    public override void Update()
    {
        // This first part handles showing the food bar that appears at the bottom of the menu
        int totalPips = (int) Mathf.Round(((OpUpdown) UIArrPlayerOptions[24]).valueFloat);
        int cutoff = (int) Mathf.Round(((OpUpdown) UIArrPlayerOptions[23]).valueFloat);
        if (!UIArrPlayerOptions[0].IsInactive && UIArrPlayerOptions[UIArrPlayerOptions.Length - 1].IsInactive)
        {
            foodBar.isVisible = true;
            staff.x = 555 + 30 * cutoff + 300 - 15 * totalPips;
            
            for (int i = 0; i < 20; i++)
            {
                if (i >= totalPips)
                {
                    circleSprites[i].isVisible = false;
                    pipSprites[i].isVisible = false;
                }
                else
                {
                    circleSprites[i].isVisible = true;
                    
                    circleSprites[i].x = 570 + 30 * i + 300 - 15 * totalPips;

                    // There should be some extra space on either side of the staff
                    if (i < cutoff)
                    {
                        circleSprites[i].x -= 8;
                    }
                    else
                    {
                        circleSprites[i].x += 8;
                    }

                    if (i < cutoff)
                    {
                        pipSprites[i].isVisible = true;
                        pipSprites[i].x = 570 + 30 * i + 300 - 15 * totalPips - 8;
                    }
                    else
                    {
                        pipSprites[i].isVisible = false;
                    }
                }
            }
        }
        else
        {
            foodBar.isVisible = false;
        }


        // I think this prevents crashing due to typing, not completely sure on that though
        ((OpUpdown) UIArrPlayerOptions[23]).InScrollBox = false;
        ((OpUpdown) UIArrPlayerOptions[24]).InScrollBox = false;
        
        // Make sure the max isn't smaller than the hibernation value 
        if (((OpUpdown) UIArrPlayerOptions[23]).MouseOver && ((OpUpdown) UIArrPlayerOptions[23]).valueFloat >
            ((OpUpdown) UIArrPlayerOptions[24]).valueFloat)
        {
            ((OpUpdown) UIArrPlayerOptions[24]).SetValueFloat(((OpUpdown) UIArrPlayerOptions[23]).valueFloat);
        }
        else if (((OpUpdown) UIArrPlayerOptions[24]).MouseOver && ((OpUpdown) UIArrPlayerOptions[24]).valueFloat <
                 ((OpUpdown) UIArrPlayerOptions[23]).valueFloat)
        {
            ((OpUpdown) UIArrPlayerOptions[23]).SetValueFloat(((OpUpdown) UIArrPlayerOptions[24]).valueFloat);
        }
        else if (((OpUpdown) UIArrPlayerOptions[23]).valueFloat > ((OpUpdown) UIArrPlayerOptions[24]).valueFloat)
        {
            ((OpUpdown) UIArrPlayerOptions[24]).SetValueFloat(((OpUpdown) UIArrPlayerOptions[23]).valueFloat);
        }
        
        
        // Second part handles display if cosmetic only mode is checked
	    if (((OpCheckBox)UIArrPlayerOptions[2]).GetValueBool())
        {
            for (int i = 3; i < UIArrPlayerOptions.Length; i++)
            {
                UIArrPlayerOptions[i].Hide();
            }
            UIArrPlayerModify[1].Show();
            UIArrPlayerModify[0].Hide();
            UIArrPlayerOptions[UIArrPlayerOptions.Length - 1].Show();
        }
        else
        {
            for (int i = 3; i < UIArrPlayerOptions.Length; i++)
            {
                UIArrPlayerOptions[i].Show();
            }
            UIArrPlayerModify[1].Hide();
            UIArrPlayerModify[0].Show();
            UIArrPlayerOptions[UIArrPlayerOptions.Length - 1].Hide(); 
        }

        // Third part grays out the food options if they are set to be overridden  
        for (int i = 3; i <= 11; i++)
        {
            if (((OpCheckBox)UIArrPlayerOptions[22]).GetValueBool())
            {
                if (UIArrPlayerOptions[i] is UIfocusable iFocusable)
                {
                    iFocusable.greyedOut = true;
                }
            }
            else
            {
                if (UIArrPlayerOptions[i] is UIfocusable iFocusable)
                {
                    iFocusable.greyedOut = false;
                }
            }
        }
    }

    public static void TurnOffFoodBar()
    {
        if (foodBar is {isVisible: true})
        {
            foodBar.isVisible = false;

            // Get rid of all textures so the foodBar doesn't mess up if the remix menu is reopened
            Futile.atlasManager.UnloadAtlas("pixel");
            Futile.atlasManager.UnloadAtlas("FoodCircleA");
            Futile.atlasManager.UnloadAtlas("FoodCircleB");
        }
    }
}
