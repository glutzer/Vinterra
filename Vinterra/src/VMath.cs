using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

public static class VMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CubicInterpolation(double n0, double n1, double n2, double n3, double a)
    {
        double p = n3 - n2 - (n0 - n1);
        double q = n0 - n1 - p;
        double r = n2 - n0;

        return p * a * a * a + q * a * a + r * a + n1;
    }

    /// <summary>
    /// Returns value from 0-1 which represents the distance along a line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetProjection(Vec2d point, Vec2d start, Vec2d end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double v = (point.X - start.X) * dx + (point.Y - start.Y) * dy;
        v /= dx * dx + dy * dy;
        return (float)(v < 0 ? 0 : v > 1 ? 1 : v);
    }

    /// <summary>
    /// Minimum distance from a 2d line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceToLine(double x, double z, Vec2d start, Vec2d end)
    {
        if ((start.X - end.X) * (x - end.X) + (start.Y - end.Y) * (z - end.Y) <= 0)
        {
            return Math.Sqrt((x - end.X) * (x - end.X) + (z - end.Y) * (z - end.Y));
        }

        if ((end.X - start.X) * (x - start.X) + (end.Y - start.Y) * (z - start.Y) <= 0)
        {
            return Math.Sqrt((x - start.X) * (x - start.X) + (z - start.Y) * (z - start.Y));
        }

        return Math.Abs((end.Y - start.Y) * x - (end.X - start.X) * z + end.X * start.Y - end.Y * start.X) / Math.Sqrt((start.Y - end.Y) * (start.Y - end.Y) + (start.X - end.X) * (start.X - end.X));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceToLine(Vec2d point, Vec2d start, Vec2d end)
    {
        if ((start.X - end.X) * (point.X - end.X) + (start.Y - end.Y) * (point.Y - end.Y) <= 0)
        {
            return Math.Sqrt((point.X - end.X) * (point.X - end.X) + (point.Y - end.Y) * (point.Y - end.Y));
        }

        if ((end.X - start.X) * (point.X - start.X) + (end.Y - start.Y) * (point.Y - start.Y) <= 0)
        {
            return Math.Sqrt((point.X - start.X) * (point.X - start.X) + (point.Y - start.Y) * (point.Y - start.Y));
        }

        return Math.Abs((end.Y - start.Y) * point.X - (end.X - start.X) * point.Y + end.X * start.Y - end.Y * start.X) / Math.Sqrt((start.Y - end.Y) * (start.Y - end.Y) + (start.X - end.X) * (start.X - end.X));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DistanceTo3DLine(Vec3d point, Vec3d start, Vec3d end)
    {
        Vec3d bc = end - start;
        double length = bc.Length();
        double param = 0.0;
        if (length != 0.0) param = Math.Clamp((point - start).Dot(bc) / (length * length), 0.0, 1.0);
        return point.DistanceTo(start + bc * param);
    }

    
    /// <summary>
    /// Maps a value, originally between a min and max, to a range.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Map(float value, float min, float max, float range)
    {
        float dif = Math.Clamp(value, min, max) - min;
        return dif >= range ? 1f : dif / range;
    }

    /// <summary>
    /// How much between 2 values a value is.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InverseLerp(float value, float min, float max)
    {
        if (Math.Abs(max - min) < float.Epsilon)
        {
            return 0f;
        }
        else
        {
            return (value - min) / (max - min);
        }
    }

    /// <summary>
    /// Find out how much a value is between 2 values.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InverseLerp(double value, double min, double max)
    {
        if (Math.Abs(max - min) < double.Epsilon)
        {
            return 0f;
        }
        else
        {
            return (value - min) / (max - min);
        }
    }

    /// <summary>
    /// Inverse lerps and applies a curve to it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InverseExponentialSmoothStep(double value, double min, double max, int power)
    {
        return Math.Pow(InverseLerp(value, min, max), power);
    }

    /// <summary>
    /// Returns the smoothstep version of inverse lerp.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InverseSmoothStep(double value, double min, double max)
    {
        double t = (value - min) / (max - min);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Some weird curve.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InverseSmoothStep3(double value, double min, double max)
    {
        double t = (value - min) / (max - min);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}