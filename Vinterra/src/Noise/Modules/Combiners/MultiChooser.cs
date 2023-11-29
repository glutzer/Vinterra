using Vintagestory.API.MathTools;

/// <summary>
/// Blends multiple terrains together within a terrain.
/// </summary>
public class MultiChooser : Module
{
    readonly Module module;
    TerrainType[] terrains;
    float[] thresholds;
    int points;

    //Returns the min of 2 values
    public MultiChooser(Module module)
    {
        this.module = module;
    }

    public MultiChooser AddTerrains(params TerrainType[] terrains)
    {
        this.terrains = terrains;
        return this;
    }

    public MultiChooser AddThresholds(params float[] thresholds)
    {
        this.thresholds = thresholds;
        points = thresholds.Length;
        return this;
    }

    /// <summary>
    /// While a value is between 2 thresholds, lerp between them by that amount.
    /// Assumes the input cannot be less than 0 or greater than 1.
    /// </summary>
    public override double Get(double x, double z, SampleData sampleData)
    {
        //Value to lerp through
        double value = module.Get(x, z, sampleData);

        //If value is 0 return first terrain
        if (value == 0)
        {
            return terrains[0].Sample(x, z, sampleData);
        }

        //Find what threshold the value is below
        int indexPos;
        for (indexPos = 0; indexPos < points; indexPos++)
        {
            if (value <= thresholds[indexPos])
            {
                break;
            }
        }

        double lerp = VMath.InverseLerp(value, thresholds[indexPos - 1], thresholds[indexPos]);

        //If terrains are the same only sample 1
        if (terrains[indexPos - 1].hash == terrains[indexPos].hash)
        {
            return terrains[indexPos].Sample(x, z, sampleData);
        }

        return GameMath.Lerp(terrains[indexPos - 1].Sample(x, z, sampleData), terrains[indexPos].Sample(x, z, sampleData), lerp * lerp);
    }
}