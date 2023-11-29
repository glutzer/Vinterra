public class SpecialMarker : Module
{
    Module module;
    double height;

    public SpecialMarker(Module module, double height)
    {
        this.module = module;
        this.height = height;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double value = module.Get(x, z, sampleData);
        if (value > height)
        {
            sampleData.specialArea = true;
        }
        return value;
    }
}