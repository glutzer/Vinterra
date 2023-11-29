using Vintagestory.API.MathTools;

[DecoratorCondition("aboveY")]
public class AboveY : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return yPos < value;
    }
}

[DecoratorCondition("belowY")]
public class BelowY : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return yPos > value;
    }
}

[DecoratorCondition("aboveTemp")]
public class AboveTemp : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return temperature < value;
    }
}

[DecoratorCondition("belowTemp")]
public class BelowTemp : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return temperature > value;
    }
}

[DecoratorCondition("aboveRain")]
public class AboveRain : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return rain < value;
    }
}

[DecoratorCondition("belowRain")]
public class BelowRain : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return rain > value;
    }
}

[DecoratorCondition("aboveFertility")]
public class AboveFertility : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return fertility < value;
    }
}

[DecoratorCondition("belowFertility")]
public class BelowFertility : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return fertility > value;
    }
}

[DecoratorCondition("chance")]
public class Chance : DecoratorCondition
{
    public override bool IsInvalid(float yPos, float temperature, float rain, float fertility, LCGRandom rand)
    {
        return rand.NextFloat() > chance;
    }
}