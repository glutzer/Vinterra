using System;
using Vintagestory.API.MathTools;

public abstract class DecoratorCondition
{
    public float value;
    public float chance;

    /// <summary>
    /// yPos, actual temperature, 0-1 fertility, 0-1 rain, and a random.
    /// </summary>
    public abstract bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DecoratorConditionAttribute : Attribute
{
    public string DecoratorConditionName { get; }

    public DecoratorConditionAttribute(string decoratorConditionName)
    {
        DecoratorConditionName = decoratorConditionName;
    }
}