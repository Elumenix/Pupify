using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace Pupify;

public class Options : OptionInterface
{
    private readonly ManualLogSource Logger;

    public Options(Plugin modInstance, ManualLogSource loggerSource)
    {
        Logger = loggerSource;
        onlyCosmetic = this.config.Bind<bool>("onlyCosmetic", false);
        //PlayerSpeed = this.config.Bind<float>("PlayerSpeed", 1f, new ConfigAcceptableRange<float>(0f, 100f));
    }

    //public readonly Configurable<float> PlayerSpeed;
    public readonly Configurable<bool> onlyCosmetic;
    private UIelement[] UIArrPlayerOptions;
    
    
    public override void Initialize()
    {
        var opTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            opTab
        };

        //new OpLabel(10f, 520f, "Player run speed factor"),
        //new OpUpdown(PlayerSpeed, new Vector2(10f,490f), 100f, 1),
            
        //new OpLabel(10f, 460f, "Gotta go fast!", false){ color = new Color(0.2f, 0.5f, 0.8f) },
        
        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Options", true),
            new OpLabel(10f, 520f, "Treat Pups as Cosmetic: "),
            new OpCheckBox(onlyCosmetic, 150f, 520f),
            
            new OpLabel(10, 400, "Great! You don't need all these other options now."){color = new Color(0.85f,0.2f,0.4f)}
        };
        opTab.AddItems(UIArrPlayerOptions);
    }

    public override void Update()
    {
        if (((OpCheckBox)UIArrPlayerOptions[2]).GetValueBool())
        {
            UIArrPlayerOptions[3].Show();
        }
        else
        {
            UIArrPlayerOptions[3].Hide();
        }
    }
}
