# Extended Lances

Extended lances can change the lance size of vanilla lance spawns and ones created with Mission Control's `Additional Lances` feature.

## Settings Breakdown  

```json
"ExtendedLances": {
  "Enable": true,
  "Autofill": true,
  "LanceSizes": {
    "5": ["Davion"],
    "6": ["Comstar"],
}
```

| Path | Required? | Default | Details |
| ---- | --------- | ------- | ------- | ------- |
| `Enable` | Optional | true | Key must be unique. It is used by the mod in the `settings.json` LancePools to specify the lance selection |
| `Autofill` | Optional | true | This specifies what type of lance this definition is. `Tagged` uses the lance tags to select an appropriate lance and `Manual` allows you to manually create a specific lance. If a specific lance def id is used then it ignores the units specified below and uses the full lance definition. `lanceTagSet` and `lanceExcludedTagSet` is ignored if a specific mech def id is set.  |
| `LanceSizes` | Optional | N/A | Allows the lance definition to specify what type of lance to select by tags |

### Lance Sizes

| Path | Required? | Default | Example | Details |
| ---- | --------- | ------- | ------- | ------- |
| Any string number above 4 (e.g. "5") | Optional | N/A | `"5": ["Davion"]` | The faction short name is used to identify which faction should have the set number of units