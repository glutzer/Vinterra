using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[Terrain("mountain")]
public class Mountain : TerrainType
{
    public Mountain(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        /*
        RidgedNoise ridged = new RidgedNoise(seed, 0.0008f, 5, 0.3f, 2.6f).Perlin();
        WarpedNoise warp = new WarpedNoise(ridged, 100, seed + 6, 0.0004f, 4, 0.5f, 2.6f);

        

        //Curve warped ridge
        LinearCurve curve = new LinearCurve(warp).AddControlPoint(0, 0).AddControlPoint(0.7, 0.3).AddControlPoint(1, 1);

        RidgedNoise billow = new RidgedNoise(seed + 4, 0.0008f, 5, 0.4f, 3).Perlin();
        WarpedNoise billowWarp = new WarpedNoise(billow, 100, seed + 6, 0.0004f, 4, 0.5f, 2.6f);
        Invert invert = new Invert(billowWarp);

        Simplex2Noise simplex = new Simplex2Noise(seed + 10, 0.0008f, 5, 0.3f, 3);

        Max max = new Max(invert, simplex);

        Multiply multiply = new Multiply(max, new RiverMaskSquared(curve, 150, 2, 0.4f, -10, 0.005f));

        //LinearCurve curveDown = new LinearCurve(multiply).AddControlPoint(0, 0).AddControlPoint(0.5, 0.5).AddControlPoint(1, 0.25);

        Scale scale = new Scale(multiply, 1.3);

        module = scale;
        */

        //Make an f1-f2 voronoi, reduce jitter to make the heights more consistent, then scale it to a height of up to 1. This now ranges from 0-1
        Module voronoi = new Noise(seed, 0.0008f, 3, 0.35f, 2).Cellular().Dist2Sub().Jitter(0.5f).Scale(2.3);

        //Curve it
        //voronoi = voronoi.LinearCurve().CP(0, 0).CP(0.3, 0.3).CP(1, 1);

        //Add spikes at increased frequency, modulated by spike noise
        Module spikeNoise = new Noise(seed + 4, 0.0008f, 2).Alpha(0.4f);
        Module spikes = new Noise(seed + 2, 0.004f, 3, 0.4f, 2.2f).Cellular().Dist2Sub().Scale(0.35).Multiply(spikeNoise);
        voronoi = voronoi.Add(spikes);

        Module breakup = new Noise(seed + 20, 0.008f, 4, 0.4f, 2).Alpha(0.1f);
        voronoi = voronoi.Multiply(breakup);

        //Add a small curve to it
        voronoi = voronoi.Power(1.1f).DomainWarp(100, seed + 2, 0.0004f, 4, 0.4f, 3).RiverMaskSquared(250, 1.5f, 0.4f, -15, 0.005f);

        module = voronoi;

        /*
        Module cellBlur = new Noise(seed, 0.0016f, 2, 0.4f, 2.6f).Cellular().Alpha(0);

        voronoi = voronoi.Multiply(cellBlur);

        Module blur = new Noise(seed + 4, 0.0064f, 5, 0.4f, 2.6f).Alpha(0);

        //Dont power yet
        Module mountains = voronoi.Multiply(blur).Power(1f).DomainWarp(100, seed + 2, 0.0004f, 4, 0.4f, 3);

        Module valleyFloor = new Noise(seed + 10, 0.0008f, 4, 0.4f, 2.6f).Scale(0.2).Max(mountains);

        module = valleyFloor.RiverMaskSquared(300, 2, 0.4f, -10, 0.002f);
        */




        /*
        Noise ridged = new Noise(seed, 0.0008f * frequencyMultiplier, 5, 0.3f, 2.6f).Perlin().Ridged();
        DomainWarp warp = new(ridged, 100 * strengthMultiplier, seed + 6, 0.0004f * frequencyMultiplier, 4, 0.5f, 2.6f);

        //Curve warped ridge
        LinearCurve curve = new LinearCurve(warp).CP(0, 0).CP(0.7, 0.3).CP(1, 1);

        Noise billow = new Noise(seed + 4, 0.0008f * frequencyMultiplier, 5, 0.4f, 3).Perlin().Ridged();
        DomainWarp billowWarp = new(billow, 100 * strengthMultiplier, seed + 6, 0.0004f * frequencyMultiplier, 4, 0.5f, 2.6f);
        Invert invert = new(billowWarp);

        Noise simplex = new(seed + 10, 0.0008f * frequencyMultiplier, 5, 0.3f, 3);

        Max max = new(invert, simplex);

        Multiply multiply = new(max, new RiverMaskSquared(curve, 300, 2, 0.4f, -10, 0.005f));

        //LinearCurve curveDown = new LinearCurve(multiply).AddControlPoint(0, 0).AddControlPoint(0.5, 0.5).AddControlPoint(1, 0.25);

        Scale scale = new(multiply, 1.3);

        module = scale;
        */
    }
}


/*
 * Mountains 2 from tf
 * VoronoiNoise voronoi = new VoronoiNoise(seed, 0.0016f, 2, 0.4f, 2, FastNoise.CellularReturnType.Distance2).Ridged();
        WarpedNoise warpedNoise = new WarpedNoise(voronoi, 200, seed + 6, 0.0008f, 4, 0.4f, 3);

        Simplex2Noise alphaNoise = new Simplex2Noise(seed, 0.012f, 5, 0.4f, 3);
        Alpha blur = new Alpha(alphaNoise, 0.05f);

        RidgedNoise surface = new RidgedNoise(seed + 4, 0.0008f, 4, 0.4f, 2.6f).Smooth();
        Alpha surfaceBlur = new Alpha(surface, 0.35f);

        Multiply multiply = new Multiply(warpedNoise, blur);
        Multiply surfaceMultiply = new Multiply(multiply, surfaceBlur);

        Power power = new Power(surfaceMultiply, 1.2f);

        RiverMaskSquared mask = new(power, 150, 2, 0.4f, -10);

        module = mask;
/*
 * //Make a ridge and warp it
        RidgedNoise ridged = new RidgedNoise(seed, 0.0008f, 4, 0.3f, 2.6f);
        WarpedNoise warp = new WarpedNoise(ridged, 150, seed, 0.0004f, 4, 0.4f, 3);

        VoronoiNoise voronoi = new VoronoiNoise(seed, 0.0032f, 1);
        Add add = new Add(warp, voronoi, 0.2f);

        Scale scale = new Scale(add, 0.7f);

        Simplex2Noise noise = new Simplex2Noise(seed + 2, 0.0008f, 5, 0.4f, 2.6f);
        Alpha alpha = new Alpha(noise, 0.1);

        Multiply multiply = new Multiply(scale, alpha);

        RiverMaskSquared river = new RiverMaskSquared(multiply, 200, 2, 0.2f, -15, 0.01f);
        module = river;
*/