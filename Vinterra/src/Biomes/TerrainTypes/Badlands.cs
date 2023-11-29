using Vintagestory.API.Server;

[Terrain("badlands")]
public class Badlands : TerrainType
{
    public Badlands(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        Module original = new Noise(seed, 0.0008f, 5).DomainWarp(400, seed, 0.0008f, 5, 0.35f, 2.8f);

        Module ridge = new Noise(seed, 0.0004f, 1).Ridged().SmoothCurve().CP(0.8, 0).CP(0.88, 0.38).CP(1, 0.5).Scale(0.65);

        module = original.Subtract(ridge).RiverMaskSquared(100, 2, 1, -10).Terrace(20).Scale(0.5);
    }
}