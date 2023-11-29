using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Server;

public abstract class TerrainType
{
    public Module module;
    public int hash = 0;

    public TerrainType(ICoreServerAPI sapi)
    {
        List<Type> annotatedTerrains = Assembly.GetExecutingAssembly().GetTypes().Where(t => Attribute.IsDefined(t, typeof(TerrainAttribute))).ToList();
        foreach (Type annotatedTerrain in annotatedTerrains)
        {
            TerrainAttribute customAttribute = (TerrainAttribute)Attribute.GetCustomAttribute(annotatedTerrain, typeof(TerrainAttribute));

            if (annotatedTerrain == GetType())
            {
                hash = customAttribute.TerrainName.GetHashCode();
                break;
            }
        }

        int worldHeight = sapi.WorldManager.MapSizeY;
        int aboveSeaLevel = worldHeight - 100;
        float heightMultiplier = aboveSeaLevel / 284f;
        float frequencyMultiplier = 284f / aboveSeaLevel;

        Initialize(sapi.World.Seed, sapi, frequencyMultiplier, heightMultiplier);
    }

    /// <summary>
    /// Frequency multiplier is what frequencies and scales must be multiplied by to be the same as 384.
    /// </summary>
    public abstract void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier);

    /// <summary>
    /// Returns height factor at location.
    /// </summary>
    public virtual double Sample(double x, double z, SampleData sampleData)
    {
        return module.Get(x, z, sampleData);
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class TerrainAttribute : Attribute
{
    public string TerrainName { get; }

    public TerrainAttribute(string terrainName)
    {
        TerrainName = terrainName;
    }
}