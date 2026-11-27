# MissionControl & Designer Props System

This directory defines placeable **buildings**, **structures**, **destructibles** (flimsies), **dropships**, **decals**, and their underlying **models** used by both MissionControl and Designer.

## Three Asset Sources

### 1. Vanilla Game Assets (`"AssetSource": "GameData"`) - Default for Vanilla Content

Both MissionControl and Designer use `BTAssetReader` to read vanilla BattleTech meshes, materials, textures, destruction shells, and split prefabs directly from `BattleTech_Data` at runtime - no AssetBundles or exported game art required.

Example (`props/models/Military/LargeMilitaryBldgA/model.json`):

```json
{
  "Key": "LargeMilitaryBldgA",
  "IsVanilla": true,
  "AssetSource": "GameData",
  "Mesh": "largeMilitaryBldgA",
  "Materials": [
    { "Name": "envMatStct_darkMetal_generic" },
    { "Name": "envMatStct_glassA_decals_generic" }
  ],
  "IsMeshInBundle": false,
  "HasCustomSplits": true,
  "SplitPrefabName": "envPrfComp_largeMilitaryBldgA_generic_static_destruct_split",
  "HasCustomShell": true,
  "DestructibleSize": "huge",
  "DestructibleMaterial": "stone",
  "FlimsyDestructibleType": "largeStone"
}
```

Optional fields for disambiguating duplicate vanilla mesh names (such as `fence`, `door`, or `acUnit1`) and adjusting pivots without Unity Editor scripts:

- `"SourceAssetFile"`: e.g. `"sharedassets8.assets"`
- `"MeshPathIds"`: per-slot PathID map (`"LOD0"`, `"LOD1"`, `"LOD2"`, `"COL"`, `"Glass"`, `"Shell"`, `"ShellCOL"`, `"Flimsy"`)
- `"SplitPrefabName"`: vanilla `*_generic_static_destruct_split` root GameObject name in `sharedassets*.assets`
- `"PivotMode"`: `"Original"`, `"Center"`, or `"Bottom"`

To regenerate or inspect the vanilla catalogue against your BattleTech install:

```bash
dotnet run --project MissionControl/tools/VanillaPropCatalogueGenerator/VanillaPropCatalogueGenerator.csproj -- --dry-run
```

### 2. Custom `.glb` / `.obj` Models (`"AssetSource": "File"`) - Recommended for Custom 3D Props

Drop a standard `.glb` (glTF 2.0 binary) or `.obj` (+ `.mtl`) file alongside `model.json` - no Unity 2018.4 or DinoFracture required. Use **Designer-Prop-Studio** (Godot 4.7.2 C# desktop app) to visually adjust orientation, pivot, height, vanilla BattleTech material overrides, `BTLight` placement, and preview procedural plane-sliced splits and Auto-Stump ruined shells before saving directly into `<Mod>/mcData/props/`.

Supported `model.json` fields for `"AssetSource": "File"`:
- `"ModelFile"`: filename relative to `model.json` (auto-detected if omitted)
- `"PivotMode"`: `"Bottom"` (default), `"Center"`, or `"Original"`
- `"RotationOffset"` / `"PositionOffset"` / `"TargetHeight"` (metres) / `"ColliderMode"` (`"Mesh"`, `"Box"`, `"Convex"`)
- `"Materials"`: `"$embedded"` to use the `.glb`/`.obj` PBR textures auto-mapped to `BattleTech Standard`, or any vanilla BT material name (e.g. `"envMatStct_darkMetal_generic"`)
- `"SplitMode"` (`"Auto"`, `"Custom"`, `"None"`), `"FractureCount"`, `"FractureSeed"`
- `"ShellMode"` (`"AutoStump"`, `"Rubble"`, `"Custom"`), `"StumpHeight"`
- `"Lights"`: array of `BTLight` definitions (`Point`, `Spot`, `Linear`, `Area`) - or auto-imported from `.glb` `KHR_lights_punctual`

### 3. Custom Bundled Assets (`"AssetSource": "Bundle"`, `"IsMeshInBundle": true`)

Use custom Unity 2018.4 AssetBundles (built with `Designer-Prop-Bundler`) only for custom Unity shaders, rigged animations, or custom VFX not covered by `"AssetSource": "File"`.

Working bundled examples are included under:
- `props/models/Examples/CustomBundledCuboid/model.json` & `props/structures/Examples/ExampleCustomBundledStructure.json` (demonstrates `"AssetSource": "Bundle"`, `"BundleOverride"`, `"PivotMode": "Bottom"`, and pairing a custom bundled mesh with a runtime-loaded vanilla material from `BattleTech_Data`)
- `props/models/Arenas/ArenaLargeOneSideOpenA/` (demonstrates a multi-submesh custom bundled structure with `MeshOffsets`)
- `props/models/Blocks/SmallCuboidA/` and `props/models/Walls/WallTinyThinRectangle/` (demonstrate standalone `*-bundle` files co-located with `model.json`)

Campaign mods can also place their own custom prop definitions and bundles under `<Mod>/mcData/props/` (using the same `models/`, `buildings/`, `structures/`, `destructibles/`, `decals/` folder structure).

## Post-1.6 TODO: Urban Warfare (`uTech`) Buildings

Urban Warfare's multi-mesh modular skyscrapers and Houdini VAT (Vertex Animation Texture) collapse buildings (`level84`–`level93` / `sharedassets84..93.assets`) are intentionally excluded from the v1.6 catalogue rollout. Supporting them after 1.6 will involve:
- Multi-mesh assembly loading for modular urban building prefabs
- Wiring Urban Warfare VAT collapse textures (`*_damaged_pos` / `*_damaged_rot`) and `Urban/Urban Building` shader states
- Optional `RequiredContentPack` DLC gating (`"urbanwarfare"`)
