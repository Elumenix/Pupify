﻿using BepInEx.Logging;
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
    }

    public readonly Configurable<bool> onlyCosmetic;
    public readonly Configurable<int> foodOption;
    public readonly Configurable<int> statsOption;
    public readonly Configurable<bool> bothHands;
    public readonly Configurable<bool> letStomach;
    public readonly Configurable<bool> letEatMeat;
    public readonly Configurable<bool> letMaul;


    private UIelement[] UIArrPlayerOptions;
    private UIelement[] UIArrPlayerModify;
    private OpRadioButtonGroup foodGroup;
    private OpRadioButtonGroup statGroup;


    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        var modifyTab = new OpTab(this, "Modify");
        Tabs = new[]
        {
            opTab,
            modifyTab
        };

        // I would have loved any sort of documentation to figure this out
        foodGroup = new OpRadioButtonGroup(foodOption);
        foodGroup.SetButtons(new OpRadioButton[] {new(100f, 403f){description = "All food meters are scaled down to what might be expected from that slugcat as a pup.\nAs the basis of the scaling, survivor has the same food meter as a base slugpup."}, new(300f, 403f){description = "The food meter for the current slugcat will remain unaltered."}, new(500f, 403f){description = "Use the base slugpup food meter.\nThe player will need their bar at least two thirds full to hibernate."}});
        statGroup = new OpRadioButtonGroup(statsOption);
        statGroup.SetButtons(new OpRadioButton[] {new(100f, 303f){description = "All stats are scaled down to what might be expected from that slugcat as a pup.\nAs the basis of the scaling, survivor has the same stats as an non-randomized base slugpup."}, new(300f, 303f){description = "The stats of the current slugcat will remain unaltered.\nNote that, as it isn't a stat, jumping will still be worse. This can only be changed using cosmetic mode."}, new(500f, 303f){description = "All slugcats will have the same stats as a a base non-randomized slugpup.\nNon-randomized stats (put very simply) are the middle value of each stat in the range that a slugpup could have for it. Not being randomized is not a penalty."}});

        
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
			new OpRect(new Vector2(0f, 343f), new Vector2(257f, 220f)),
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
			
			
			new OpRect(new Vector2(282f, 375f), new Vector2(300f, 188f)),
			new OpLabel(295f, 510f, "Slugpups generally lose access to major abilities. \n\n\nThey can be re-enabled here."),

            
			new OpRect(new Vector2(282f, 223f), new Vector2(195f, 105f)),
			new OpCheckBox(bothHands, new Vector2(360f, 260f))
			{
				description = "Unlock the use of your second set of digits."
			},
			new OpLabel(298f, 295f, "Allow Two Hands", true),
        };
        modifyTab.AddItems(UIArrPlayerModify);
    }

    public override void Update()
    {
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
    }
}
