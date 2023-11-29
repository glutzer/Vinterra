using Vintagestory.API.Server;

[Terrain("jaggedplateau")]
public class JaggedPlateau : TerrainType
{
    public JaggedPlateau(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        //Make spiky mountains
        Module voronoi = new Noise(seed, 0.0016f, 2).Cellular().Dist2Sub();

        DomainWarp warpedVoronoi = new(voronoi, 300, seed, 0.00032f, 4, 0.4f, 3.5f);

        Noise simplex = new(seed, 0.0008f, 6, 0.3f, 3.0f);

        Add add = new(warpedVoronoi, simplex);

        Scale scale = new(add, 0.75f);

        LinearCurve curve = new(scale);
        curve.CP(0, 0.2)
            .CP(0.18, 0.46)
            .CP(0.44, 0.58)
            .CP(0.72, 0.75)
            .CP(1, 0.95);

        Gain gain = new(curve, 1.3);

        //Make ridge
        Noise ridge = new(seed, 0.0008f, 1);
        ridge = ridge.Ridged();

        DomainWarp warpedRidge = new(ridge, 100, seed, 0.0008f, 3, 0.4f, 4);

        SmoothCurve ridgeCurve = new(warpedRidge);
        ridgeCurve.CP(0, 0)
            .CP(0.75, 0.35)
            .CP(0.92, 0.84)
            .CP(1, 0.9);

        Invert invert = new(ridgeCurve);

        //Multiply 2 together
        //Min min = new Min(gain, invert);

        Power power = new(gain, 2);

        RiverMaskSquared mask = new(power, 100, 2, 0.4f, -5, 0.01f);

        module = mask;
    }
}