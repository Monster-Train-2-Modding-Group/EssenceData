using Conductor.Extensions;
using EssenceData;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

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

[HarmonyPatch(typeof(CardUI), nameof(CardUI.UpdateTextContent))]
class CardUI_UpdateTextContent_ShowSynthesisEffectPatch
{
    public static bool EnableShowingSynthesis = false;
    public static bool Prefix(CardState cardState, CardFrameUI ____cardFrame)
    {
        if (!EnableShowingSynthesis || !cardState.IsMonsterCard())
            return true;

        var essence = cardState.GetSpawnCharacterData()?.GetEssence();

        cardState.GetCardTypeCardText(out string outCardText);
        string text = essence?.GetUpgradeDescriptionKey()?.Localize(new CardEffectLocalizationContext(essence, null, cardState)) ?? "No essence";
        text = $"Effect: {text}";
        ____cardFrame.SetTextContent(cardState.GetCardType(), cardState.GetTitle(), text, outCardText);
        return false;
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
            UnityEngine.Debug.LogError("[SetupBodyUpgradeText_Patch] Still failed to locate MoveNext call. Dumping opcodes for debugging.");
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
            UnityEngine.Debug.LogError("[SetupBodyUpgradeText_Patch] Failed to locate get_Current assignment.");
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