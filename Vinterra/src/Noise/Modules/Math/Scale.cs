public class Scale : Module
{
    readonly Module module;
    readonly double scale;

    public Scale(Module module, double scale)
    {
        this.module = module;
        this.scale = scale;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return module.Get(x, z, sampleData) * scale;
    }
}