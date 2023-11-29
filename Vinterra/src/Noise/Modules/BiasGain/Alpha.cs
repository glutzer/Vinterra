public class Alpha : Module
{
    readonly Module module;
    readonly double alpha;

    public Alpha(Module module, double alpha)
    {
        this.module = module;
        this.alpha = alpha;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return (module.Get(x, z, sampleData) * alpha) + (1 - alpha);
    }
}