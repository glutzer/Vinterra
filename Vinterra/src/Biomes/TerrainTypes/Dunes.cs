using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[Terrain("dunes")]
public class Dunes : TerrainType
{
    public Dunes(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        //Make dunes
        Module voronoi = new Noise(seed, 0.0064f, 1, 0.5f, 2).Cellular().Ridged().DomainWarp(300, seed + 2, 0.0004f, 4, 0.4f, 3).Scale(0.3f * frequencyMultiplier);
        Module mask = new Noise(seed + 4, 0.0008f, 4, 0.4f, 2.6f);
        voronoi = voronoi.Multiply(mask);

        //Make rocks, make it use the max of dunes and itself
        Module rockNoise = new Noise(seed + 6, 0.0016f, 5, 0.4f, 2.6f).LinearCurve().CP(0, 0).CP(0.7, 0).CP(0.8, 0.2).CP(1, 0.4).Scale(GameMath.Lerp(frequencyMultiplier, 1, 0.5f)).Max(voronoi);

        //River
        module = rockNoise.RiverMaskSquared(100, 2, 1, -15, 0.005f);
    }
}