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
| ---- | --------- | ------- | ------- |
| `Enable` | Optional | true | Should this feature be enabled or not? |
| `Autofill` | Optional | true | If a lance is selected for a spawn that has below the require number of units - should Extended Lances fill the lance up to the right size?  |
| `LanceSizes` | Optional | N/A | Sets which faction should have higher lance sizes. By default all faction lances are 4 units like vanilla. |

### Lance Sizes

| Path | Required? | Default | Example | Details |
| ---- | --------- | ------- | ------- | ------- |
| Any string number above 4 (e.g. "5") | Optional | N/A | `"5": ["Davion"]` | The faction short name is used to identify which faction should have the set number of units