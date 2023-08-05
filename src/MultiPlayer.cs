using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Pupify;

public static class MultiPlayer
{
    public static GameSession Session;
    private static List<SlugcatStats> playerStats = new List<SlugcatStats>();
    public static int startingIncrement = 0;
    public static int currentIndex = 0;
    

    public static void Init()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.StoryGameSession.CreateJollySlugStats += StoryGameSession_CreateJollySlugStats;
    }

    
    public static SlugcatStats GetCurrentPlayer()
    {
        //return playerStats[currentIndex];
        if (ModManager.CoopAvailable)
        {
            SlugcatStats value = startingIncrement >= 2 ? playerStats[currentIndex] : Plugin.currentSlugcat;

            if (startingIncrement < 2)
            {
                startingIncrement++;
                if (startingIncrement == 2)
                {
                    value = null;
                }
            }
            else
            {
                currentIndex++;
            }
            
            // Lets count back up from slugpups : Session won't be instantiated on the main menu
            if (currentIndex >= Session?.Players.Count && Session?.Players.Count != 0)
            {
                currentIndex = 0;
                Plugin.playerCreated = true;
            }
            
            return value;
        }
        else
        {
            return Plugin.currentSlugcat;
        }
    }

    
    // Lets stats be private while still adding players to it
    public static void AddPlayer(SlugcatStats self)
    {
        // This will run several more times when slugpup puppets are created, this 
        // prevents those from taking up memory in this class
        if (playerStats.Count != Session.Players.Count)
        {
            playerStats.Add(self);
        }
    }


    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        Session = self;
        orig(self, saveStateNumber, game);
    }
    
    
    private static void StoryGameSession_CreateJollySlugStats(ILContext il)
    {
        ILCursor c = new ILCursor(il);


        // Hard coded value at the end of the loop, when it's incrementing to begin it's next iteration
        var loopStart = c.Body.Instructions.First(instr => instr.Offset == 0x125);
            
        // Find the third to last assignment in the method : hard coded because it's definitive
        if (c.TryGotoNext(MoveType.After,
                instr => instr.MatchLdarg(0),
                instr => instr.MatchLdfld<StoryGameSession>("characterStatsJollyplayer"),
                instr => instr.MatchLdloc(0),
                instr => instr.MatchLdfld<PlayerState>("playerNumber"),
                instr => instr.MatchLdelemRef(),
                instr => instr.MatchLdloc(1),
                instr => instr.MatchLdfld<SlugcatStats>("foodToHibernate"),
                instr => instr.MatchStfld<SlugcatStats>("foodToHibernate")))
        {
            // Inserts a continue statement before the assignments that changes stats run
            // essentially making it so that Pupify stats supersede the multiplayer ones
            c.Emit(OpCodes.Br, loopStart);
        }
    }
}