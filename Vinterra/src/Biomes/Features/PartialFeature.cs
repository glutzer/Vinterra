﻿using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

public class PartialFeature
{
    public int chunkSize = 32;
    public ICoreServerAPI sapi;
    public int mapHeight;
    
    public float hSize;
    public float vSize;

    public float hSizeVariance;
    public float vSizeVariance;

    public float chance;
    public int tries;
    public DecoratorCondition[] conditions;
    public int[] blocks;

    public Noise noise;

    public int seaLevel = 100;

    public PartialFeature(ICoreServerAPI sapi)
    {
        this.sapi = sapi;
        mapHeight = sapi.WorldManager.MapSizeY;
    }

    /// <summary>
    /// Start and end coordinates are for the chunk being generated.
    /// </summary>
    public virtual void Generate(BlockPos blockPos, IServerChunk[] chunkData, LCGRandom rand, Vec2d chunkStart, Vec2d chunkEnd, IWorldGenBlockAccessor blockAccessor)
    {

    }

    public virtual bool CanGenerate(int localX, int localZ, GenerationData data)
    {
        return true;
    }

    /*
    public void SetBlockFromGlobal(IServerChunk[] chunks, int worldX, int yPos, int worldZ, int blockId)
    {
        int localX = worldX % chunkSize;
        int localY = yPos % chunkSize;
        int localZ = worldZ % chunkSize;
        IServerChunk chunk = chunks[yPos / chunkSize];

        int index = LocalChunkIndex3D(localX, localY, localZ);
        chunk.Data.SetBlockUnsafe(index, blockId);
        chunk.Data.SetFluid(index, 0);
    }

    public void SetBlockAirGlobal(IServerChunk[] chunks, int worldX, int yPos, int worldZ)
    {
        int localX = worldX % chunkSize;
        int localY = yPos % chunkSize;
        int localZ = worldZ % chunkSize;
        IServerChunk chunk = chunks[yPos / chunkSize];
        int index = LocalChunkIndex3D(localX, localY, localZ);
        chunk.Data.SetBlockAir(index);
    }
    */

    public int LocalChunkIndex2D(int localX, int localZ)
    {
        return localZ * chunkSize + localX;
    }

    public int LocalChunkIndex3D(int localX, int localY, int localZ)
    {
        return (localY * chunkSize + localZ) * chunkSize + localX;
    }

    public bool CanPlace(int yPos, float temperature, int rain, float fertility, LCGRandom rand)
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

public class FeatureBoundingBox
{
    public Vec3d start;
    public Vec3d end;

    public FeatureBoundingBox(int startX, int startY, int startZ, int endX, int endY, int endZ)
    {
        start = new Vec3d(startX, startY, startZ);
        end = new Vec3d(endX, endY, endZ);
    }

    public FeatureBoundingBox(Vec3d start, Vec3d end)
    {
        this.start = start;
        this.end = end;
    }

    /// <summary>
    /// Sets bounds to within a chunk and returns if it still exists.
    /// </summary>
    public bool SetBounds(Vec2d chunkStart, Vec2d chunkEnd)
    {
        if (start.X < chunkStart.X) start.X = chunkStart.X;
        if (start.Z < chunkStart.Y) start.Z = chunkStart.Y;

        if (end.X > chunkEnd.X) end.X = chunkEnd.X;
        if (end.Z > chunkEnd.Y) end.Z = chunkEnd.Y;

        if (start.X > end.X) return false;
        if (start.Z > end.Z) return false;

        if (start.Y < 0) start.Y = 0;
        //if (end.Y > 380) end.Y = 380; See if this can be set higher

        if (start.Y > end.Y) return false;

        return true;
    }

    /// <summary>
    /// Evaluates something based on each world position.
    /// </summary>
    public void ForEachPosition(Action<int, int, int, Vec3d> action)
    {
        Vec3d currentPosition = new();

        for (int x = (int)start.X; x <= end.X; x++)
        {
            for (int z = (int)start.Z; z <= end.Z; z++)
            {
                for (int y = (int)start.Y; y <= end.Y; y++)
                {
                    currentPosition.X = x;
                    currentPosition.Y = y;
                    currentPosition.Z = z;
                    action(x, y, z, currentPosition);
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class FeatureAttribute : Attribute
{
    public string FeatureName { get; }

    public FeatureAttribute(string featureName)
    {
        FeatureName = featureName;
    }
}