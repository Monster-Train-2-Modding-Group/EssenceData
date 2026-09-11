# EssenceData

Data mod containing Essences for Units. This mod by itself does not surface the mechanic in-game.

Currently the mod defines essences for the MT1 units, along with Dante, Shield and Spear Steward.

If you wish to help with designing essences for the MT2 units there is a spreadsheet.
https://docs.google.com/spreadsheets/d/1lRSo0pYxt87rcTUNz7858ZSK5r8swT7mkqz8okg7jZo/edit?usp=sharing

## For Modders
You merely only need to define a dependency on this mod to use.
If you wish to conduct Fusion a RewardData subclass is already defined and setup. Just grant the reward and the player is allowed to use an available unit to fuse with another.

```json
    {
      "id": "UnitSynthesisReward",
      "type": "custom_class",
      "custom_class": {
        "id": "@UnitSynthesisRewardData",
        "mod_reference": "EssenceData"
      },
      "show_animation_in_event": true,
      "show_cancel_override": true,
      "collect_sfx_cue": "Collect_CardRare",
      "costs": [ 50 ]
    },
```

## To define an essence for a custom unit.
This is done via a dependency on Conductor and defining a map between unit <=> essence upgrade. This is a 1:1 mapping that is an essence upgrade is only associated with one character and one character has only 1 essence.

Examples of how to define an essence are in the `json/essences` folder

## TODOs
- UI: A Deck Screen button to toggle unit synthesis effects. Currently you can only view the essences via when a fusion happens.