using Conductor.Extensions;
using EssenceData;
using HarmonyLib;
using ShinyShoe;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using static CardUI;

[HarmonyPatch(typeof(CharacterData), nameof(CharacterData.GetCharacterCardText))]
class CharacterData_GetCharacterCardText_AddFusedMonster_Patch
{
    public static void Postfix(ref string text, CardState cardState)
    {
        var upgrades = cardState?.GetCardStateModifiers().GetCardUpgrades();
        if (upgrades.IsNullOrEmpty()) return;
        foreach (var upgrade in upgrades!)
        {
            if (upgrade.IsEssenceUpgrade())
            {
                var character = upgrade.GetSourceEssenceCharacter();
                var addedText = string.Format("TextFormat_Fused".Localize(), character!.GetName());
                text += $"{Environment.NewLine}{addedText}{Environment.NewLine}";
            }
        }
    }
}

[HarmonyPatch(typeof(CardTooltipContainer), "AddUpgradedCharacterTriggers")]
class CardTooltipContainer_AddUpgradedCharacterTriggers_SynthesisTooltipsPatch
{
    public static void Postfix(CardStateModifiers cardStateModifiers, CardTooltipContainer __instance)
    {
        if (cardStateModifiers == null)
        {
            return;
        }
        foreach (CardUpgradeState cardUpgrade in cardStateModifiers.GetCardUpgrades())
        {
            var sourceCharacter = cardUpgrade.GetSourceEssenceCharacter();
            if (cardUpgrade.IsEssenceUpgrade() && sourceCharacter != null)
            {
                TooltipUI tooltipUI = __instance.InstantiateTooltip("synthesis", Plugin.Synthesis, false);
                string title = string.Format("CardFrameUI_SynthesisTextFormat".Localize(null), sourceCharacter.GetName());
                tooltipUI?.Set(title, cardUpgrade.GetUpgradeDescriptionKey().Localize(new CardEffectLocalizationContext(cardUpgrade.GetSourceCardUpgradeData()!)));
            }
        }
    }
}

[HarmonyPatch(typeof(CardUI), nameof(CardUI.ApplyStateToUI), [typeof(CardState), typeof(CardStatistics), typeof(MonsterManager), typeof(HeroManager), typeof(RelicManager), typeof(SaveManager), typeof(MasteryType), typeof(bool), typeof(bool), typeof(List<CardUpgradeState>), typeof(CardArtPool), typeof(CardEdgeVfxPool)])]
static class CardUI_UpdateTextContent_ShowSynthesisEffectPatch
{
    public static bool EnableShowingSynthesis = false;
    public static bool DisableSynthesisButton = false;

    public static void Postfix(CardUI __instance, CardState cardState, CardFrameUI ____cardFrame, ContentTinter ___cardFrontTinter, TMP_Text ___filteredReasonLabel)
    {
        bool flag = cardState.CurrentDisabledReason == CardState.UpgradeDisabledReason.NONE;
        if (!EnableShowingSynthesis && flag)
        {
            __instance.ResetSyntheisPreview();
        }
        else if (EnableShowingSynthesis && flag)
        {
            __instance.EnableSynthesisPreview(___cardFrontTinter, ___filteredReasonLabel);
        }
    }

    static readonly FieldInfo CardUI_CardFrontTinter = AccessTools.Field(typeof(CardUI), "cardFrontTinter");
    static readonly FieldInfo CardUI_FilteredReasonLabel = AccessTools.Field(typeof(CardUI), "filteredReasonLabel");
    public static void EnableSynthesisPreview(this CardUI cardUI, ContentTinter? cardFrontTinter = null, TMP_Text? filteredReasonLabel = null)
    {
        var cardState = cardUI.GetCardState();
        if (cardState == null)
            return;

        cardFrontTinter ??= CardUI_CardFrontTinter.GetValue(cardUI) as ContentTinter;
        filteredReasonLabel ??= CardUI_FilteredReasonLabel.GetValue(cardUI) as TMP_Text;

        cardFrontTinter!.Toggle(tintOn: true);

        if (!cardState.IsMonsterCard() || cardState.IsChampionCard())
            return;

        var essence = cardState.GetSpawnCharacterData()?.GetEssence();

        string text = essence?.GetUpgradeDescriptionKey()?.Localize(new CardEffectLocalizationContext(essence, null, cardState)) ?? "No essence";
        filteredReasonLabel!.SetText(text);
        filteredReasonLabel.gameObject.SetActive(true);
        filteredReasonLabel.enabled = true;
    }
}

// "Show Essence" toggles every card list between a unit's card text and its essence text. It
// exists in the deck screen (deck, piles, upgrade and purge pickers), the card draft screen and
// the logbook. The toggle is bound to the Dragon's Hoard control (H, or right stick click),
// the same control that toggles the forge. The button added to each screen is bound to that
// control too, so a click and a key press arrive through the same input path. On the deck and
// draft screens the button shows a copy of the settings screen's switch, whose gem lights up
// while essences are shown.
static class UnitEssenceToggle
{
    private const string ButtonName = "ToggleUnitEssencesButton";
    private const string GemName = "UnitEssencesGem";

    private static void ShowState(Component screen, bool enable = true)
    {
        var on = CardUI_UpdateTextContent_ShowSynthesisEffectPatch.EnableShowingSynthesis;
        foreach (var gem in screen.GetComponentsInChildren<Transform>(true).Where(t => t.name == GemName))
        {
            gem.gameObject.SetActive(on && enable);
        }
        foreach (var child in screen.GetComponentsInChildren<Transform>(true).Where(b => b.name == ButtonName))
        {
            var button = child.GetComponent<FilterOptionButton>();
            if (button != null)
                button.SetSelected(on);
            child.gameObject.SetActive(enable);
        }
    }

    private static IEnumerable<MethodBase> Screens(string deck, string draft, string logbook) =>
    [
        AccessTools.Method(typeof(DeckScreen), deck),
        AccessTools.Method(typeof(CardDraftScreen), draft),
        AccessTools.Method(typeof(CompendiumSectionCards), logbook),
    ];

    private static readonly FieldInfo DeckScreen_Mode = AccessTools.Field(typeof(DeckScreen), "mode");

    // The deck screen in ApplyUpgrade mode previews the upgrade on each card, so essences stay off there.
    private static bool ButtonEnabled(MonoBehaviour screen)
    {
        if (CardUI_UpdateTextContent_ShowSynthesisEffectPatch.DisableSynthesisButton)
            return false;
        return screen is not DeckScreen deck || (DeckScreen.Mode)DeckScreen_Mode.GetValue(deck) != DeckScreen.Mode.ApplyUpgrade;
    }

    [HarmonyPatch]
    static class Toggle
    {
        static IEnumerable<MethodBase> TargetMethods() => Screens("ApplyScreenInput", "ApplyScreenInput", "ApplyScreenInput");

        static bool Prefix(MonoBehaviour __instance, InputManager.Controls triggeredMappingID, ref bool __result)
        {
            // Typing "h" into the logbook's search field must not toggle essences.
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (triggeredMappingID != InputManager.Controls.DragonsHoard
                || (selected != null && selected.TryGetComponent<TMP_InputField>(out var field) && field.isFocused)
                || !ButtonEnabled(__instance))
                return true;

            var flag = (CardUI_UpdateTextContent_ShowSynthesisEffectPatch.EnableShowingSynthesis ^= true);
            // The logbook has no cardStatistics field, so this is null there; UpdateTextContent accepts null.
            var stats = Traverse.Create(__instance).Field<CardStatistics>("cardStatistics").Value;
            foreach (var cardUI in __instance.GetComponentsInChildren<CardUI>())
            {
                if (cardUI.GetCardState() != null)
                {
                    if (flag)
                        cardUI.EnableSynthesisPreview();
                    else
                        cardUI.ResetSyntheisPreview();
                }
            }
            ShowState(__instance);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    static class AddButton
    {
        static IEnumerable<MethodBase> TargetMethods() => Screens("Setup", "Setup", "Open");

        // Setup/Open run every time the screen is shown while the button object survives, so only
        // create it the first time. ShowState still runs every time: Close resets the flag, and a
        // fusion may have turned it on.
        static void Postfix(MonoBehaviour __instance)
        {
            if (!__instance.GetComponentsInChildren<GameUISelectableButton>(true).Any(b => b.name == ButtonName))
            {
                Create(__instance);
            }

            Place(__instance);
            ShowState(__instance, ButtonEnabled(__instance));
        }

        // The draft screen's "Choose a card" line sits under its title, so the button goes above
        // the title instead, in the dialog's top frame.
        private const float DraftFrameTop = -85f;

        // Space between the deck screen's banner and the button under it.
        private const float BannerGap = 12f;

        private static void Place(MonoBehaviour screen)
        {
            if (screen.GetComponentsInChildren<GameUISelectableButton>(true).FirstOrDefault(b => b.name == ButtonName)?.transform is not RectTransform rect)
                return;
            if (screen is DeckScreen deck)
            {
                PlaceUnderBanner(rect, Traverse.Create(deck).Field<GameObject>("deckBanner").Value?.transform as RectTransform);
                return;
            }
            rect.anchoredPosition = new Vector2(0f, DraftFrameTop);
        }

        // The deck screen's cards scroll up to the top edge, so the top middle is no place for the
        // button: it goes under the view's banner ("Deck", "Draw Pile", ...), at the left, where
        // the grid never reaches. Every banner sits in the same spot, so the deck banner serves as
        // the reference even when another one, or none (the pickers), is shown. On an ultrawide
        // screen the banner's left end is cut off by the screen edge, so the button lines up with
        // the banner's right edge, which is the end that stays visible.
        private static void PlaceUnderBanner(RectTransform rect, RectTransform? banner)
        {
            if (banner == null)
                return;
            rect.SetParent(banner.parent, false);
            // Same anchor point and pivot as the banner, so its anchoredPosition is directly comparable.
            var anchor = banner.anchorMin + Vector2.Scale(banner.pivot, banner.anchorMax - banner.anchorMin);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = banner.pivot;
            var right = (banner.rect.width - rect.rect.width) * (1f - banner.pivot.x);
            rect.anchoredPosition = banner.anchoredPosition + new Vector2(right, -(banner.rect.height + rect.rect.height) / 2f - BannerGap);
        }

        private static void Create(MonoBehaviour __instance)
        {
            // The logbook gets a filter row instead of a floating button.
            if (__instance is CompendiumSectionCards logbook)
            {
                CreateFilterRow(Traverse.Create(logbook).Field<FilterToolbar>("filterToolbar").Value);
                return;
            }

            var template = Traverse.Create(UnityEngine.Object.FindObjectOfType<DialogScreen>(true))
                .Field("dialogPrefab").Field("button1").GetValue<GameUISelectableButton>();
            if (template == null)
                return;
            var button = UnityEngine.Object.Instantiate(template, __instance.transform);
            button.name = ButtonName;
            Traverse.Create(button).Field("inputType").SetValue((int)new CoreSymbol(nameof(InputManager.Controls.DragonsHoard)).m_value);

            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f); // top middle; Place sets the position
            rect.sizeDelta = new Vector2(255f, 64f);
            if (button.transform.Find("Target Graphic") is RectTransform graphic)
                graphic.sizeDelta = new Vector2(graphic.sizeDelta.x, 80f); // 104 by default

            // The button has two texts: the key hint, filled by the game from inputType, and the
            // label. Only the label is ours to set.
            var label = button.GetComponentsInChildren<TMP_Text>(true).First(text => text.GetComponentInParent<GameUIControlMapping>() == null);
            label.SetLocKey("DeckScreen_ShowEssence");
            // The dialog button's label auto-sizes between 24 and 40 pt on a single line. At full
            // size it overflows this smaller button, so scale both bounds down. Translations too
            // long for one line carry an explicit line break in localizations.json.
            label.fontSizeMin *= 0.8f;
            label.fontSizeMax *= 0.8f;
            label.margin = new Vector4(8f, 0f, 60f, 0f); // clear of the lozenge

            // Copy the settings screen's switch handle (the lozenge with a gem) into the right side
            // of the button. In the settings screen the switch's own component lights the gem; the
            // copy has no such component, so ShowState toggles the gem object directly.
            var handle = Traverse.Create(UnityEngine.Object.FindObjectOfType<SettingsDialog>(true))
                .Field("backgroundMuteToggle").Field("handle").GetValue<RectTransform>();
            if (handle == null)
                return;
            var lozenge = UnityEngine.Object.Instantiate(handle, button.transform);
            lozenge.anchorMin = lozenge.anchorMax = lozenge.pivot = new Vector2(1f, 0.5f);
            lozenge.anchoredPosition = new Vector2(-20f, 0f);
            lozenge.sizeDelta = new Vector2(56f, 56f);
            // The gem is nested under the frame image, so transform.Find (one level only) would miss it.
            lozenge.GetComponentsInChildren<Transform>(true).First(t => t.name == "Image toggle on").name = GemName;
        }

        // The logbook's filter sidebar gets one more row, between Mastery and Search. It is a copy
        // of the Cost row (a title above a row of option buttons) reduced to a single wide button,
        // so it looks like the other filters and the sidebar's layout group positions it with
        // them. The button is drawn as a selected filter while essences are shown.
        private static void CreateFilterRow(FilterToolbar toolbar)
        {
            var fields = Traverse.Create(toolbar);
            var cost = fields.Field<OptionsFilterUI>("costFilterUI").Value;
            var search = fields.Field<SearchFilterUI>("searchFilterUI").Value;
            var content = (RectTransform)search.transform.parent;
            var row = UnityEngine.Object.Instantiate(cost.gameObject, content);
            row.name = "UnitEssencesFilter";
            row.transform.SetSiblingIndex(search.transform.GetSiblingIndex());
            // Remove the copied filter component. The toolbar only knows filters through its own
            // serialized fields, so the copy would never be read, but it would still handle clicks
            // on our button as a cost filter.
            UnityEngine.Object.DestroyImmediate(row.GetComponent<OptionsFilterUI>());
            // Retitle the row. Its title is the only text not inside an option button.
            row.GetComponentsInChildren<TMP_Text>(true).First(t => t.GetComponentInParent<FilterOptionButton>() == null).SetLocKey("EssenceData_Essences");

            var options = row.GetComponentsInChildren<FilterOptionButton>(true);
            foreach (var extra in options.Skip(1))
                UnityEngine.Object.DestroyImmediate(extra.gameObject);
            var option = options[0];
            option.name = ButtonName;
            option.Set(new OptionsFilter.OptionDisplay("LogBook_ShowEssence"));
            Traverse.Create(option.Button).Field("inputType").SetValue((int)new CoreSymbol(nameof(InputManager.Controls.DragonsHoard)).m_value);

            // The Cost row lays its buttons out in a grid of equal fixed cells. Widen the one
            // remaining cell to the Mastery dropdown's width (forcing a layout pass first: nothing
            // has a size yet while the screen opens) and keep one column, so the grid stays as wide
            // as the sidebar. The button's frame images have point anchors, not stretched ones, so
            // widen them too.
            if (option.transform.parent.GetComponent<UnityEngine.UI.GridLayoutGroup>() is { } grid)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                var dropdown = fields.Field<DropdownFilterUI>("masteryFilterUI").Value.GetComponentInChildren<GameUISelectableDropdown>(true).transform as RectTransform;
                var widened = (dropdown?.rect.width > 0f ? dropdown.rect.width : grid.cellSize.x * 6f + grid.spacing.x * 5f) - grid.cellSize.x;
                grid.cellSize += new Vector2(widened, 0f);
                grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
                foreach (var child in option.GetComponentsInChildren<RectTransform>(true).Where(r => r != option.transform && r.anchorMin.x == r.anchorMax.x))
                    child.sizeDelta += new Vector2(widened, 0f);
            }
            foreach (var text in option.GetComponentsInChildren<TMP_Text>(true))
            {
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = text.fontSize * 0.5f;
            }

            // The sidebar is laid out for six rows. To fit a seventh when the clan grid
            // has three rows: tighten the row spacing, move the whole block up into the gap below
            // the Back button, and move the bottom ornament down. Offsets tuned at 1920x1080.
            if (content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() is { } layout)
                layout.spacing = Mathf.Min(layout.spacing, 4f);
            content.anchoredPosition += new Vector2(0f, 5f);
            if (content.Find("Bottom divider") is RectTransform divider)
                divider.anchoredPosition -= new Vector2(0f, 10f);
        }
    }

    // The flag is global. Left on after the screen closes, it would also rewrite the cards in hand.
    [HarmonyPatch]
    static class Reset
    {
        static IEnumerable<MethodBase> TargetMethods() => Screens("Close", "Close", "Close");

        static void Postfix()
        {
            CardUI_UpdateTextContent_ShowSynthesisEffectPatch.EnableShowingSynthesis = false;
            CardUI_UpdateTextContent_ShowSynthesisEffectPatch.DisableSynthesisButton = false;
        }
    }
}

[HarmonyPatch(typeof(CardState), "SetupBodyUpgradeText")]
public static class SetupBodyUpgradeText_Patch
{
    public static bool ExcludeUpgrade(CardUpgradeState cardUpgrade)
    {
        // Return true to skip/continue, false to process normally
        return cardUpgrade.IsEssenceUpgrade();
    }

    private static readonly MethodInfo FilterMethod = AccessTools.Method(
        typeof(SetupBodyUpgradeText_Patch),
        nameof(ExcludeUpgrade)
    );

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var matcher = new CodeMatcher(instructions, il);

        // 1. Locate the MoveNext call (supports both List<T>.Enumerator and interface calls)
        matcher.MatchForward(false,
            new CodeMatch(i => (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) &&
                               i.operand?.ToString().Contains("MoveNext") == true)
        );

        if (matcher.IsInvalid)
        {
            Plugin.Logger.LogError("[SetupBodyUpgradeText_Patch] Failed to locate MoveNext call. Patch needs to be redone.");
            return instructions;
        }

        // Attach a label to the MoveNext instruction so 'continue' jumps straight to it
        Label loopContinueLabel = il.DefineLabel();
        matcher.Instruction.labels.Add(loopContinueLabel);

        // 2. Go back and find get_Current followed by the local variable assignment (stloc)
        matcher.Start();
        matcher.MatchForward(false,
            new CodeMatch(i => (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) &&
                               i.operand?.ToString().Contains("get_Current") == true),
            new CodeMatch(i => i.IsStloc())
        );

        if (matcher.IsInvalid)
        {
            Plugin.Logger.LogError("[SetupBodyUpgradeText_Patch] Failed to locate get_Current assignment. Patch needs to be redone");
            return instructions;
        }

        // Move to the stloc instruction
        matcher.Advance(1);
        var stlocInstruction = matcher.Instruction;
        var ldlocInstruction = ConvertStlocToLdloc(stlocInstruction);

        // Move past stloc to insert the filter
        matcher.Advance(1);

        // Inject: ldloc <cardUpgrade> -> call FilterMethod -> brtrue <loopContinueLabel>
        matcher.Insert(
            ldlocInstruction,
            new CodeInstruction(OpCodes.Call, FilterMethod),
            new CodeInstruction(OpCodes.Brtrue, loopContinueLabel)
        );

        return matcher.InstructionEnumeration();
    }

    private static CodeInstruction ConvertStlocToLdloc(CodeInstruction stloc)
    {
        if (stloc.opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
        if (stloc.opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
        if (stloc.opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
        if (stloc.opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
        if (stloc.opcode == OpCodes.Stloc_S) return new CodeInstruction(OpCodes.Ldloc_S, stloc.operand);
        if (stloc.opcode == OpCodes.Stloc) return new CodeInstruction(OpCodes.Ldloc, stloc.operand);

        throw new InvalidOperationException($"Unexpected store local opcode: {stloc.opcode}");
    }
}
