using System;

public class Max : Module
{
    readonly Module module1;
    readonly Module module2;

    public Max(Module module1, Module module2)
    {
        this.module1 = module1;
        this.module2 = module2;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return Math.Max(module1.Get(x, z, sampleData), module2.Get(x, z, sampleData));
    }
}