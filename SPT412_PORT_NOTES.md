# SAIN SPT 4.1.2 Port Notes

This branch starts from `origin/4.1.0-SPT4` and makes the minimum metadata/build changes needed to test against SPT `4.1.2`.

## Current Port Changes

- EFT build gate changed from `35392` to `40743`, matching SPT `4.1.2`.
- BepInEx SPT dependency changed from minimum `4.0.0` to `4.1.0`.
- Server mod `package.json` changed from `~3.11.0` to `~4.1.0`.
- `SAIN.csproj` now supports `SPT_INSTALL_DIR` or `/p:SptInstallDir=...` instead of hard-coded local paths.
- Post-build copy now targets `$(SptInstallDir)\BepInEx\plugins\SAIN`.

## Build

From this directory:

```powershell
$env:SPT_INSTALL_DIR = "D:\Games\SPT-4.1.2"
dotnet build SAIN.sln -c Release
```

Or without setting an environment variable:

```powershell
dotnet build SAIN.sln -c Release /p:SptInstallDir="D:\Games\SPT-4.1.2"
```

The SPT install must contain:

- `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
- `BepInEx\plugins\spt\spt-core.dll`
- `BepInEx\plugins\spt\spt-common.dll`
- `BepInEx\plugins\spt\spt-custom.dll`
- `BepInEx\plugins\spt\spt-debugging.dll`
- `BepInEx\plugins\spt\spt-reflection.dll`
- `BepInEx\plugins\spt\spt-singleplayer.dll`
- `BepInEx\plugins\DrakiaXYZ-BigBrain.dll`

## Expected Next Triage

1. Build against a real SPT `4.1.2` install. Done with `D:\SPT`.
2. Fix compile errors caused by renamed obfuscated EFT symbols. Compile-first pass complete.
3. Start the game and verify the plugin passes its EFT/SPT version checks. Requires game/server restart after install.
4. Run a factory offline raid with SAIN only, BigBrain, and Waypoints. Not reached.
5. Triage runtime exceptions by subsystem: patches first, then hearing/vision, then movement/cover. Not reached.

## Build Result Against `D:\SPT`

The compile-first pass now builds cleanly against `D:\SPT`:

```powershell
dotnet build SAIN.sln -c Release /p:SptInstallDir="D:\SPT"
```

Client artifact installed by post-build:

- `D:\SPT\BepInEx\plugins\SAIN\SAIN.dll`

Server artifact installed manually:

- `D:\SPT\SPT_Runtime\user\mods\SAIN\package.json`
- `D:\SPT\SPT_Runtime\user\mods\SAIN\src\mod.js`

The second pass restores the major SAIN behavior surfaces with current 4.1.2 APIs:

- Component bootstrap and SAIN-driven bot ticking.
- Custom SAIN look sensor update via `VisionRaycastJob`.
- Global look settings, no AI ESP, flashlight state tracking.
- Aim status/hard aim integration through `BotAimingData`.
- SAIN aim time, aim offset, smooth turn, pitch limit, hit reaction, and malfunction patches.
- Patrol stance, pose/aim stamina, movement AI flags, snap prevention, and shoot-state movement patches.
- Hearing hooks for shots, weapon modification, bullet impact, grenade collision, and bot hearing sensor interception.
- Talk suppression/listening hooks for player and bot speech.
- Grenade state tracking and throw decisions through current `BotGrenadeController` APIs.
- Medical item direct use through `TryApplyToCurrentPart`.
- Busy-hands recovery, weapon attachment classification, and magazine refill helpers.

Old patch source files remain quarantined with `#if false` because they target removed obfuscated symbols. Their restored behavior lives in `Patches/PortedSubsystemPatches.cs`.

Known residual risk:

- The tool policy blocked recursive deletion of the previous live SAIN folders, but the final build/install overwrote the client DLL and refreshed the server mod.
- Door interaction animation completion remains conservative because the old `Player.vmethod_0/vmethod_1` hooks are gone; door state changes still use `Door.Interact`.
- Some highly specific sound hooks from the old hearing patch file are not individually restored; the core sound model hooks are restored.
- Runtime validation still requires restarting SPT server and EFT, then checking BepInEx/SPT logs after an offline raid.

## Replacement Direction

If the SAIN port becomes too expensive, build a smaller replacement instead of copying SAIN wholesale:

- Keep BigBrain as the layer injection mechanism.
- Keep the first release scoped to combat behavior only: acquire threat, choose cover, peek/shoot, reposition, push, retreat.
- Avoid custom GUI and preset complexity until the combat loop is stable.
- Treat hearing, vision, and cover as explicit services with debug logging before adding personality systems.
- Prefer correctness and predictable performance over trying to match every SAIN feature.
