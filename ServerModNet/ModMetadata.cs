using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SAIN.Server;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "me.sol.sain";
    public string Name { get; init; } = "SAIN";
    public string Author { get; init; } = "zSolarint";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.3.0-spt412-local", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Solarint/SAIN";
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}
