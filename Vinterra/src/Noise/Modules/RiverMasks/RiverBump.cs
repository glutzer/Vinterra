using Vintagestory.API.MathTools;

/// <summary>
/// Mask areas by valley factor passed in through sample data. Multiply for things like terraced biomes.
/// I used this for salt flats.
/// </summary>
public class RiverBump : Module
{
    readonly Module module;
    readonly int width;
    readonly int middleWidth;
    readonly double amountToBump;

    public RiverBump(Module module, int width, double amountToBump = 0.01)
    {
        this.module = module;
        this.width = width;
        middleWidth = width / 2;
        this.amountToBump = amountToBump;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double bump = 0;

        if (sampleData.riverDistance < width)
        {
            if (sampleData.riverDistance < middleWidth)
            {
                bump = amountToBump * GameMath.SmoothStep(VMath.InverseLerp(sampleData.riverDistance, 0, middleWidth));
            }
            else
            {
                bump = amountToBump * GameMath.SmoothStep(VMath.InverseLerp(sampleData.riverDistance, width, middleWidth));
            }
        }
        
        return module.Get(x, z, sampleData) + bump;
    }
}