# SAIN for SPT 4.1.2

**Solarint's AI Modifications** — replaces the combat AI of almost all NPCs in
Escape From Tarkov: decision trees, dynamic cover, multi-threaded vision raycasting,
advanced movement, personalities, and an in-game GUI editor (F6).

This repository is a **redistribution of pre-built SAIN binaries for SPT 4.1.2**.
No official SPT 4.1.2 build is published upstream, so this repo hosts the compiled
release so people can actually install it.

---

# 🟣 JOIN THE DISCORD — https://discord.gg/nxa3W7w4rJ

### **https://discord.gg/nxa3W7w4rJ**

**This is the single most important link in this README.** All updates, release
announcements, bug fixes, early builds and support happen in the Discord **first**.
If you run this mod, join it — it is the only place you will reliably hear about
breaking changes and new versions.

**What's coming next:** I am building a **post-1.0 patcher and backend, written
from scratch and engineered to be very performant** — a proper foundation instead
of the current patchwork. **All of these mods will shortly be merged into that new
system.** If you want to follow that work, or use it when it lands, the Discord is
where it will be announced.

### 👉 **https://discord.gg/nxa3W7w4rJ** 👈

---


## Provenance & credits

- Original mod by **Solarint** — https://github.com/Solarint/SAIN
- SPT 4.x work by **ArchangelWTF** — https://github.com/ArchangelWTF/SAIN
- Built from the ArchangelWTF SPT 4.1 develop branch. Source lives upstream; this repo
  ships the binaries plus install docs. Licensed MIT (see [LICENSE](LICENSE)).

An earlier, independent SPT 4.1.2 port attempt of mine is preserved on the
`port/spt-4.1.2-legacy` branch and at tag `v2.3.0-spt412`. It is **superseded** by
these builds and is kept for reference only — do not use it.

## Requirements

- **SPT 4.1.2**
- **BigBrain** (required)
- **Waypoints** (required)

## Installation

1. Install BigBrain and Waypoints first.
2. Download the latest release zip.
3. Extract it into your **SPT install root** — the folder containing `EscapeFromTarkov.exe` and `SPT_Runtime`.

The zip is laid out correctly and will place:

```
BepInEx/plugins/SAIN/                              <- client plugin + default configs
SPT_Runtime/user/mods/Solarint-SAIN-ServerMod/     <- server mod (preset sync + web UI)
```

> **Both halves are required.** The server mod serves SAIN's preset storage, settings
> and update endpoints (`/sain/presets`, `/sain/server-settings`, `/sain/updates`) and
> registers the SAIN page in the SPT web UI. Without it the client falls back to local
> presets only.
>
> Server mods live under `SPT_Runtime/user/mods/` — **not** `user/mods/` at the SPT root.
> A mod folder placed at the root is silently ignored.

## Upgrading from an older SAIN

**Delete any old SAIN server mod folder before installing**, in particular
`user/mods/zSolarint-SAIN` — that is the legacy SPT 3.x JavaScript mod. SPT 4.x cannot
load it and will fail at startup with:

```
Exception occured while loading a mod at path: ./user/mods/zSolarint-SAIN
No assemblies found in path: SPT_Runtime/user/mods/zSolarint-SAIN
```

Remove the folder and the error goes away.

## Verified

Boots clean on SPT 4.1.2 alongside 33 other server mods:

```
Mod: SAIN version: 4.5.0 (GUID: me.sol.sain | targets SPT: ~4.1.2) by: Solarint loaded
Mod SAIN has a wwwroot, mapping to /SAIN/
[SAIN] Generated 6 default presets
```
