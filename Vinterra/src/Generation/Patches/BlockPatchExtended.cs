using Newtonsoft.Json;
using Vintagestory.ServerMods.NoObf;

public class BlockPatchExtended : BlockPatch
{
    [JsonProperty]
    public bool nearRivers = false;

    [JsonProperty]
    public double riverDistance = 50;

    [JsonProperty]
    public bool onBeaches = false;

    [JsonProperty]
    public string[] tags = new string[] { "global" };
}