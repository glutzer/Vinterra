using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

public class GeneratePartialFeatures : WorldGenPartial
{
    public BiomeSystem biomeSystem;

    public Dictionary<int, int> gravelMappings = new();
    public Dictionary<int, int> sandMappings = new();
    public Dictionary<int, int> hardenedSandMappings = new();

    public override double ExecuteOrder() => 0.15;

    public IWorldGenBlockAccessor blockAccessor;

    //Can't be more than 1 because neighbor chunks are required
    public override int chunkRange => 1;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        if (TerraGenConfig.DoDecorationPass)
        {
            sapi.Event.InitWorldGenerator(InitWorldGenerator, "standard");
            sapi.Event.GetWorldgenBlockAccessor(GetWorldgenBlockAccessor);
            sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.TerrainFeatures, "standard");
        }

        chunkRand = new LCGRandom(sapi.World.Seed);
    }

    public void InitWorldGenerator()
    {
        //Load config into the system base
        LoadGlobalConfig(sapi);
        biomeSystem = BiomeSystem.Get(sapi);

        //Get rock strata json
        GenerateBlockLayers blockLayers = sapi.ModLoader.GetModSystem<GenerateBlockLayers>();
        gravelMappings = blockLayers.gravelMappings;
        sandMappings = blockLayers.sandMappings;
        hardenedSandMappings = blockLayers.hardenedSandMappings;
    }

    private void GetWorldgenBlockAccessor(IChunkProviderThread chunkProvider)
    {
        blockAccessor = chunkProvider.GetBlockAccessor(true);
    }

    public override void GeneratePartial(IServerChunk[] chunks, int mainChunkX, int mainChunkZ, int generatingChunkX, int generatingChunkZ)
    {
        blockAccessor.BeginColumn();

        //Can't use:
        //Biome map (not scattered yet)
        //GenerationData generationData = GenerateData.GetOrCreateData(generatingChunkX, generatingChunkZ, chunkMapWidth, sapi);
        GenerationData generationData = GenerateData.GetOrCreateErodedData(generatingChunkX, generatingChunkZ, chunkMapWidth, sapi);
        ushort[] biomeMap = chunks[0].GetModdata<ushort[]>("biomeMap");
        BiomeType biome = biomeSystem.biomes[biomeMap[512]]; //Generate features based on the center biome

        int startX = generatingChunkX * chunkSize;
        int startZ = generatingChunkZ * chunkSize;

        float[,] erosionMap = generationData.erosionMap;

        BlockPos pos = new(0, 0, 0);

        //Features listed in the biome
        foreach (PartialFeature feature in biome.biomeFeatures)
        {
            for (int x = 0; x < feature.tries; x++)
            {
                if (chunkRand.NextFloat() >= feature.chance) continue;

                int randX = chunkRand.NextInt(chunkSize);
                int randZ = chunkRand.NextInt(chunkSize);

                pos.X = startX + randX;
                pos.Y = (int)(seaLevel + erosionMap[randX, randZ] * aboveSeaLevel);
                pos.Z = startZ + randZ;

                if (!feature.CanGenerate(randX, randZ, generationData)) continue; //Combine these later
                if (!feature.CanPlace(pos.Y, 0, 0, 0, chunkRand)) continue;

                int rockId = chunks[0].MapChunk.TopRockIdMap[randZ * chunkSize + randX];
                int gravelId = gravelMappings.Get(rockId, rockId);
                int sandId = sandMappings.Get(rockId, rockId);
                int hardenedSandId = hardenedSandMappings.Get(rockId, rockId);

                //Set strata so that layers can be used in feature
                Decorator.SetStrata(rockId, gravelId, sandId, hardenedSandId);

                feature.Generate(pos, chunks, chunkRand, new Vec2d(mainChunkX * chunkSize, mainChunkZ * chunkSize), new Vec2d(mainChunkX * chunkSize + chunkSize - 1, mainChunkZ * chunkSize + chunkSize - 1), blockAccessor);
            }
        }

        //Features that generate everywhere
        foreach (PartialFeature feature in biomeSystem.globalFeatures)
        {
            for (int x = 0; x < feature.tries; x++)
            {
                if (chunkRand.NextFloat() <= feature.chance) continue;

                int randX = chunkRand.NextInt(chunkSize);
                int randZ = chunkRand.NextInt(chunkSize);

                pos.X = startX + randX;
                pos.Y = (int)(seaLevel + erosionMap[randX, randZ] * aboveSeaLevel);
                pos.Z = startZ + randZ;

                if (!feature.CanGenerate(randX, randZ, generationData)) continue; //Combine these later
                if (!feature.CanPlace(pos.Y, 0, 0, 0, chunkRand)) continue;

                int rockId = chunks[0].MapChunk.TopRockIdMap[randZ * chunkSize + randX];
                int gravelId = gravelMappings.Get(rockId, rockId);
                int sandId = sandMappings.Get(rockId, rockId);
                int hardenedSandId = hardenedSandMappings.Get(rockId, rockId);

                //Set strata so that layers can be used in feature
                Decorator.SetStrata(rockId, gravelId, sandId, hardenedSandId);

                feature.Generate(pos, chunks, chunkRand, new Vec2d(mainChunkX * chunkSize, mainChunkZ * chunkSize), new Vec2d(mainChunkX * chunkSize + chunkSize - 1, mainChunkZ * chunkSize + chunkSize - 1), blockAccessor);
            }
        }
    }
}