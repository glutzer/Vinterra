using System;
using Vintagestory.API.MathTools;

/// <summary>
/// Noise for determining continent shape.
/// </summary>
public class ContinentNoise
{
    public Noise continentalnessNoise; //Continent shape noise
    public Noise overlayNoise; //Small overlay noise for beaches / breakup

    public SmoothCurve continentCurve;

    public int plateStartX;
    public int plateStartZ;

    public int plateSize;
    public int halfPlateSize;

    public Vec2d globalPlateCenterPosition;

    public float overlayWeight = 0.05f;

    public LinearCurve oceanCurve;

    public ContinentNoise(int seed, int plateSize)
    {
        halfPlateSize = plateSize / 2;

        continentalnessNoise = new Noise(seed, 0.000035f, 4, 0.5f, 2.6f);
        overlayNoise = new Noise(seed + 512, 0.001f, 2, 0.4f, 2.6f);

        continentCurve = new SmoothCurve();
        continentCurve.CP(0, -0.2) //Continentalness of deepest ocean

                      .CP(0.17, 0) //Land end
                      .CP(0.18, 1 - VinterraMod.GetFactorFromBlocks(160))
                      .CP(0.19, 1 - VinterraMod.GetFactorFromBlocks(80))
                      .CP(0.20, 1 - VinterraMod.GetFactorFromBlocks(40))
                      .CP(0.21, 1 - VinterraMod.GetFactorFromBlocks(20))
                      .CP(0.22, 1 - VinterraMod.GetFactorFromBlocks(5))
                      .CP(0.23, 1) //Land stabilize

                      .CP(1, 1); //Multiplier at max, disabled for now

        //---------- Ocean curve
        oceanCurve = new LinearCurve();

        oceanCurve.CP(-2, -VinterraMod.GetFactorFromBlocks(50)) //At -1 depth, it will be a depth of 50
                  .CP(-VinterraMod.GetFactorFromBlocks(5), -VinterraMod.GetFactorFromBlocks(7)) //At a depth of 1 block, it will be 2 blocks, at a depth of 5 blocks, it will be 10 blocks
                  .CP(0, 0); //Sea level
    }

    public double SquareDropoff(Vec2d position)
    {
        double xDist = Math.Abs(globalPlateCenterPosition.X - position.X);
        double zDist = Math.Abs(globalPlateCenterPosition.Y - position.Y);

        double distFactor = Math.Max(xDist, zDist) / halfPlateSize;

        return 1 - Math.Clamp(distFactor, 0, 1);
    }

    /// <summary>
    /// Takes a global position and calculates continent noise.
    /// </summary>
    public double GetContinentNoise(Vec2d position)
    {
        double sample = (continentalnessNoise.GetPosNoise(position.X, position.Y) + overlayNoise.GetPosNoise(position.X, position.Y) * overlayWeight) * SquareDropoff(position);
        return continentCurve.Get(sample);
    }
}