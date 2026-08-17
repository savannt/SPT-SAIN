using System.Threading;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace SAIN.Server;

[Injectable(InjectionType.Singleton)]
public sealed class ModEntry(
    ISptLogger<ModEntry> logger,
    PmcConfig pmcConfig,
    BotConfig botConfig,
    LocationTable locationTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ForceSingleBrain(pmcConfig.PmcType, "pmcBot");
        ForceSingleBrain(botConfig.AssaultBrainType, "assault");
        ForceSingleBrain(botConfig.PlayerScavBrainType, "pmcBot");
        NormalizeBotLocationModifiers(locationTable);

        logger.Success("[SAIN] Server config patch applied for SPT 4.1.2.", null);
        return Task.CompletedTask;
    }

    private static void ForceSingleBrain(Dictionary<string, Dictionary<string, Dictionary<string, double>>> maps, string allowedBrain)
    {
        foreach (var map in maps.Values)
        {
            foreach (var side in map.Values)
            {
                foreach (var brain in side.Keys.ToList())
                {
                    side[brain] = string.Equals(brain, allowedBrain, StringComparison.OrdinalIgnoreCase) ? 1d : 0d;
                }
            }
        }
    }

    private static void ForceSingleBrain(Dictionary<string, Dictionary<string, int>> maps, string allowedBrain)
    {
        foreach (var brains in maps.Values)
        {
            foreach (var brain in brains.Keys.ToList())
            {
                brains[brain] = string.Equals(brain, allowedBrain, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
        }
    }

    private static void NormalizeBotLocationModifiers(LocationTable locationTable)
    {
        foreach (var location in locationTable.GetDictionary().Values)
        {
            var modifier = location.Base?.BotLocationModifier;
            if (modifier == null)
            {
                continue;
            }

            modifier.AccuracySpeed = 1d;
            modifier.GainSight = 1d;
            modifier.Scattering = 1d;
            modifier.VisibleDistance = 1d;
        }
    }
}
