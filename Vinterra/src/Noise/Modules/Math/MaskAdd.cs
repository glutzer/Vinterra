/// <summary>
/// Adds something masked by the alpha of the previous value.
/// </summary>
public class MaskAdd : Module
{
    readonly Module module1;
    readonly Module module2;

    public MaskAdd(Module module1, Module module2)
    {
        this.module1 = module1;
        this.module2 = module2;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double original = module1.Get(x, z, sampleData);

        return original + module2.Get(x, z, sampleData) * original;
    }
}