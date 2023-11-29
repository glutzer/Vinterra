using System;

public class Power : Module
{
    readonly Module module;
    readonly float power;

    public Power(Module module, float power)
    {
        this.module = module;
        this.power = power;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return Math.Pow(module.Get(x, z, sampleData), power);
    }
}