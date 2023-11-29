using Vintagestory.API.MathTools;

public class Blend : Module
{
    readonly Module module1;
    readonly Module module2;
    readonly Module alphaModule;

    public Blend(Module module1, Module module2, Module alphaModule)
    {
        this.module1 = module1;
        this.module2 = module2;
        this.alphaModule = alphaModule;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double v0 = module1.Get(x, z, sampleData);
        double v1 = module2.Get(x, z, sampleData);
        double alpha = alphaModule.Get(x, z, sampleData);
        return GameMath.Lerp(v0, v1, alpha);
    }
}