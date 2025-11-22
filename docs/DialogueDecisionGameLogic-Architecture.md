# DialogueDecisionGameLogic Architecture

## Overview

DialogueDecisionGameLogic is a new dialogue type for Mission Control that displays multiple decision buttons instead of a single OK button. Players can select different options, each triggering different results/actions defined in the contract type builder.

---

## Architecture

### 1. Core Component: DialogueDecisionGameLogic

**Inheritance:**
```csharp
public class DialogueDecisionGameLogic : DialogueGameLogic
```

**Why inherit from DialogueGameLogic:**
- ✅ Inherits `ApplyContractOverride()` - automatic contract JSON text support
- ✅ Inherits `conversationContent` - dialogue text storage
- ✅ Inherits `showOnlyOnce`, `dialogueShownStatus` - standard dialogue behavior
- ✅ Works with `ItemRegistry.GetItemByGUID<DialogueGameLogic>()` - full compatibility
- ✅ `TriggerDialogue()` works as-is - sends `TriggerDialog` message normally

**Additional Fields:**
```csharp
public List<DialogueDecisionOption> decisionOptions;
```

**DialogueDecisionOption Structure:**
```csharp
public class DialogueDecisionOption {
    public string ButtonText;                  // Text shown on button
    public List<DesignResult> Results;         // Actions to execute when clicked
    public bool CloseOnClick = true;           // Auto-close dialogue after click
    public DesignConditional Conditional;      // Show/hide button conditionally
    public string NextDialogueGuid;            // Branch to different dialogue (optional)
    public int NextContentIndex = -1;          // Jump to content index (-1 = sequential)
}
```

---

### 2. Integration Point: CombatHUD Patch

**Patch Target:** `CombatHUD.OnTriggerDialog`

**Strategy:** Intercept at the UI rendering stage to detect our subclass and show custom UI instead of `InterruptDialogQueueSequence`.

**Why this approach:**
- ✅ **Mod Compatible** - `TriggerDialog` message still sent (other mods can intercept)
- ✅ **Message Flow Maintained** - `DialogueStartMessage` and `DialogComplete` published
- ✅ **Validation Reused** - Same checks as vanilla (showOnlyOnce, empty content, etc.)
- ✅ **Clean Separation** - Only replaces UI rendering, not the entire flow

**Patch Logic:**
```csharp
[HarmonyPatch(typeof(CombatHUD), "OnTriggerDialog")]
public class CombatHUDOnTriggerDialogPatch {
    static bool Prefix(CombatHUD __instance, MessageCenterMessage message) {
        TriggerDialog triggerDialog = message as TriggerDialog;
        if (triggerDialog == null) return true;

        // Detect DialogueDecisionGameLogic subclass
        var decisionLogic = __instance.Combat.ItemRegistry
            .GetItemByGUID<DialogueDecisionGameLogic>(triggerDialog.DialogID);

        if (decisionLogic == null) return true; // Not our type

        // Replicate vanilla validation
        if (decisionLogic.showOnlyOnce &&
            decisionLogic.dialogueShownStatus == DialogueShownStatus.Shown) {
            __instance.Combat.MessageCenter.PublishMessage(
                new DialogComplete(triggerDialog.DialogID));
            return false;
        }

        if (decisionLogic.conversationContent.contents.Length == 0) {
            __instance.Combat.MessageCenter.PublishMessage(
                new DialogComplete(triggerDialog.DialogID));
            return false;
        }

        // Maintain message flow
        __instance.Combat.MessageCenter.PublishMessage(
            new DialogueStartMessage(triggerDialog.DialogID));

        // Show custom decision sequence using SGDialogWidget
        InterruptDialogDecisionSequence sequence = new InterruptDialogDecisionSequence(
            __instance.Combat,
            __instance.dialogWidget,
            decisionLogic
        );
        __instance.Combat.MessageCenter.PublishMessage(
            new AddSequenceToStackMessage(sequence));

        return false; // Skip vanilla
    }
}
```

---

### 3. Sequence Implementation: InterruptDialogDecisionSequence

**Approach:** Extend vanilla `InterruptDialogSequence` pattern, using existing `SGDialogWidget` for UI

**Why SGDialogWidget:**
- ✅ Already supports multiple response buttons via `SGDialogOption`
- ✅ Proven vanilla UI component (used in SimGame conversations)
- ✅ No custom UI cloning needed
- ✅ Automatically styled to match game UI
- ✅ Built-in button click handling

**Key Implementation:**
```csharp
public class InterruptDialogDecisionSequence : InterruptSequence {
    private SGDialogWidget dialogWidget;
    private DialogueDecisionGameLogic decisionLogic;
    private CombatGameState combat;

    public InterruptDialogDecisionSequence(CombatGameState combat,
                                          SGDialogWidget dialogWidget,
                                          DialogueDecisionGameLogic decisionLogic)
        : base(combat) {
        this.combat = combat;
        this.dialogWidget = dialogWidget;
        this.decisionLogic = decisionLogic;
    }

    public override void OnAdded() {
        base.OnAdded();

        // Get dialogue content
        DialogueContent content = decisionLogic.conversationContent.contents[0];

        // Filter buttons by Conditionals
        List<DialogueDecisionOption> visibleOptions = FilterVisibleOptions();

        // Create SGDialogOption for each visible button
        List<SGDialogOption> responses = CreateResponseButtons(visibleOptions);

        // Show dialogue with responses
        dialogWidget.Show(
            content.words,
            content.selectedCastDefId,
            responses,
            OnResponseSelected
        );
    }

    private void OnResponseSelected(int responseIndex) {
        DialogueDecisionOption selectedOption = visibleOptions[responseIndex];

        // Execute results
        foreach (var result in selectedOption.Results) {
            result.Trigger(null, decisionLogic.encounterObjectGuid);
        }

        // Handle branching
        if (!string.IsNullOrEmpty(selectedOption.NextDialogueGuid)) {
            // Branch to different dialogue
            combat.MessageCenter.PublishMessage(
                new TriggerDialog(selectedOption.NextDialogueGuid));
        } else if (selectedOption.NextContentIndex >= 0) {
            // Jump to specific content index
            // (advance conversationContent index)
        }
        // else: sequential (default behavior)

        // Close if specified
        if (selectedOption.CloseOnClick) {
            dialogWidget.Hide();
        }

        // Mark complete
        combat.MessageCenter.PublishMessage(
            new DialogComplete(decisionLogic.encounterObjectGuid));
    }

    private List<DialogueDecisionOption> FilterVisibleOptions() {
        List<DialogueDecisionOption> visible = new List<DialogueDecisionOption>();
        foreach (var option in decisionLogic.decisionOptions) {
            if (option.Conditional == null || option.Conditional.Evaluate()) {
                visible.Add(option);
            }
        }
        return visible;
    }
}
```

**Branching System (Option 4 Hybrid):**
- `NextDialogueGuid` - Branch to a different DialogueGameLogic object
- `NextContentIndex` - Jump to specific index within same dialogue
- Default (neither set) - Sequential, continue to next content
- Each button can have different branching behavior

---

### 4. Factory Integration: DialogueFactory

**Add Factory Method:**
```csharp
public static DialogueDecisionGameLogic CreateDialogueDecisionLogic(
    GameObject parent,
    string name,
    string guid,
    DialogueOverride dialogueOverride,
    List<DialogueDecisionOption> options,
    Dictionary<string, EncounterObjectGameLogic> encounterObjects) {

    GameObject decisionGo = new GameObject(name);
    decisionGo.transform.parent = parent.transform;

    DialogueDecisionGameLogic decision =
        decisionGo.AddComponent<DialogueDecisionGameLogic>();
    decision.encounterObjectGuid = guid;
    decision.decisionOptions = options;

    // Apply dialogue text from contract override
    if (dialogueOverride != null) {
        decision.ApplyContractOverride(dialogueOverride, encounterObjects);
    }

    return decision;
}
```

---

### 5. Builder Integration: DialogueBuilder

**Extend Switch Statement** (line ~33 in DialogueBuilder.cs):
```csharp
switch (subType) {
    case "Simple":
        // existing...
        break;
    case "Sequence":
        // existing...
        break;
    case "Decision":  // NEW
        BuildDecisionDialogue(parent, dialogue);
        break;
}
```

**New Builder Method:**
```csharp
private void BuildDecisionDialogue(GameObject parent, JObject dialogue) {
    string guid = dialogue["Guid"].ToString();
    string name = dialogue["Name"].ToString();

    // Parse decision options
    JArray optionsArray = (JArray)dialogue["Options"];
    List<DialogueDecisionOption> options = new List<DialogueDecisionOption>();

    foreach (JObject option in optionsArray) {
        DialogueDecisionOption decisionOption = new DialogueDecisionOption {
            ButtonText = option["ButtonText"].ToString(),
            Results = resultsBuilder.Build((JArray)option["Results"]),
            CloseOnClick = option.ContainsKey("CloseOnClick") ?
                          (bool)option["CloseOnClick"] : true
        };
        options.Add(decisionOption);
    }

    // Get dialogue override for text
    DialogueOverride dialogueOverride = GetDialogueOverride(guid);

    // Create dialogue decision
    DialogueFactory.CreateDialogueDecisionLogic(
        parent, name, guid, dialogueOverride, options, encounterObjects);
}
```

---

---

## Data Separation: Button Text Location

### Recommended Approach: Button Text in Contract Type Build

**Rationale:**
- **Contract Type Build JSON** = Structural/gameplay data (logic, results, conditions)
- **Contract Override JSON** = Flavor text (moddable mission-specific content)
- Button text is **structural** - tied to Results and Conditionals, not flavor
- Keeps button logic and text together for easier maintenance

**Why Contract Override Can't Store Button Text:**
- `DialogueOverride` class has no extensibility (fixed fields only)
- `ApplyContractOverride()` is not virtual (can't override to parse custom data)
- No JsonExtensionData support (can't capture unknown fields)
- Would require complex workarounds (parallel config, field abuse, or patching)

**Clean Data Flow:**
```
Contract Type Build → decisionOptions (button texts + Results + Conditionals)
Contract Override → conversationContent (main dialogue text only)
```

**Benefits:**
- ✅ Button text stays with its logic/results
- ✅ Main dialogue text moddable via contract override
- ✅ No hacks or workarounds needed
- ✅ Follows existing MC architecture patterns
- ✅ Easy to maintain and extend

---

## JSON Format

### Contract Type Builder JSON (Structure & Logic)

```jsonc
{
  "Name": "Dialogue_Choose_Approach",
  "Type": "Dialogue",
  "SubType": "Decision",
  "Guid": "12345678-abcd-1234-abcd-123456789012",
  "Options": [
    {
      "ButtonText": "Attack Immediately",
      "Results": [
        {
          "Type": "SetStatus",
          "EncounterGuid": "combat-chunk-guid",
          "Status": "Active"
        },
        {
          "Type": "CompleteObjective",
          "EncounterGuid": "stealth-objective-guid",
          "Status": "Failed"
        }
      ],
      "CloseOnClick": true,
      "Conditional": null,
      "NextDialogueGuid": "",
      "NextContentIndex": -1
    },
    {
      "ButtonText": "Sneak Around Back",
      "Results": [
        {
          "Type": "ExecuteGameLogic",
          "EncounterGuid": "stealth-logic-guid"
        }
      ],
      "CloseOnClick": false,
      "Conditional": {
        "Type": "DesignConditional",
        "Comment": "Only show if player has stealth lance"
      },
      "NextDialogueGuid": "stealth-approach-dialogue-guid",
      "NextContentIndex": -1
    },
    {
      "ButtonText": "Wait and Observe",
      "Results": [
        {
          "Type": "Delay",
          "Rounds": 2
        }
      ],
      "CloseOnClick": true,
      "NextContentIndex": 1
    }
  ]
}
```

### Contract Override JSON (Dialogue Text)

```json
{
  "dialogueList": [
    {
      "dialogue": {
        "EncounterObjectGuid": "12345678-abcd-1234-abcd-123456789012"
      },
      "name": "Dialogue_Choose_Approach",
      "dialogueContent": [
        {
          "words": "Commander, we've reached the enemy position. What's your approach?",
          "wordsColor": { "r": 1, "g": 1, "b": 1, "a": 1 },
          "selectedCastDefId": "castDef_DariusDefault",
          "emote": "Default",
          "audioName": "NONE",
          "cameraFocusGuid": "",
          "cameraDistance": "Far",
          "cameraHeight": "Default",
          "revealRadius": -1
        }
      ]
    }
  ]
}
```

### Triggering from Contract Type Builder

```jsonc
{
  "OnActiveExecute": [
    {
      "Type": "Dialogue",
      "EncounterGuid": "12345678-abcd-1234-abcd-123456789012",
      "IsInterrupt": true
    }
  ]
}
```

---

## File Structure

### New Files

```
src/Core/EncounterNodes/Dialogue/DialogueDecisionGameLogic.cs
src/Core/LogicComponents/Dialogue/DialogueDecisionOption.cs
src/Core/EncounterSequences/InterruptDialogDecisionSequence.cs
src/Patches/CombatDialog/CombatHUDOnTriggerDialogPatch.cs
```

### Modified Files

```
src/Core/EncounterFactories/DialogueFactory.cs (add factory method)
src/Core/ContractTypeBuilders/NodeBuilders/DialogueBuilder.cs (add Decision case)
```

---

## Message Flow

### Complete Flow

1. **Contract Type Builder** → Creates `DialogueDecisionGameLogic` as child of DialogueGameLogic
2. **Contract Override** → `ApplyContractOverride()` loads dialogue text
3. **Trigger** → `DialogResult.Trigger()` sends `TriggerDialog` message
4. **Other Mods** → Can intercept `TriggerDialog` message
5. **CombatHUD** → `OnTriggerDialog` receives message
6. **Our Patch** → Detects `DialogueDecisionGameLogic` subclass
7. **Our Patch** → Publishes `DialogueStartMessage`
8. **Our Patch** → Creates `InterruptDialogDecisionSequence` and adds to stack
9. **Sequence** → Shows SGDialogWidget with filtered buttons (Conditionals)
10. **Button Click** → Execute option's results, handle branching
11. **Cleanup** → Publish `DialogComplete` message

### Message Types Used

- `TriggerDialog` - Sent by DialogResult.Trigger (standard)
- `DialogueStartMessage` - Published by our patch (for mod compatibility)
- `DialogComplete` - Published when button clicked (standard)

---

## Compatibility Considerations

### Mod Compatibility

- ✅ Standard `TriggerDialog` message sent
- ✅ Standard `DialogueStartMessage` sent
- ✅ Standard `DialogComplete` sent
- ✅ Works with dialogue sequences
- ✅ Other mods can patch any part of the flow
- ✅ No custom messages that could break other mods

### Vanilla Compatibility

- ✅ Inherits all DialogueGameLogic behavior
- ✅ Registered in ItemRegistry as DialogueGameLogic
- ✅ Contract override system works unchanged
- ✅ Validation, serialization all inherited
- ✅ No changes to base game classes

---

## UI Approach Decision

### Chosen: SGDialogWidget via InterruptDialogDecisionSequence

**Reasons:**
- ✅ SGDialogWidget already supports multiple response buttons
- ✅ Proven vanilla UI component (used in SimGame)
- ✅ No custom UI cloning needed
- ✅ Follows vanilla InterruptDialogSequence pattern
- ✅ Integrates with sequence stack system
- ✅ Automatic styling and behavior

**Alternative Considered:**
- Custom UI via GenericPopup clone (initial approach) - More complex, less integrated
- Asset Bundle approach - Requires Unity Editor, harder to maintain

**Implementation:**
- Extend InterruptSequence (like vanilla InterruptDialogSequence)
- Use SGDialogWidget.Show() with multiple SGDialogOption responses
- Handle button clicks via callback
- Filter buttons by Conditionals before display

---

## Key Insights

### Why This Architecture Works

1. **No Override Needed** - `TriggerDialogue()` doesn't need to be virtual because we patch the receiver (CombatHUD), not the sender
2. **Full Inheritance Benefits** - Get all DialogueGameLogic functionality for free
3. **Mod Compatibility** - Maintain entire message flow so other mods work
4. **Clean Separation** - Only replace sequence, everything else reused
5. **Vanilla UI Reuse** - SGDialogWidget provides proven multi-button interface
6. **Conditional Logic** - MC Conditionals system for show/hide buttons
7. **Branching Support** - NextDialogueGuid and NextContentIndex for complex flows
8. **Future Extensible** - Easy to add new result types or button behaviors

### Alternative Approaches Considered

1. **Don't Inherit from DialogueGameLogic** - Would require reimplementing ApplyContractOverride, validation, etc.
2. **Patch DialogResult.Trigger** - Would bypass message flow, breaking mod compatibility
3. **Create Custom Message Type** - Would require more patches, less compatible
4. **Use ExecutableGameLogic Pattern** - Less consistent with vanilla dialogue system
5. **Custom UI via GenericPopup Clone** - More complex, less integrated than SGDialogWidget
6. **Use Conversation System** - Too complex, user explicitly wanted simpler MC-based approach

---

## Implementation Checklist

- [x] Create DialogueDecisionOption data class
- [x] Create DialogueDecisionGameLogic component
- [x] Extend DialogueFactory with factory method
- [x] Extend DialogueBuilder with Decision case
- [x] Create CombatHUDOnTriggerDialogPatch (initial version)
- [ ] Add Conditional, NextDialogueGuid, NextContentIndex to DialogueDecisionOption
- [ ] Create InterruptDialogDecisionSequence
- [ ] Update CombatHUDOnTriggerDialogPatch to use sequence
- [ ] Update DialogueBuilder to parse Conditional, NextDialogueGuid, NextContentIndex
- [ ] Delete obsolete DialogueDecisionUI
- [ ] Test multiple button options
- [ ] Test Conditional button visibility
- [ ] Test result execution
- [ ] Test NextDialogueGuid branching
- [ ] Test NextContentIndex jumping
- [ ] Test sequential default behavior
- [ ] Verify mod compatibility
- [ ] Test dialogue sequences with decisions
- [ ] Test showOnlyOnce behavior
- [ ] Test empty content handling

---

## Future Enhancements

### Potential Features

1. ~~**Conditional Buttons**~~ - ✅ IMPLEMENTED via DesignConditional
2. ~~**Branching Dialogues**~~ - ✅ IMPLEMENTED via NextDialogueGuid/NextContentIndex
3. **Button Tooltips** - Show consequences of each choice
4. **Icon Support** - Add icons to buttons
5. **Timer Support** - Auto-select after timeout
6. **Keyboard Shortcuts** - Number keys to select options
7. **Result Preview** - Show what each button will do
8. **Button Validation** - Gray out invalid choices (vs hide via Conditional)
9. **Multiple Pages** - More than X buttons with pagination
10. **Button Text from Override** - Parallel config system for moddable button text

### Extension Points

1. **Custom Result Types** - Add new result types in ResultsBuilder
2. **Custom Validators** - Add validation for decision-specific rules
3. **Custom UI Themes** - Support different button layouts/styles
4. **Analytics** - Track which decisions players make
5. **Replay Support** - Record decision choices for replay

---

## References

### Key Source Files

**Base Game:**
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech\DialogueGameLogic.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech\DialogueSequenceGameLogic.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech\InterruptDialogSequence.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech\InterruptDialogQueueSequence.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech.UI\CombatHUD.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech.UI\SGDialogWidget.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech.UI\SGDialogOption.cs`
- `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech.Designed\DialogResult.cs`

**Mission Control:**
- `src/Core/EncounterFactories/DialogueFactory.cs`
- `src/Core/ContractTypeBuilders/NodeBuilders/DialogueBuilder.cs`
- `src/Core/ContractTypeBuilders/ResultsBuilders/ResultsBuilder.cs`
- `src/Patches/CombatDialog/DialogResultPatch.cs`
- `src/Core/UiManager.cs`

### Related Documentation

- Contract Type Builder Documentation
- Results System Documentation
- Dialogue System Documentation
