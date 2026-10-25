# Third Party assets credits

EndGameCredits shows a dedicated Third Party assets page after the Made with
Unity logo and thank-you message, before returning to MainMenu. Its content and
layout are authored in Assets/UI Toolkit/EndGameCredits/EndGameCredits.uxml and
EndGameCredits.uss. The font attribution was moved out of the team/thanks monitor
into this page; the main menu's existing font credit is unchanged.

The page credits:

- Deep-sea Creatures by PixelCampGames:
  https://pixelcampgames.itch.io/deep-sea-creatures
  The author's asset page lists CC BY-SA 4.0. The credit includes the source and
  https://creativecommons.org/licenses/by-sa/4.0/.
- m5x7 and m6x11 fonts by Daniel Linssen (managore), with both source addresses.
  The existing font notices remain in Assets/Art/UI/Gameplay/Fonts.

EndGameCreditsController exposes Third Party Fade Duration (1 second) and
Third Party Hold Duration (8 seconds). Confirm, Back or the Main Menu button can
continue after the page opens. The previous Main Menu action still completes
the Unity splash and this final attribution page before loading the menu.
