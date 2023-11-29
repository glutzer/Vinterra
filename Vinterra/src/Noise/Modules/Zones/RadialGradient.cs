using System;
using Vintagestory.API.MathTools;

/// <summary>
/// Gaussian gradient from 0 (1) to radius (0).
/// Forms in the center of a region.
/// </summary>
public class RadialGradient : Module
{
    readonly double radius;
    readonly double min;

    public RadialGradient(double radius, double min = 0)
    {
        this.radius = radius;
        this.min = min;
    }

    public override double Get(double x, double z, SampleData sampleData)
    {
        return Math.Max(GameMath.SmoothStep(VMath.InverseLerp(Math.Clamp(sampleData.zoneCenterDistance, 0, radius), radius, 0)), min);
    }
}