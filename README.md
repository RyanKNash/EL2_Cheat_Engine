# EL2 Cheat Engine

A BepInEx mod for Endless Legend 2 that adds an in-game cheat menu for yield multipliers and resource injection.

## How to use

1. Install the mod and start Endless Legend 2.
2. Open or hide the cheat menu with any of these keys:
   - `Insert`
   - `F10`
   - `Home`
   - `F8`
3. Select one or more target empires at the top of the menu.
   - `Player` is empire index `0`.
   - `AI 1` through `AI 6` are the other supported empire slots.
4. Enable the yield cheats you want, then set the multiplier sliders.
   - Dust
   - Industry
   - Science
   - Influence
   - Fame
5. For resources, set an amount, choose resource groups, enable `Allow Resource Injection`, then press `ADD RESOURCES NOW`.

Yield multipliers only apply to selected target empires. Resource injection is also blocked unless at least one target empire is selected and `Allow Resource Injection` is enabled.

## Installation

1. Install BepInEx for Endless Legend 2.
2. Download the latest release from this repository.
3. Copy `EL2_cheat_engine.dll` into:

```text
Endless Legend 2/BepInEx/plugins/
```

4. Launch the game.
5. Confirm the mod loaded by looking for the yellow on-screen text:

```text
EL2 Cheat Engine loaded - Press Home/F8/Insert/F10
```

If the menu is hidden, click `Open EL2CE` or press one of the toggle keys.

## Features

- In-game draggable IMGUI menu.
- Per-empire targeting for up to seven empire slots.
- Toggleable yield multipliers for:
  - Dust
  - Industry
  - Science
  - Influence
  - Fame
- Resource injection for selected target empires.
- Separate resource group toggles for strategic, luxury, and special resources.
- BepInEx logging for patching, targeting, and resource injection diagnostics.

## Compatibility

This mod is built for the current Endless Legend 2 assemblies used by this project. Game updates can rename or move internal methods, which may break Harmony patches until the mod is updated.

This is intended for single-player or private testing. Do not use it in multiplayer games where other players have not agreed to it.

## Building from source

Requirements:

- .NET SDK capable of building `net472`
- BepInEx installed in your Endless Legend 2 directory
- Endless Legend 2 managed assemblies available locally

The project references local game and BepInEx assemblies from the paths in `EL2_cheat_engine.csproj`. If your game is installed somewhere else, update the `HintPath` values before building.

Build with:

```powershell
dotnet build -c Release
```

The compiled plugin DLL will be created under:

```text
bin/Release/net472/EL2_cheat_engine.dll
```

Copy that DLL to `BepInEx/plugins/` to test it in game.

## Troubleshooting

If the mod does not load:

- Make sure BepInEx is installed and the game has been launched once with BepInEx.
- Make sure `EL2_cheat_engine.dll` is directly inside `BepInEx/plugins/` or a folder under `plugins/`.
- Check the BepInEx console or log file for `EL2 Cheat Engine`.

If a cheat does not affect the expected empire:

- Confirm the correct target button is selected.
- Try selecting only `Player` first, then test again.
- Check the BepInEx log for target empire indexes and patch diagnostics.

If resource injection does nothing:

- Select at least one target empire.
- Enable `Allow Resource Injection`.
- Select at least one resource group: `Strategic`, `Luxury`, or `Specials`.
- Press `ADD RESOURCES NOW`.

## Repository notes

Game assemblies, build output, and local BepInEx files are ignored by `.gitignore`. Public releases should include only the compiled mod DLL and any release notes needed by users.

