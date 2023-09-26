using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.Menu;
using Pupify.Hooks;
using UnityEngine;


namespace Pupify;

public static class MultiPlayer
{
    public static GameSession Session;
    public static readonly List<SlugcatStats> playerStats = new();
    public static List<bool> makePup = new();
    public static int startingIncrement;
    public static int currentIndex;
    public static int playerToLoad;
    public static bool onlyPupsLeft;
    public static int numAccessed;
    private static bool rightHeld;
    public static bool reorganized;
    

    public static void Init()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        On.ArenaGameSession.ctor += ArenaGameSession_ctor;
        MultiplayerMenu.Update += MultiplayerMenu_Update;
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

            // verify that the current index is marked as a player that should be a slugpup
            // current index can be wrong now that players are allowed to play as adults
            if (Session is StoryGameSession gameSession && ModManager.CoopAvailable)
            {
                while (Plugin.playersCreated && startingIncrement > 1 && !gameSession.game.rainWorld.options.jollyPlayerOptionsArray[currentIndex].isPup)
                {
                    currentIndex++;
                } 
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
                if (Plugin.playersCreated)
                {
                    onlyPupsLeft = true;
                }
                
                currentIndex = 0;
                Plugin.playersCreated = true;
            }
            
            return value;
        }
        else
        {
            // Single Player story mode
            return Plugin.currentSlugcat;
        }
    }


    public static SlugcatStats GetSpecificPlayer(int index)
    {
        // Edge case where only players 1 and 3 are active or similar (players are skipped so indexes don't line up)
        if (!reorganized)
        {
            while (index >= playerStats.Count)
            {
                playerStats.Add(null);
            }

            // This loop puts all players in an position that matches their ID
            for (int i = 0; i <= index; i++)
            {
                switch (Session)
                {
                    // first half handles competitive arena, second half handles sandbox 
                    case CompetitiveGameSession compSession when playerStats[i] != compSession.characterStats_Mplayer[i]:
                        playerStats[i] = compSession.characterStats_Mplayer[i];
                        break;
                    case SandboxGameSession session when playerStats[i] != session.characterStats_Mplayer[i]:
                        playerStats[i] = session.characterStats_Mplayer[i];
                        break;
                }
            }

            reorganized = true;
        }
        
        numAccessed++;
        return playerStats[index];
    }

    
    // Lets stats be private while still adding players to it
    public static void AddPlayer(SlugcatStats self)
    {
        // This will run several more times when slugpup puppets are created, this 
        // prevents those from taking up memory in this class
        if (playerStats.Count != Session.Players.Count && Session is not ArenaGameSession ||
            Session is ArenaGameSession session && playerStats.Count < session.arenaSitting.players.Count)
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
    
    
     private static void MultiplayerMenu_Update(MultiplayerMenu.orig_Update orig, Menu.MultiplayerMenu self)
    {
        orig(self);
        
        for (int i = 0; i < self.playerJoinButtons?.Length; i++)
        {
            // Because I want to scale for more than 4 players, I add new ones as they come
            if (makePup.Count < i + 1)
            {
                makePup.Add(false);    
            }

            if ((self.playerJoinButtons[i].Selected || self.playerClassButtons[i].Selected) && rightHeld &&
                !Input.GetKey(KeyCode.Mouse1) && !makePup[i])
            {
                // Colors boxes red
                if (self.playerJoinButtons[i].Joined)
                {
                    self.playerJoinButtons[i].roundedRect.borderColor = new HSLColor(0, 1, .5f);
                }
                else
                {
                    self.playerJoinButtons[i].roundedRect.borderColor = new HSLColor(.0055f, 1, .2239f);
                }
                
                self.playerClassButtons[i].roundedRect.borderColor = new HSLColor(0, 1, .5f);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                makePup[i] = true;
            }
            else if ((self.playerJoinButtons[i].Selected || self.playerClassButtons[i].Selected) && rightHeld &&
                     !Input.GetKey(KeyCode.Mouse1) && makePup[i])
            {
                // Colors boxes white
                self.playerJoinButtons[i].roundedRect.borderColor = null;
                self.playerClassButtons[i].roundedRect.borderColor = null;
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                makePup[i] = false;
            }
            else if (makePup[i]) // Make sure colors stay updated
            {
                // Colors boxes red
                if (self.playerJoinButtons[i].Joined)
                {
                    self.playerJoinButtons[i].roundedRect.borderColor = new HSLColor(0, 1, .5f);
                }
                else
                {
                    self.playerJoinButtons[i].roundedRect.borderColor = new HSLColor(.0055f, 1, .2239f);
                }
                self.playerClassButtons[i].roundedRect.borderColor = new HSLColor(0, 1, .5f);
            }
        }

        rightHeld = Input.GetKey(KeyCode.Mouse1);

        if (MenuHooks.challengePupButton == null) return;
        
        // Theres no sound when clicked for some reason so I do it myself
        if (MenuHooks.challengePupButton.isToggled != MenuHooks.arenaPreviousState)
        {
            self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

            if (makePup.Count > 0)
            {
                makePup[0] = MenuHooks.challengePupButton.isToggled;
            }
        }

        MenuHooks.arenaPreviousState = MenuHooks.challengePupButton.isToggled;
    }
    
    
    private static void StoryGameSession_CreateJollySlugStats(ILContext il)
    {
        ILCursor c = new ILCursor(il);


        // Hard coded value at the end of the loop, when it's incrementing to begin it's next iteration
        var loopStart = c.Body.Instructions.First(instr => instr.Offset == 0x125);
            
        // Find the third to last assignment in the method : hard coded because it's definitive
        if (c.TryGotoNext(MoveType.Before,
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