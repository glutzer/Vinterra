public class Invert : Module
{
    readonly Module module;

    public Invert(Module module)
    {
        this.module = module;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return 1 - module.Get(x, z, sampleData);
    }
}