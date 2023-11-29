public class DomainWarp : Module
{
    readonly Module module;

    readonly double strength;

    readonly Noise xNoise;
    readonly Noise zNoise;

    public DomainWarp(Module module, double strength, int seed, float frequency, int octaves, float gain = 0.5f, float lacunarity = 2, string noiseType = "simplex")
    {
        this.module = module;

        this.strength = strength;

        if (noiseType == "simplex")
        {
            xNoise = new Noise(seed + 150, frequency, octaves, gain, lacunarity);
            zNoise = new Noise(seed + 510, frequency, octaves, gain, lacunarity);
        }

        if (noiseType == "voronoi")
        {
            xNoise = new Noise(seed + 150, frequency, octaves, gain, lacunarity).Cellular();
            zNoise = new Noise(seed + 510, frequency, octaves, gain, lacunarity).Cellular();
        }
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return module.Get(x + xNoise.GetNormalNoise(x, z) * strength, z + zNoise.GetNormalNoise(x, z) * strength, sampleData);
    }
}