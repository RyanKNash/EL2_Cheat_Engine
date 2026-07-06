# Developer Notes

## Project Basics

- This is a BepInEx 5 mod for Endless Legend 2.
- The project targets .NET Framework 4.7.2.
- Plugin metadata lives in `Plugin.cs`; do not rename the plugin GUID, name, or version unless the release plan explicitly requires it.
- Runtime state for menu toggles and multiplier values lives in `ModState.cs`.
- Slider defaults and bounds live in `config.cs`.
- The IMGUI menu is rendered by `GuiRenderer.cs`.
- Harmony patches for yield multipliers live in `HarmonyPatches.cs`.
- One-shot resource/current-stock injection lives in `ResourceInjector.cs`.

## Target Empire Safety

Resource and yield changes are intended to apply only to selected `TargetPlayers`.

- Never fall back to all empires when target resolution fails.
- If no target empire is selected, injection should do nothing and log a warning.
- If a selected target cannot be resolved to a MajorEmpire, skip it and log the failure.

## Special Resource Mapping

Known special resource indices:

- Special 26: Corpses. This is exposed in the GUI as `Corpses` and backed by `ModState.FillSpecial26`.
- Special 27: Fallen Spirits. This is not currently used in gameplay, so it is intentionally hidden from the GUI. The source keeps commented-out placeholders in `ModState.cs` and `ResourceInjector.cs` so it can be restored quickly if the game starts using it.

Special resource indices 28 through 32 are not currently exposed in the GUI because their gameplay meaning is unknown or unused for current testing.

## Fame

Fame is not currently in use and is intentionally hidden from the GUI. The source keeps commented-out placeholders for the fame multiplier state, configuration, icon, slider, and Harmony patch so it can be restored for future implementation.

## Current-Stock Injection

`ResourceInjector` uses defensive reflection for current Gold/Money and Influence injection.

Money lookup order:

- `MoneyStock`
- `DustStock`
- `GoldStock`

Influence lookup:

- `InfluenceStock`

These members may be plain writable numeric/`FixedPoint` values or EditableProperty-style objects with a writable `Value` member.
