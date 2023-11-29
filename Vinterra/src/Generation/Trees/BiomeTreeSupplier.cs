using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

public class BiomeTreeSupplier : WgenTreeSupplier
{
    public TreeGenProperties treeGenProperties;

    public ICoreServerAPI sapi;
    public int worldHeight;

    //For getting different types of trees
    public LCGRandom treeRand;

    //Pool of valid trees
    public Dictionary<BiomeTree, int> pool = new();

    public BiomeTreeSupplier(ICoreServerAPI api) : base(api)
    {
        sapi = api;
        worldHeight = sapi.WorldManager.MapSizeY;

        treeRand = new LCGRandom(sapi.World.Seed);

        treeGenProperties = sapi.Assets.Get("game:worldgen/treengenproperties.json").ToObject<TreeGenProperties>();
    }

    public ITreeGenerator GetGenerator(AssetLocation location)
    {
        object treeGenerators = typeof(WgenTreeSupplier).GetField("treeGenerators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
        return (ITreeGenerator)treeGenerators.GetType().GetMethod("GetGenerator", new Type[] { typeof(AssetLocation) }).Invoke(treeGenerators, new object[] { location });
    }

    public TreeGenInstance GetRandomGenForBiome(BiomeTree[] trees, int posY, bool isUnderwater, int worldX, int worldZ, double continentalness, Block block, int climate, double riverDistance)
    {
        treeRand.InitPositionSeed(worldX, worldZ);

        //Get climate from the biome
        int unscaledTemperature = (climate >> 16) & 0xff;
        int temperature = TerraGenConfig.GetScaledAdjustedTemperature(unscaledTemperature, posY - TerraGenConfig.seaLevel); //0-255 rainfall scaled
        int rain = TerraGenConfig.GetRainFall((climate >> 8) & 0xff, posY); //0-255 rainfall scaled
        float height = (float)posY / worldHeight; //0-1 height

        pool.Clear();
        int totalWeight = 0;
        BiomeTree pickedTree = null;

        //Check if underwater
        foreach (BiomeTree tree in trees)
        {
            if (isUnderwater && tree.variant.Habitat == EnumTreeHabitat.Land) continue;
            if (!isUnderwater && tree.variant.Habitat == EnumTreeHabitat.Water) continue;

            if (continentalness > tree.maxContinentalness || continentalness < tree.minContinentalness) continue; //Beach trees
            if (block.Fertility < tree.minFertility || block.Fertility > tree.maxFertility) continue; //Sand check

            if (height < tree.minY || height > tree.maxY) continue;

            if (tree.nearRivers == true)
            {
                //Quadratic falloff of trees
                double lerp = VMath.InverseLerp(riverDistance, tree.riverDistance, 0);
                lerp = Math.Clamp(lerp, 0, 1);
                if (treeRand.NextFloat() > lerp * lerp) continue;
            }

            pool.Add(tree, tree.weight);
            totalWeight += tree.weight;
        }

        if (totalWeight == 0) return null;
        int treeRarity = treeRand.NextInt(totalWeight);

        //Pick from a weight
        foreach (KeyValuePair<BiomeTree, int> tree in pool)
        {
            totalWeight -= tree.Value;
            if (totalWeight <= treeRarity)
            {
                pickedTree = tree.Key;
                break;
            }
        }

        if (pickedTree != null)
        {
            float chance = treeRand.NextFloat();
            if (chance > pickedTree.chance) return null; //Chance to not spawn a tree

            float size = pickedTree.variant.MinSize + (float)treeRand.NextDouble() * (pickedTree.variant.MaxSize - pickedTree.variant.MinSize);
            size *= pickedTree.sizeMultiplier;

            //Vines / moss calculation
            float descaledTemperature = TerraGenConfig.DescaleTemperature(temperature);

            float temperatureVal = Math.Max(0, (descaledTemperature / 255f - treeGenProperties.descVineMinTempRel) / (1 - treeGenProperties.descVineMinTempRel));
            float rainVal = Math.Max(0, (rain / 255f - treeGenProperties.vinesMinRain) / (1 - treeGenProperties.vinesMinRain));

            float temperatureValMoss = descaledTemperature / 255f;
            float rainValMoss = rain / 255f;

            float vinesGrowthChance = 1.5f * rainVal * temperatureVal + 0.5f * rainVal * GameMath.Clamp((temperatureVal + 0.33f) / 1.33f, 0, 1);

            double mossGrowChance = 2.25 * rainValMoss - 0.5 + Math.Sqrt(temperatureValMoss) * 3 * Math.Max(-0.5, 0.5 - temperatureValMoss);
            float mossGrowthChance = GameMath.Clamp((float)mossGrowChance, 0, 1);

            //Get the generator
            ITreeGenerator treeGen = GetGenerator(pickedTree.variant.Generator);

            if (treeGen == null)
            {
                sapi.World.Logger.Error("treengenproperties.json references tree generator {0}, but no such generator exists!", pickedTree.variant.Generator);
                return null;
            }

            return new TreeGenInstance()
            {
                treeGen = treeGen,
                size = size,
                vinesGrowthChance = vinesGrowthChance,
                mossGrowthChance = mossGrowthChance
            };
        }

        return null;
    }
}