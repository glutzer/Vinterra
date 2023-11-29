/// <summary>
/// Mask areas by valley factor passed in through sample data. Multiply for things like terraced biomes.
/// </summary>
public class RiverMaskLinear : Module
{
    readonly Module module;
    readonly int valleyWidth;
    readonly float distortionStrength;
    readonly Noise breakupNoise;

    public RiverMaskLinear(Module module, int valleyWidth, float distortionStrength = 1, int malus = 0, float frequency = 0.01f)
    {
        this.module = module;
        this.valleyWidth = valleyWidth;
        this.distortionStrength = distortionStrength;

        breakupNoise = new Noise(512, frequency, 2);
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double valleyFactor = 1;

        double riverDistance = sampleData.riverDistance * (1 + breakupNoise.GetPosNoise(x, z) * distortionStrength);

        if (sampleData.riverDistance < valleyWidth) valleyFactor = VMath.InverseLerp(riverDistance, 0, valleyWidth);

        return module.Get(x, z, sampleData) * valleyFactor;
    }
}