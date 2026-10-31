# Epic Loot: No Fog Destinations

Keep new Epic Loot adventures on the map you have explored. New bounty and treasure-map destinations use explored centres in the appropriate biome, including exploration shared through the cartography table.

**OctoberJemini's maintained fork of Ken Jorissen's MIT-licensed add-on. This is an independent release, not an official Epic Loot or upstream release.**

## What it does

| Situation | Behavior |
|---|---|
| Buying a new treasure map | Chooses a suitable explored centre in its biome. |
| Accepting a new bounty | Chooses a suitable explored centre in its biome. |
| No eligible explored location | Disables Buy/Accept and explains why. Explore more of that biome. |
| Map scan in progress | Shows “Checking explored locations...” while the incremental scan runs. |
| Existing treasure maps or bounties | Leaves their saved locations unchanged. |
| Personal or shared exploration | Both count toward eligible destinations. |

This restricts the **destination centre**, not the entire search circle or every creature spawn offset. It does not reveal fog, mark undiscovered locations, change rewards, or bypass Epic Loot's affordability and progression rules. Epic Loot's terrain, biome, water, lava and world-edge validation still applies.

## Compatibility

Version 0.3.0 checks the Epic Loot APIs it needs before installing patches. A matching version number is no longer required: compatible older and newer releases can pass the same checks.

| Epic Loot version | Validation |
|---|---|
| 0.14.4, 0.14.5, 0.14.7–0.14.13 | Passed the required API checks. |
| 0.15.1–0.15.4 | Passed the required API checks. |
| 0.13.3 | Missing required APIs; add-on remains inactive. |
| Other/future versions | Checked at startup; supported only if required APIs remain compatible. |

These are assembly/API checks, not full in-game playtests of every release. Future compatibility is best effort: unchanged signatures cannot guarantee unchanged behavior. Valheim updates can also affect compatibility.

If required APIs change or patch installation fails, this add-on reports the problem in the BepInEx log and displays an in-game warning. Epic Loot keeps running, so **new destinations may then be placed in unexplored areas**. Do not ignore an inactive warning.

## Installation

Install BepInEx and Epic Loot, then install this package on each playing client that should use the restriction. It is not a server-wide enforcement mechanism.

**Disable/remove any older No Fog Destinations package first.** This fork keeps the original plugin GUID and configuration filename for migration; running both copies causes a duplicate plugin conflict. Close Valheim before replacing DLLs.

For a manual install, copy the package's `plugins` folder into `BepInEx`. The generated configuration is `BepInEx/config/kenjorissen.mods.epiclootnofogdestinations.cfg`.

| Setting | Default | Effect |
|---|---|---|
| General → Enabled | true | Restricts new destinations to explored centres. Set false to restore Epic Loot placement. |

Optional console command: `epiclootnofog_warningpreview` previews the warning without changing settings or quest behavior.

## Credits and source

- Original add-on: [Ken Jorissen / ValheimMods](https://github.com/kenjorissen/ValheimMods), MIT licensed.
- Maintained fork and OCT map icon: OctoberJemini. The icon was created with AI assistance.
- Requires [Epic Loot by RandyKnapp and contributors](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot).
- [Source, releases and issue reports](https://github.com/OctoberJemini/EpicLootNoFogDestinations).

For a bug report, include your Valheim and Epic Loot versions, this add-on's version, and the relevant BepInEx error. See LICENSE and THIRD_PARTY_NOTICES.md for attribution.

## Building from source

Use `build.ps1` with paths to your Valheim `valheim_Data/Managed` directory, BepInEx directory, EpicLoot.dll and a Roslyn `csc.exe` compiler. Game/mod dependencies are not redistributed. `test.ps1` runs policy tests and API checks against the supplied Epic Loot assembly; it also requires the .NET 8 runtime. See the scripts for parameters.
