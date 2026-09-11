using BepInEx;
using BepInEx.Logging;
using EssenceData.code;
using HarmonyLib;
using TrainworksReloaded.Base;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace EssenceData
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new(MyPluginInfo.PLUGIN_GUID);
        public static TooltipDesigner.TooltipDesignType Synthesis;
        
        public void Awake()
        {
            Logger = base.Logger;

            var builder = Railhead.GetBuilder();
            builder.Configure(
                MyPluginInfo.PLUGIN_GUID,
                c =>
                {
                    c.AddMergedJsonFile(
                        "json/localizations.json",
                        "json/tooltips.json",
                        "json/unit_synthesis_reward_setup.json",
                        "json/essences/awoken/Animus of Speed.json",
                        "json/essences/awoken/Animus of Will.json",
                        "json/essences/awoken/Awoken Hollow.json",
                        "json/essences/awoken/Edge Prior.json",
                        "json/essences/awoken/Husk Hermit.json",
                        "json/essences/awoken/Shard Channeler.json",
                        "json/essences/awoken/Shattered Shell.json",
                        "json/essences/awoken/Steelsinger.json",
                        "json/essences/awoken/Thorned Hollow.json",
                        "json/essences/awoken/Vinemother.json",
                        "json/essences/awoken/Wildwood Custodian.json",
                        "json/essences/awoken/Wilting Sapwood.json",
                        "json/essences/clanless/Shield Steward.json",
                        "json/essences/clanless/Spear Steward.json",
                        "json/essences/clanless/Dante.json",
                        "json/essences/hellhorned/Alpha Fiend.json",
                        "json/essences/hellhorned/Apex Imp.json",
                        "json/essences/hellhorned/Branded Warrior.json",
                        "json/essences/hellhorned/Consumer of Crowns.json",
                        "json/essences/hellhorned/Demon Fiend.json",
                        "json/essences/hellhorned/Deranged Brute.json",
                        "json/essences/hellhorned/Fledgling Imp.json",
                        "json/essences/hellhorned/Horned Warrior.json",
                        "json/essences/hellhorned/Impish Scholar.json",
                        "json/essences/hellhorned/Molting Imp.json",
                        "json/essences/hellhorned/Pyre Chomper.json",
                        "json/essences/hellhorned/Queen's Impling.json",
                        "json/essences/hellhorned/Railbeater.json",
                        "json/essences/hellhorned/Steelworker.json",
                        "json/essences/hellhorned/Transcendimp.json",
                        "json/essences/hellhorned/Welder Helper.json",
                        "json/essences/remnant/Big Sludge.json",
                        "json/essences/remnant/Bounty Stalker.json",
                        "json/essences/remnant/Devourer of Death.json",
                        "json/essences/remnant/Draff.json",
                        "json/essences/remnant/Dreg.json",
                        "json/essences/remnant/Entombed Explosive.json",
                        "json/essences/remnant/Formless Child.json",
                        "json/essences/remnant/Lady of the House.json",
                        "json/essences/remnant/Lady of the Reformed.json",
                        "json/essences/remnant/Legion of Wax.json",
                        "json/essences/remnant/Molten Encasement.json",
                        "json/essences/remnant/Paraffin Enforcer.json",
                        "json/essences/remnant/Paraffin Thug.json",
                        "json/essences/remnant/Remnant Host.json",
                        "json/essences/remnant/Votivary.json",
                        "json/essences/remnant/Wickless Baron.json",
                        "json/essences/remnant/Wickless Tycoon.json",
                        "json/essences/stygian/Coldcaelia.json",
                        "json/essences/stygian/Cuttlehex.json",
                        "json/essences/stygian/Eel Gorgon.json",
                        "json/essences/stygian/Glacial Seal.json",
                        "json/essences/stygian/Guard of the Unnamed.json",
                        "json/essences/stygian/Guardian Stone.json",
                        "json/essences/stygian/Icy Cilophyte.json",
                        "json/essences/stygian/Lodestone Totem.json",
                        "json/essences/stygian/Molluscmage.json",
                        "json/essences/stygian/Nameless Siren.json",
                        "json/essences/stygian/Offering Monument.json",
                        "json/essences/stygian/Siren of the Sea.json",
                        "json/essences/stygian/Titan Sentry.json",
                        "json/essences/umbra/Alloyed Construct.json",
                        "json/essences/umbra/Antumbra Morsel.json",
                        "json/essences/umbra/Crucible Collector.json",
                        "json/essences/umbra/Crucible Warden.json",
                        "json/essences/umbra/Ember Forge.json",
                        "json/essences/umbra/Magma Morsel.json",
                        "json/essences/umbra/Morsel Excavator.json",
                        "json/essences/umbra/Morsel Jeweler.json",
                        "json/essences/umbra/Morsel Miner.json",
                        "json/essences/umbra/Morsel-Made.json",
                        "json/essences/umbra/Morselmaker.json",
                        "json/essences/umbra/Morselmaster.json",
                        "json/essences/umbra/Overgorger.json",
                        "json/essences/umbra/Rubble Morsel.json",
                        "json/essences/umbra/Shadoweater.json",
                        "json/essences/umbra/Shadowsiege.json",
                        "json/essences/wurmkin/Bog Chrysalis.json",
                        "json/essences/wurmkin/Bog Fly.json",
                        "json/essences/wurmkin/Bog Wurmling.json",
                        "json/essences/wurmkin/Bogdeep Cocoon.json",
                        "json/essences/wurmkin/First of Kin.json",
                        "json/essences/wurmkin/Glareminder.json",
                        "json/essences/wurmkin/Glugsider.json",
                        "json/essences/wurmkin/Keeper of Echoes.json",
                        "json/essences/wurmkin/Kinhost Carapace.json",
                        "json/essences/wurmkin/Kinhost Pupa.json",
                        "json/essences/wurmkin/Kinhost Vessel.json",
                        "json/essences/wurmkin/Shardsoul Carver.json"
                    );
                }
            );

            Railend.ConfigurePostAction(c =>
                {
                    Synthesis = c.GetInstance<IRegister<TooltipDesigner.TooltipDesignType>>().GetValueOrDefault(MyPluginInfo.PLUGIN_GUID.GetId(TemplateConstants.TooltipDesignTypeEnum, "Synthesis"));

                    var reward = c.GetInstance<IRegister<RewardData>>().GetValueOrDefault(MyPluginInfo.PLUGIN_GUID.GetId(TemplateConstants.RewardData, "unit_synthesis_reward")) as UnitSynthesisRewardData;
                    UnitSynthesisRewardData.FilterMask = c.GetInstance<IRegister<CardUpgradeMaskData>>().GetValueOrDefault(MyPluginInfo.PLUGIN_GUID.GetId(TemplateConstants.UpgradeMask, "HasBeenFused"));
                    UnitSynthesisRewardData.fusedUpgrade = c.GetInstance<IRegister<CardUpgradeData>>().GetValueOrDefault(MyPluginInfo.PLUGIN_GUID.GetId(TemplateConstants.Upgrade, "Fusion"));
                }
            );

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }
    }
}
