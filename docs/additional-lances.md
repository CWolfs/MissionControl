# Additional Lances

Additional enemy and ally lances can be spawned in all contract types based on contract type, biome type, percentage chances, maximum limits and lance configs. These are controlled by the `settings.json` section `AdditionalLances`.

## Settings Breakdown

Comments next to each setting explain its use.

```json
  "AdditionalLances": {
    "IncludeContractTypes": [],
    "ExcludeContractTypes": [],
    "LancePool": {
      "ALL": [ "GENERIC_BATTLE_LANCE"]
    },
    "Enemy": {
      "Max": 3,
      "ExcludeContractTypes": ["Rescue"],
      "ChanceToSpawn": 0.3,
      "LancePool": {
        "ALL": [
          "GENERIC_BATTLE_LANCE"
        ],
        "CONTRACT_TYPE:DestroyBase": [
          "BRAWLER_LANCE", "DEFENDING_LANCE"
        ],
        "CONTRACT_TYPE:DefendBase": [
          "HEAVY_MISSILE_LANCE", "ATTACKING_LANCE"
        ],
        "BIOME:DesertParched": [
          "BBQ_MASTERS"
        ],
        "BIOME:LowlandsSpring": [
          "IM_LIKE_A_TURRET_IN_WATER_I_RUN_SO_HOT"
        ]
      },

    },
    "Allies": {
      "Max": 1,
      "ChanceToSpawn": 0.3,
      "LancePool": {
        "ALL": [
          "GENERIC_BATTLE_LANCE"
        ],
        "CONTRACT_TYPE:DestroyBase": [
          "HEAVY_MISSILE_LANCE", "ATTACKING_LANCE"
        ],
        "CONTRACT_TYPE:DefendBase": [
          "BRAWLER_LANCE", "DEFENDING_LANCE"
        ]
      }
    }
  }
```

| Path | Required? | Default | Example | Details |
| ---- | --------- | ------- | ------- | ------- |
| `IncludeContractTypes` | Optional | All available contract types | `["Rescue", "DestroyBase"]` would limit lances to these two contract types <br><br> `[]` would fallback to default | When set, it overrides `ExcludeContractTypes` for this level |
| `ExcludeContractTypes` | Optional | No contract types | `["Assasinate", "CaptureBase"]` would remove these two contract types from the entire list of available contract types. <br><br> `[]` would fallback to default | |
| `LancePool` | Optional | `ALL` situations will use `GENERIC_BATTLE_LANCE` | See the above code as a full example. <br> Can match to `ALL`, `CONTRACT_TYPE:{key}` and `BIOME:{key}` | All matched conditions will be added to one list of lance pool keys. One key per lance spawn is selected at random for the specific lance |
| `Enemy` | Optional | Children defaults | - | Controls enemy/target specific lance details |
| `Allies` | Optional | Children defaults | - | Controls allies/employer specific lance details |
| `[Enemy|Allies]/Max` | Optional | `1` | `3` | Maximum number of lances to attempt to spawn |
| `[Enemy|Allies]/ChanceToSpawn` | Optional | `0` | `0.3` | Float number from `0` to `1` to represent percentage. `1` being 100% |
| `[Enemy|Allies]/LancePool` | Optional | Empty | Same as parent `LancePool` example | Additive process. Adds to the parent `LancePool` |