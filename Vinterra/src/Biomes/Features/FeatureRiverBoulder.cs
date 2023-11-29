using Vintagestory.API.Common;
using Vintagestory.API.Server;

[Feature("riverboulder")]
public class FeatureRiverBoulder : FeatureBoulder
{
    public FeatureRiverBoulder(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override bool CanGenerate(int localX, int localZ, GenerationData data)
    {
        return data.riverDistance[localX, localZ] < 10 && data.riverDistance[localX, localZ] > 1;
    }
}