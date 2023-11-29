public abstract class Module
{
    public abstract double Get(double x, double z, SampleData sampleData);

    public Terrace Terrace(int points)
    {
        return new Terrace(this, points);
    }

    public Alpha Alpha(double alpha)
    {
        return new Alpha(this, alpha);
    }

    public Bias Bias(double bias)
    {
        return new Bias(this, bias);
    }

    public Gain Gain(double gain)
    {
        return new Gain(this, gain);
    }

    public BorderDistanceAdd BorderDistanceAdd(double maxBorderDistance, double additionAtMax)
    {
        return new BorderDistanceAdd(this, maxBorderDistance, additionAtMax);
    }

    public BorderDistanceMultiply BorderDistanceMultiply(double maxBorderDistance, double multiplierAtMin, double multiplierAtMax)
    {
        return new BorderDistanceMultiply(this, maxBorderDistance, multiplierAtMin, multiplierAtMax);
    }

    public Blend Blend(Module module, Module alphaModule)
    {
        return new Blend(this, module, alphaModule);
    }

    public Max Max(Module module)
    {
        return new Max(this, module);
    }

    public Min Min(Module module)
    {
        return new Min(this, module);
    }

    public CubicCurve CubicCurve()
    {
        return new CubicCurve(this);
    }

    public LinearCurve LinearCurve()
    {
        return new LinearCurve(this);
    }

    public SmoothCurve SmoothCurve()
    {
        return new SmoothCurve(this);
    }

    public Abs Abs()
    {
        return new Abs(this);
    }

    public Add Add(Module module)
    {
        return new Add(this, module);
    }

    public Clamp Clamp(double min, double max)
    {
        return new Clamp(this, min, max);
    }

    public Invert Invert()
    {
        return new Invert(this);
    }

    public MaskAdd MaskAdd(Module module)
    {
        return new MaskAdd(this, module);
    }

    public Multiply Multiply(Module module)
    {
        return new Multiply(this, module);
    }

    public Power Power(float power)
    {
        return new Power(this, power);
    }

    public Scale Scale(double scale)
    {
        return new Scale(this, scale);
    }

    public Subtract Subtract(Module module)
    {
        return new Subtract(this, module);
    }

    public RiverBump RiverBump(int width, double amountToBump = 0.01)
    {
        return new RiverBump(this, width, amountToBump);
    }

    public RiverMaskLinear RiverMaskLinear(int valleyWidth, float distortionStrength = 1, int malus = 0, float frequency = 0.01f)
    {
        return new RiverMaskLinear(this, valleyWidth, distortionStrength, malus, frequency);
    }

    public RiverMaskSquared RiverMaskSquared(int valleyWidth, float power, float distortionStrength = 1, int malus = 0, float frequency = 0.01f)
    {
        return new RiverMaskSquared(this, valleyWidth, power, distortionStrength, malus, frequency);
    }

    public static RadialGradient RadialGradient(double radius, double min)
    {
        return new RadialGradient(radius, min);
    }

    public DomainWarp DomainWarp(double strength, int seed, float frequency, int octaves, float gain = 0.5f, float lacunarity = 2, string noiseType = "simplex")
    {
        return new DomainWarp(this, strength, seed, frequency, octaves, gain, lacunarity, noiseType);
    }

    public SpecialMarker SpecialMarker(double height)
    {
        return new SpecialMarker(this, height);
    }
}