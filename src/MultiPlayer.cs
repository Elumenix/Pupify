using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Pupify;

public static class MultiPlayer
{
    //private static Dictionary<SlugcatStats, bool> allPlayers;
    public static GameSession Session;
    public static bool firstThrough;
    //public static float[] savedStats = new float[3];

    public static void Init()
    {
        firstThrough = false;
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.StoryGameSession.CreateJollySlugStats += StoryGameSession_CreateJollySlugStats;
    }


    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        Session = self;
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