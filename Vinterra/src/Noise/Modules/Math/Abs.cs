using System;

public class Abs : Module
{
    readonly Module module;

    public Abs(Module module)
    {
        this.module = module;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return Math.Abs(module.Get(x, z, sampleData));
    }
}