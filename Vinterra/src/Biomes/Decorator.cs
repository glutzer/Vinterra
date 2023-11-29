using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

/// <summary>
/// Contains all data for decorators and annotations.
/// </summary>
public abstract class Decorator
{
    public string type;

    public static int[] strataIds = new int[4]; //Set once per block in layer placer

    public int seaLevel;

    public int[] layers;
    public int layerLength;

    public DecoratorCondition[] conditions;

    //Noise
    public float[] noiseThresholds;
    public Noise noise;
    public float noiseStrength;

    //Repeating
    public int repeatTimes;
    public int repeatLayerThickness;

    public abstract int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertNormal);

    public static void SetStrata(int rockId, int gravelId, int sandId, int hardenedSandId)
    {
        strataIds[0] = rockId;
        strataIds[1] = gravelId;
        strataIds[2] = sandId;
        strataIds[3] = hardenedSandId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetId(int id)
    {
        if (id > 0) return id;

        return strataIds[-id];
    }

    public static void ResolveBlock(string layer, ref int layerToResolve, ICoreServerAPI sapi)
    {
        if (layer == null)
        {
            layerToResolve = 0; //Rock if nothing in the layer
            return;
        }

        if (layer == "rock")
        {
            layerToResolve = 0;
            return;
        }

        if (layer == "gravel")
        {
            layerToResolve = -1;
            return;
        }

        if (layer == "sand")
        {
            layerToResolve = -2;
            return;
        }

        if (layer == "hardenedsand")
        {
            layerToResolve = -3;
            return;
        }

        layerToResolve = sapi.World.GetBlock(new AssetLocation(layer)).Id;
    }

    public static void ResolveLayers(string[] layers, ref int[] layersToResolve, ICoreServerAPI sapi)
    {
        layersToResolve = new int[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == "rock")
            {
                layersToResolve[i] = 0;
                continue;
            }

            if (layers[i] == "gravel")
            {
                layersToResolve[i] = -1;
                continue;
            }

            if (layers[i] == "sand")
            {
                layersToResolve[i] = -2;
                continue;
            }

            if (layers[i] == "hardenedsand")
            {
                layersToResolve[i] = -3;
                continue;
            }

            layersToResolve[i] = sapi.World.GetBlock(new AssetLocation(layers[i])).Id;
        }
    }

    public bool CanDecorate(int yPos, float temperature, int rain, float fertility, LCGRandom rand)
    {
        foreach (DecoratorCondition condition in conditions)
        {
            if (condition.IsInvalid(yPos, temperature, rain, fertility, rand))
            {
                return false;
            }
        }

        return true;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DecoratorAttribute : Attribute
{
    public string DecoratorName { get; }

    public DecoratorAttribute(string decoratorName)
    {
        DecoratorName = decoratorName;
    }
}