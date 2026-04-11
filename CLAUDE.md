# MissionControl

**Use British (UK) English in all responses and generated content.**

## What This Is

A HBS BattleTech mod (v1.6.0.8) that adds custom contract types and varies encounter specifics:
encounter boundary sizes, spawn locations, lance numbers, and objectives. It reads JSONC "contract
type build" files that define how encounters are constructed at runtime.

Website: https://www.missioncontrolmod.com/

## GitHub

- **Repository**: CWolfs/SpawnVariation (historical name - NOT "MissionControl")
- **Primary branch**: `develop` (not master - master is for releases)

## Build

```bash
dotnet build src/MissionControl.csproj
# Output: src/bin/Debug/net471/MissionControl.dll
```

## Deploy

```bash
./copy-assets.sh
# Copies DLL + all data directories to:
# D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl/
```

## Architecture

**Entry point**: `MissionControl.Main.Init(string modDirectory, string modSettings)` via mod.json

### Source layout (`src/`)
- `Main.cs` - Entry point, Harmony setup
- `Core/MissionControl.cs` - Singleton managing encounter state
- `Core/DataManager.cs` - Loads contract type builds, configs, props
- `Core/API.cs` - Public API for external mods
- `Core/ContractTypeBuilders/` - Parses JSONC build files into Unity GameObjects
- `Core/EncounterRules/` - Contract type rules (~12 types: SimpleBattle, CaptureBase, Blackout, DuoDuel, SoloDuel, etc.)
- `Core/EncounterLogic/` - Runtime spawn, boundary, lance, objective logic
- `Core/EncounterFactories/` - Factory pattern for creating encounter objects (PropFactory, SpawnerFactory)
- `Core/EncounterNodes/` - Node types (Generator, Objectives, Dialogue, Dropship, CombatStates)
- `Core/EncounterTriggers/` - Event trigger system
- `Core/Settings/` - Configuration classes
- `Core/Data/` - Data models (ContractTypeMetadata, Props, Tags)
- `Patches/` - Harmony patches (~30 categories: CustomContractTypes, Props, AI, Save/Load, etc.)
- `Util/` - Extension methods (ArrayExtensions, ContractExtensions, ComponentExtensions)

### Data directories
- `contractTypeBuilds/` - Custom contract type definitions (JSONC). This is the primary output of the Designer.
- `config/AdditionalLances/` - Lance configs per difficulty level (General.json + Difficulty1-10.json)
- `config/Contracts/` - Per-contract override configurations
- `overrides/` - Game data overrides (contracts, encounterLayers, cast, enums)
- `props/` - Asset definitions (buildings, destructibles, dropships, structures)
- `bundles/` - Asset bundles (common-assets-bundle)
- `lances/`, `cast/`, `dialogue/`, `sprites/` - Supporting data

### Initialization flow
1. `Main.Init()` - Load settings, mod.json, create Harmony instance
2. `DataManager.Init()` - Load contract configs, lance data, props
3. `DataManager.SubscribeDeferredDefs()` - Wait for BattleTech DataManager
4. `LoadDeferredDefs()` - Load contractTypeBuilds/*.jsonc, encounter layers, mech/pilot data
5. `ContractTypeBuilder.Build()` - Parse JSONC into encounter GameObjects

## Key Data Formats

### Contract type build (contractTypeBuilds/*.jsonc)
```
Key: string (e.g. "Blackout")
Metadata: { DesignerVersion, MinCompatibleMCVersion, ContractTypeVersion, Authors }
GlobalData: { ContractObjectives[] }
Chunks[]: { Children[] with lance spawners, objectives, AI orders }
Triggers[]: { TriggerOn, SucceedOn, Conditionals[], Results[] }
```
Each contract type has `common.jsonc` plus optional map-specific overrides.

### settings.json
60+ config options including feature toggles: RandomSpawns, HotDropProtection, AdditionalLances,
ExtendedLances, ExtendedBoundaries, DynamicWithdraw. Uses `TypeNameHandling.All` for polymorphic
deserialization.

### mod.json
Declares DLL entry point, custom resource types (EncounterLayer), manifest paths, and
data addendum entries for contract type enumeration.

## CI/CD

**No standalone CI workflow.** MissionControl is built as part of Designer's GitHub Actions
workflow, which checks out the `develop` branch and builds it alongside Designer.

## Dependencies

- `BT-Mod-Dependencies/libs/` - Game DLLs, Harmony, Unity modules
- Krafs.Publicizer v2.2.1 - Accesses internal game APIs
- Newtonsoft.Json - All serialization
- 0Harmony - Runtime patching
- Assembly-CSharp (publicized) - HBS game code

## Gotchas

- GitHub repo is "SpawnVariation", not "MissionControl"
- Primary branch is `develop`, NOT master
- The `docs/` folder contains a built Docusaurus website (HTML/JS), not source documentation
- settings.json uses `TypeNameHandling.All` which is critical for polymorphic deserialization
- Contract type builds use JSONC (JSON with comments) - standard JSON parsers will choke on them
