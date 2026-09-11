using Conductor.Data.Registers;
using Conductor.Extensions;
using ShinyShoe.Analytics;
using ShinyShoe.Logging;
using System.Collections;
using UnityEngine;
using static ChampionUpgradeScreen;
using static GrantableRewardData;

namespace EssenceData.code
{
    public sealed class UnitSynthesisRewardData : GrantableRewardData
    {
        public readonly int numCardsToSelect = 2;

        [SerializeField]
        public static CardUpgradeMaskData? FilterMask;

        [SerializeField]
        public static CardUpgradeData? fusedUpgrade;

        [SerializeField]
        [LocalizedTextKey(false)]
        public string? purgeCardTitleKey = "UnitSynthesisRewardData_purgeTitleKey";

        [SerializeField]
        [LocalizedTextKey(false)]
        public string? upgradeUnitTitleKey = "UnitSynthesisRewardData_upgradeUnitTitleKey";

        private List<CardState> cardsChosen = [];

        public override InteractionType interactionType => InteractionType.Targeted;

        public UnitSynthesisRewardData()
        {
            _rewardTitleKey = "UnitSynthesisRewardData_rewardTitleKey";
            _rewardDescriptionKey = "UnitSynthesisRewardData_rewardDescriptionKey";
        }

        public override void GrantReward(GrantParams grantParams)
        {
            grantParams.coreGameManagers.GetSaveManager().StartCoroutine(ShowDeckSelection(grantParams));
        }

        private IEnumerator ShowDeckSelection(GrantParams grantParams)
        {
            cardsChosen.Clear();
            for (int i = 0; i < numCardsToSelect; i++)
            {
                string instructionContent = purgeCardTitleKey.Localize();
                DeckScreen.FilterCardStateDelegate? func = OnlyUnfusedMonstersWithEssences;
                CardUpgradeData? upgrade = null;
                CardUI_UpdateTextContent_ShowSynthesisEffectPatch.EnableShowingSynthesis = true;
                if (i > 0)
                {
                    CardUI_UpdateTextContent_ShowSynthesisEffectPatch.EnableShowingSynthesis = false;
                    upgrade = cardsChosen[i - 1].GetSpawnCharacterData()!.GetEssence()!;
                    instructionContent = string.Format(upgradeUnitTitleKey.Localize(), upgrade.GetUpgradeDescriptionKey()?.Localize(new CardEffectLocalizationContext(upgrade, null, cardsChosen[i - 1])) ?? "");
                    func = null /*OnlyUnfusedMonsters FilterFunc handles this*/;
                }
                bool cardChosen = false;
                bool interactionCompleted = false;
                ref Action interactionCompleteCallback = ref grantParams.interactionCompleteCallback;
                interactionCompleteCallback = (Action)Delegate.Combine(interactionCompleteCallback, (Action)delegate
                {
                    interactionCompleted = true;
                });
                DoUIScreenConfirmationFlow(ScreenName.Deck, grantParams, delegate (IScreen screen, ScreenSetupConfirmationDelegate screenSetupConfirmation)
                {
                    DeckScreen obj = (screen as DeckScreen)!;
                    obj.Setup(new DeckScreen.Params
                    {
                        mode = DeckScreen.Mode.SpellMergeSelection,
                        showCancel = true,
                        rewardSource = grantParams.source,
                        cardUpgradeMaskData = FilterMask,
                        cardTypeFilter = CardType.Monster,
                        filterFunc = func,
                        totalUses = grantParams.correspondingReward.TotalUses,
                        sourceRewardState = grantParams.correspondingReward,
                        forceExcludeCard = ((i > 0) ? cardsChosen[0] : null),
                        numDeckScreensNeededAfterThis = numCardsToSelect - i - 1,
                        titleKey = _rewardTitleKey,
                        overrideInstructionContent = instructionContent,
                        cardUpgradeData = upgrade,
                        ignoreUpgradeLimit = true
                    });
                    obj.AddDeckScreenCardStateChosenDelegate(delegate (CardState returnCardState)
                    {
                        if (returnCardState != null)
                        {
                            cardsChosen.Add(returnCardState);
                            cardChosen = true;
                        }
                    });
                });
                yield return new WaitWhile(() => !interactionCompleted);
                if (!cardChosen)
                {
                    break;
                }
            }
            if (cardsChosen.Count == numCardsToSelect)
            {
                InfuseCards(grantParams.coreGameManagers.GetSaveManager());
                grantParams.rewardGrantedCallback?.Invoke(default(GrantResult));
                grantParams.correspondingReward.ClaimReward(increment: false);
                Log.Info(LogGroups.Gameplay, "Granted UnitSynthesisRewardData reward");
            }
            else
            {
                Log.Info(LogGroups.Gameplay, "Cancelled UnitSynthesisRewardData");
            }
        }

        private void InfuseCards(SaveManager saveManager)
        {
            var cardToPurge = cardsChosen[0];
            var upgradeData = cardToPurge.GetSpawnCharacterData()!.GetEssence()!;
            var cardToUpgrade = cardsChosen[1];
            CardUpgradeState cardUpgradeState = new();
            cardUpgradeState.Setup(upgradeData);
            cardToUpgrade.ApplyPermanentUpgrade(cardUpgradeState, saveManager, true);
            cardUpgradeState = new();
            cardUpgradeState.Setup(fusedUpgrade!);
            cardToUpgrade.ApplyPermanentUpgrade(cardUpgradeState, saveManager, true);
            saveManager.RemoveCardFromDeck(cardToPurge);
            saveManager.ShowCardAddedAnimation(cardToUpgrade);
        }

        private bool OnlyUnfusedMonstersWithEssences(CardState cardState)
        {
            if (!cardState.IsMonsterCard()) return true;
            var characterData = cardState.GetSpawnCharacterData();
            if (characterData == null) return true;
            return characterData.GetEssence() == null;
        }
    }
}