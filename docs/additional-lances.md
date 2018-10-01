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
| `IncludeContractTypes` | Optional | All available contract types | ["Rescue", "DestroyBase"] would limit lances to these two contract types | An empty array would fallback to 'default' |