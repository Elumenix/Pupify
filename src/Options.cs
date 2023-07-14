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
        onlyCosmetic = this.config.Bind<bool>("onlyCosmetic", false);
        foodOption = this.config.Bind<int>("foodOption", 0);
    }

    public readonly Configurable<bool> onlyCosmetic;
    public readonly Configurable<int> foodOption;
    private UIelement[] UIArrPlayerOptions;
    private OpRadioButtonGroup foodGroup;
    
    
    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            opTab
        };

        // I would have loved any sort of documentation to figure this out
        foodGroup = new OpRadioButtonGroup(foodOption);
        foodGroup.SetButtons(new OpRadioButton[] {new(100f, 420f), new(300f, 420f), new(500f, 420f)});
        
        
        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Options", true),
            new OpLabel(10f, 520f, "Treat Pups as Cosmetic: "),
            new OpCheckBox(onlyCosmetic, 150f, 520f),
            
            new OpLabel(10f, 450f, "Food Values:", true),
            new OpLabel(50f, 420f, "Scaled"),
            foodGroup, // Config button shows up on this one but options still don't apply
            foodGroup.buttons[0],
            new OpLabel(225, 420, "Base Game"),
            foodGroup.buttons[1],
            new OpLabel(440, 420, "Slugpup"),
            foodGroup.buttons[2],


            new OpLabel(10, 400, "Great! You don't need all these other options now.", true){color = new Color(0.85f,0.2f,0.4f)}
        };
        opTab.AddItems(UIArrPlayerOptions);
    }

    public override void Update()
    {
        Debug.Log(foodOption.Value);
        
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
