using Vintagestory.API.MathTools;

/// <summary>
/// Adds height based on distance from border.
/// </summary>
public class BorderDistanceAdd : Module
{
    readonly Module module;
    readonly double maxBorderDistance;
    readonly double additionAtMax;

    public BorderDistanceAdd(Module module, double maxBorderDistance, double additionAtMax)
    {
        this.module = module;
        this.maxBorderDistance = maxBorderDistance;
        this.additionAtMax = additionAtMax;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        if (sampleData.borderDistance > maxBorderDistance) return module.Get(x, z, sampleData) + additionAtMax;

        return module.Get(x, z, sampleData) + GameMath.Lerp(0, additionAtMax, VMath.InverseLerp(sampleData.borderDistance, 0, maxBorderDistance));
    }
}