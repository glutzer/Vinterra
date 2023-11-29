using System;
using Vintagestory.API.Server;

[Terrain("ridgedmountain")]
public class RidgedMountain : TerrainType
{
    public RidgedMountain(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        //V1, pretty realistic
        //Ridge
        Module ridge = new Noise(seed, 0.0008f * frequencyMultiplier, 3, 0.2f).Ridged().OpenSimplex2S();
        ridge = ridge.LinearCurve().CP(0, 0).CP(0.3, 0.1).CP(0.5, 0.5).CP(0.8, 0.95).CP(1, 1).Power(1.2f);

        ridge = ridge.Terrace(6);

        //Extremely large curve on ridge
        module = ridge.RiverMaskSquared(Math.Min((int)(200 * strengthMultiplier), 500), 1, 0.1f, 0, 0.001f * frequencyMultiplier);

        //0-1, does not need scaling
        Module plates = new Noise(seed + 2, 0.0016f, 1).Cellular().Ridged().Dist2Sub().Jitter(0.8f).Alpha(0.3);
        module = module.Multiply(plates);

        //Rocks which get larger on top of the ridge
        Module breakup = new Noise(seed + 4, 0.0024f, 5).Alpha(0.4);
        Module rocks = new Noise(seed + 2, 0.0064f, 1).Cellular().Ridged().Dist2Sub().Power(1.3f).Scale(0.3).Multiply(ridge).RiverMaskSquared(50, 1.5f);
        rocks = rocks.Multiply(breakup);
        module = module.Add(rocks); //Adds in area where

        module = module.DomainWarp(50, seed + 20, 0.002f, 3, 0.4f, 2.2f);

        //module = module.Scale(1);
    }
}