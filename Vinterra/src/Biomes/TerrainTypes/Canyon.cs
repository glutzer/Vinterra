using Vintagestory.API.Server;

[Terrain("canyon")]
public class Canyon : TerrainType
{
    public Canyon(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        Noise simplex = new(seed, 0.002f, 5);

        RiverMaskLinear riverMask = new(simplex, 100);

        LinearCurve curves = new(riverMask);

        curves.CP(0, 0).CP(0.45, 0.042).CP(0.47, 0.15).CP(0.61, 0.16).CP(0.62, 0.27).CP(0.76, 0.29).CP(0.77, 0.42).CP(1, 0.45);

        Terrace terrace = new(curves, 15);

        module = terrace;
    }
}