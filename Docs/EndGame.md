# EndGame

Assets/Scenes/EndGame.unity contains EndGame UI with connected UIDocument and EndGameController. PanelSettings/styles are separate from menu/gameplay, based on 640×360 with two 20-pixel black bands.

## Final report

The centered monitor spans two-thirds of frame width/height (about 427×240 at base resolution), uses black/green communication styling and CRT startup from point to line to full screen. It reports tests completed/planned, subjects used/deceased/recovered and experiment success percentage.

Success measures completed tests rather than survival: ten completed tests are 100% success even with ten deaths, preserving the concept's reveal.

Continue supports click, Enter and controller South (A/Cross) through the scene EventSystem. It receives focus after startup. Confirmation collapses the CRT report and displays the ending image; repeated requests during transitions are ignored.

## Three endings

EndGameOutcomeSelector.Select(int deceasedSubjects):

| Deaths | Ending | Inspector field |
| --- | --- | --- |
| 0 | NoDeaths | No Deaths Ending Image |
| 1 | SingleDeath | Single Death Ending Image |
| 2+ | MultipleDeaths | Multiple Deaths Ending Image |

Ending classification uses the death count: zero, exactly one, or two and above, independently of the total number of tests.

EndGame assigns the supplied artwork: `Assets/Art/UI/EndGame/End Game 0 Morti.JPG` for no deaths, `Assets/Art/UI/EndGame/EndGame 1 morto.JPG` for one death, and `Assets/Art/UI/EndGame/End Game Molte Morti.JPG` for two or more deaths. The former placeholder images remain available but are no longer assigned. Former serialized field names migrate through FormerlySerializedAs. Each ending has a distinct title and subject summary. Main Menu returns without automatically starting a new game.

## Session data

GameSession retains TotalTests, CompletedTests, SubjectsUsed, DeceasedSubjects and RecoveredSubjects across loads. GameManager records once when accepting EndTest, before intermediate screens; state gating prevents duplicates. New Game resets the session.

TestEndEvent.Subject Died defaults to true to preserve existing death causes, including green cheese. Disable it for a surviving-subject outcome. Every completed test counts one used subject; a non-deceased subject counts as recovered. Random result text does not determine deaths.

Opening EndGame directly without results previews ten tests using Preview Deceased Subjects (0–10, initially 5). Continue therefore shows the multiple-deaths artwork. Real session data overrides the preview. The selector needs no external images/services.

## Manual review

No automated tests or Play Mode were run.

1. Open EndGame directly and try preview values 0, 1, 2 and 10; confirm Continue and check selection.
2. Review 640×360 and larger displays: centered/legible report, proportional image and black bands.
3. Check click, Enter, A/Cross, CRT startup/shutdown, repeated-input rejection and menu return.
4. Start a new game, complete tests with Subject Died enabled/disabled, inspect the real report, then confirm New Game resets counts.

Results are not persisted to disk; they belong to the current session, like the universe number.
