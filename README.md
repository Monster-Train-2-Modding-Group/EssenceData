# EssenceData

Data mod containing Essences for Units. This mod by itself does not surface the mechanic in-game.

Currently the mod defines essences for the MT1 units, along with Dante, Shield and Spear Steward.

In game, a Toggle Unit Essences button on the deck, draft and logbook screens (also the
Dragon's Hoard key, H by default, or right stick click) switches every unit card between
its own text and its essence text.

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

## Credits
* Amaury-Lepicard for the patches to add the Toggle Unit essences button in various UI screens.