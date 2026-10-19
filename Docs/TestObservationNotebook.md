# Stage observation notebook

The intermediate result screen shows **Stage Passed** and **Outcome: Success**, even when the subject died. The final EndGame report is unchanged. The existing per-outcome result message is retained as a note below the checklist. The ruled paper, red margin and binding holes are editable UI Toolkit styles in GameplayHud.uss; no flattened background image is required. Content can scroll with the mouse, Page Up/Down or the controller's right stick.

Each checklist item corresponds to a TestObservation flag:

| Label | Condition |
| --- | --- |
| Enjoys the food | At least one cheese or green cheese collected this life |
| Gets regular exercise | Hamster wheel used |
| Loves the pool | Entered water |
| Extended stay | Active gameplay time strictly greater than 60 seconds |
| Enjoys gardening | At least one photo collected this life |
| Very curious | Dug |

Food, duration and photos read the existing per-test counters and elapsed timer. PlayerWheelInteraction records `TestObservation.Exercises` when mounting a wheel. Water and digging are prepared for later integration: call `GameSession.RecordObservation(TestObservation.LovesPool)` or `GameSession.RecordObservation(TestObservation.VeryCurious)` when the respective action actually happens. Those flags stay unchecked until recorded. Gardening maps to photos and curiosity maps to digging, following the requested order.

GameSession clears observation flags and per-test counters on reset/new game and when advancing to the next subject. TestResultView.SetObservations can also directly supply a complete set of flags to the view. The existing Continue action and device-aware key hints are preserved.

No compilation or Play Mode checks were run, following project rules. Review readability at 640 × 360, long notes scrolling, checked/unchecked states and reset between lives in Unity.

The checklist and notes occupy a bounded ScrollView, with a permanently reserved vertical scrollbar so overflow cannot repeatedly change text width. Title, outcome, metadata and Continue stay outside it. Checklist rows do not shrink; scrolling is clamped with no inertia. Controller/Page Up/Down scrolling only writes a changed offset when there is actual input, and the maximum offset is clamped to zero or greater while geometry is settling.
