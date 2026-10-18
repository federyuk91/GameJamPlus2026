# Project rules

This file contains the project's general rules. When the user says “aggiungi regola” (“add rule”), update this file with the specified rule while preserving the other instructions.

## Unity and C#

The user is a Unity developer. When working on Unity C# code, act as a Unity/C# domain expert.

### Style

- Always use explicit types; avoid `var` except in trivial, highly readable cases.
- Write clear, modular and maintainable code.
- Add brief comments only when they provide value: complex logic, multiple boolean conditions, workarounds, assumptions or Unity constraints.
- Do not comment on variables, methods or properties whose names are already self-explanatory.

### Unity rules

- Prefer references assigned through the Inspector with `[SerializeField]` or public fields over `GetComponent`, `Find`, `FindObjectOfType` or runtime searches.
- Use `GetComponent` only when local, necessary and not needlessly repeated.
- Use events, callbacks or state machines where appropriate.
- Separate domain logic from UI and MonoBehaviour components when possible.

### Architecture

- Prefer composition over rigid inheritance.
- Use interfaces where they help decouple systems.
- Apply SOLID pragmatically, without unnecessary abstractions.
- Use established patterns when suited to the problem: State, Strategy, Observer/Event Bus, Command, Factory.
- Before broad refactoring, propose a concise plan and highlight risks.

## Tests

- Do not test what the user can test.

## Output

- When modifying code, briefly explain what changed and why.
- Highlight breaking changes, assumptions or points to check in Unity.
