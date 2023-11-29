using System;

public class RiverMaskSquared : Module
{
    readonly Module module;
    readonly int valleyWidth;
    readonly float power;
    readonly float distortionStrength;
    readonly int malus;

    readonly Noise breakupNoise;

    public RiverMaskSquared(Module module, int valleyWidth, float power, float distortionStrength = 1, int malus = 0, float frequency = 0.01f)
    {
        this.module = module;
        this.valleyWidth = valleyWidth;
        this.power = power;
        this.distortionStrength = distortionStrength;
        this.malus = malus;

        breakupNoise = new Noise(512, frequency, 2);
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double valleyFactor = 1;

        double riverDistance = sampleData.riverDistance * (1 + breakupNoise.GetPosNoise(x, z) * distortionStrength);

        if (riverDistance < valleyWidth) valleyFactor = VMath.InverseLerp(riverDistance, malus, valleyWidth); //-10 makes the quadratic curve begin faster

        return module.Get(x, z, sampleData) * Math.Pow(valleyFactor, power);
    }
}