using System;

/// <summary>
/// Assumes a noise value or derived from 0-1.
/// </summary>
public class Gain : Module
{
    readonly Module module;
    readonly double gain;

    public Gain(Module module, double gain)
    {
        this.module = module;
        this.gain = gain;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        double value = module.Get(x, z, sampleData);
        value *= 2;
        value -= 1;

        value *= gain;

        value = Math.Clamp(value, -1, 1);

        value += 1;
        value /= 2;

        return value;
    }
}