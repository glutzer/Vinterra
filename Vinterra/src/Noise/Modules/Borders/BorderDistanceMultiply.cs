using Vintagestory.API.MathTools;

/// <summary>
/// Multiplies module based on border distance.
/// </summary>
public class BorderDistanceMultiply : Module
{
    readonly Module module;
    readonly double maxBorderDistance;

    readonly double multiplierAtMin;
    readonly double multiplierAtMax;

    public BorderDistanceMultiply(Module module, double maxBorderDistance, double multiplierAtMin, double multiplierAtMax)
    {
        this.module = module;
        this.maxBorderDistance = maxBorderDistance;

        this.multiplierAtMin = multiplierAtMin;
        this.multiplierAtMax = multiplierAtMax;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        if (sampleData.borderDistance > maxBorderDistance) return module.Get(x, z, sampleData) * multiplierAtMax;

        return module.Get(x, z, sampleData) * GameMath.Lerp(multiplierAtMin, multiplierAtMax, VMath.InverseLerp(sampleData.borderDistance, 0, maxBorderDistance));
    }
}