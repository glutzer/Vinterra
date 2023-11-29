public class Bias : Module
{
    readonly Module module;
    readonly double bias;

    public Bias(Module module, double bias)
    {
        this.module = module;
        this.bias = bias;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return module.Get(x, z, sampleData) + bias;
    }
}