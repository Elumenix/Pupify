using System.Collections.Generic;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using On.Menu;
using UnityEngine;

namespace Pupify;

public class Options : OptionInterface
{
    private readonly ManualLogSource Logger;

    public Options(Plugin modInstance, ManualLogSource loggerSource)
    {
        Logger = loggerSource;
        onlyCosmetic = config.Bind("onlyCosmetic", false);
        foodOption = config.Bind("foodOption", 0);
        statsOption = config.Bind("statsOption", 0);
        bothHands = config.Bind("bothHands", false);
        
    }

    public readonly Configurable<bool> onlyCosmetic;
    public readonly Configurable<int> foodOption;
    public readonly Configurable<int> statsOption;
    public readonly Configurable<bool> bothHands;
    

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
        foodGroup.SetButtons(new OpRadioButton[] {new(100f, 420f), new(300f, 420f), new(500f, 420f)});
        statGroup = new OpRadioButtonGroup(statsOption);
        statGroup.SetButtons(new OpRadioButton[] {new(100f, 320f), new(300f, 320f), new(500f, 320f)});

        
        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Options", true),
            new OpLabel(10f, 520f, "Treat Pups as Cosmetic: "),
            new OpCheckBox(onlyCosmetic, 150f, 520f),
            
            new OpLabel(10f, 450f, "Food Values:", true),
            new OpLabel(50f, 420f, "Scaled"),
            foodGroup, 
            foodGroup.buttons[0],
            new OpLabel(225f, 420f, "Base Game"),
            foodGroup.buttons[1],
            new OpLabel(440f, 420f, "Slugpup"),
            foodGroup.buttons[2],
            statGroup,
            statGroup.buttons[0],
            statGroup.buttons[1],
            statGroup.buttons[2],
            new OpLabel(10f, 350f, "Stat Values:", true),
            new OpLabel(50f, 320f, "Scaled"),
            new OpLabel(225f, 320f, "Base Game"),
            new OpLabel(440f, 320f, "Slugpup"),
            
            
            new OpLabel(10, 400, "Great! You don't need any other options.", true){color = new Color(0.85f,0.2f,0.4f)}
        };
        opTab.AddItems(UIArrPlayerOptions);

        UIArrPlayerModify = new UIelement[]
        {
            new OpLabel(10f, 570f, "Modify", true),
				new OpRect(new Vector2(0f, 248f), new Vector2(257f, 305f), 0.3f),
				new OpLabel(15f, 520f, "Stomach Items", false),
				new OpLabel(15f, 460f, "Carnivorous Diet", false),
				
				new OpLabel(15f, 400f, "Maul Enemies", false),
				
				
				new OpRect(new Vector2(282f, 365f), new Vector2(260f, 188f), 0.3f),
				new OpLabel(295f, 510f, "Slugpups generally lose access \nto major abilities. \nThey can be re-enabled here.", false),


				
				
				new OpRect(new Vector2(282f, 223f), new Vector2(195f, 105f), 0.3f),
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
            UIArrPlayerOptions[UIArrPlayerOptions.Length - 1].Show();
        }
        else
        {
            for (int i = 3; i < UIArrPlayerOptions.Length; i++)
            {
                UIArrPlayerOptions[i].Show();
            }
            UIArrPlayerOptions[UIArrPlayerOptions.Length - 1].Hide();
        }
    }
}
