using Vintagestory.API.Server;

[Terrain("caldera")]
public class Caldera : TerrainType
{
    public Caldera(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        Module grad = new RadialGradient(1000);
        module = grad.SpecialMarker(0.95).LinearCurve().CP(0, 0).CP(0.8, 1).CP(0.90, 0.70).CP(0.95, 0.65).CP(0.96, 0.25).CP(1, 0.25);

        Module cellMask = new Noise(seed, 0.0032f, 2).Cellular().Dist2Sub().Ridged().Alpha(0.1);
        module = module.Multiply(cellMask);

        Module noiseMask = new Noise(seed + 2, 0.002f, 5, 0.4f, 2.6f).Alpha(0.1);
        module = module.Multiply(noiseMask);

        module = module.Scale(1.1);

        Module valleyFloor = new Noise(seed + 4, 0.001f, 5, 0.4f, 2.6f).Scale(0.2);
        module = module.Max(valleyFloor);

        //module = module.DomainWarp(100, seed + 10, 0.001f, 4, 0.4f, 2.6f);
    }
}