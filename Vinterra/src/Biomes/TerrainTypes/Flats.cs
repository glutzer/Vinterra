using Vintagestory.API.Server;

[Terrain("flats")]
public class Flats : TerrainType
{
    public Flats(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        Module noise = new Noise(seed, 0.01f, 2).LinearCurve().CP(0, 0).CP(1, 0).RiverMaskSquared(30, 3, 0, -10).RiverBump(50, 0.015f);
        module = noise;
    }
}