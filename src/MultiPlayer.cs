using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Pupify;

public static class MultiPlayer
{
    public static GameSession Session;
    private static List<SlugcatStats> playerStats = new();
    public static int startingIncrement;
    public static int currentIndex;
    public static int playerToLoad;
    

    public static void Init()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        On.ArenaGameSession.ctor += ArenaGameSession_ctor;
        IL.StoryGameSession.CreateJollySlugStats += StoryGameSession_CreateJollySlugStats;
    }


    #region Stats Handling
    public static SlugcatStats GetCurrentPlayer()
    {
        if (ModManager.CoopAvailable || Session is not StoryGameSession)
        {
            int check;
            if (Session != null)
            {
                // A number of false character creations are made depending on the mode
                check = Session is not StoryGameSession ? 1 : 2;
            }
            else
            {
                check = 2;
            }

            SlugcatStats value = startingIncrement >= check ? playerStats[currentIndex] : Plugin.currentSlugcat;

            if (startingIncrement < check)
            {
                startingIncrement++;
                if (startingIncrement == check)
                {
                    value = null;
                }
            }
            else
            {
                currentIndex++;
            }
            
            // Lets count back up from slugpups : Session won't be instantiated on the main menu
            // First set of checks are for story session, second set are for arena session
            if ((Session is not ArenaGameSession && currentIndex >= Session?.Players.Count &&
                 Session?.Players.Count != 0) || (Session is ArenaGameSession session &&
                                                  currentIndex >= session.arenaSitting?.players.Count &&
                                                  session.arenaSitting?.players.Count != 0))
            {
                currentIndex = 0;
                Plugin.playersCreated = true;
            }
            
            return value;
        }
        else
        {
            // Sandbox/Challenge pathway
            if (!Session.game.IsStorySession)
            {
                startingIncrement++;
            }
            
            // Also Single Player story mode
            return Plugin.currentSlugcat;
        }
    }


    public static SlugcatStats GetSpecificPlayer(int index)
    {
        return playerStats[index];
    }

    
    // Lets stats be private while still adding players to it
    public static void AddPlayer(SlugcatStats self)
    {
        // This will run several more times when slugpup puppets are created, this 
        // prevents those from taking up memory in this class
        if (playerStats.Count != Session.Players.Count && Session is not ArenaGameSession ||
            Session is ArenaGameSession session && playerStats.Count != session.arenaSitting.players.Count)
        {
            playerStats.Add(self);
        }
    }

    
    // Allows for the next player creation process to initiate without problems
    public static void ClearPlayers()
    {
        playerStats.Clear();
    }
    #endregion


    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self,
        SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        Session = self; // Only used to keep track of the number of players in the game
        orig(self, saveStateNumber, game);
    }


    private static void ArenaGameSession_ctor(On.ArenaGameSession.orig_ctor orig, ArenaGameSession self,
        RainWorldGame game)
    {
        Session = self; // Only used to keep track of the number of players in the game
        orig(self, game);
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