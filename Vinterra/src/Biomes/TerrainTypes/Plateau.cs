using Vintagestory.API.Server;

[Terrain("plateau")]
public class Plateau : TerrainType
{
    public Plateau(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        Noise original = new(seed, 0.0008f, 1);

        DomainWarp warp = new(original, 400, seed, 0.0002f, 4, 0.4f, 2.6f);

        Noise simplex = new(seed + 24, 0.0016f, 4, 0.4f, 2.6f);

        Add add = new(warp, new Scale(simplex, 0.2));

        SmoothCurve curve = new(add);
        curve.CP(0, 0)
            .CP(0.66, 0)
            .CP(0.87, 0.18)
            .CP(0.92, 0.4)
            .CP(1, 0.44);

        Scale scale = new(curve, 0.5f);

        module = scale;
    }
}