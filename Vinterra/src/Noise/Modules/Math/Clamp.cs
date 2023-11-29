using System;

public class Clamp : Module
{
    readonly Module module;
    readonly double min;
    readonly double max;

    public Clamp(Module module, double min, double max)
    {
        this.module = module;
        this.min = min;
        this.max = max;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return Math.Clamp(module.Get(x, z, sampleData), min, max);
    }
}