using System;
using System.Collections;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class GenerateTerrain : WorldGenBase
{
    public ICoreServerAPI sapi;

    public BiomeSystem biomeSystem;

    public double seaWaterContinentalness;

    public Column[] columns;
    public struct Column
    {
        public BitArray signs;
    }

    public override double ExecuteOrder()
    {
        return 0.001;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        sapi.Event.InitWorldGenerator(InitWorldGenerator, "standard");
        sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");

        seaWaterContinentalness = VConfig.Loaded.seaWaterContinentalness;
    }

    public void InitWorldGenerator()
    {
        LoadGlobalConfig(sapi);
        biomeSystem = BiomeSystem.Get(sapi);

        //Initialize columns for 3D noise
        columns = new Column[chunkSize * chunkSize];
        for (int i = 0; i < columns.Length; i++)
        {
            columns[i].signs = new BitArray(mapHeight - 1);
        }
    }

    public void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        IServerChunk[] chunks = request.Chunks;
        IMapChunk mapChunk = chunks[0].MapChunk;
        IChunkBlocks chunkBlockData = chunks[0].Data;

        int startX = request.ChunkX * chunkSize;
        int startZ = request.ChunkZ * chunkSize;

        //Set mantle
        chunkBlockData.SetBlockBulk(0, chunkSize, chunkSize, globalConfig.mantleBlockId);

        //Get water/rocks. Will be biome specific later
        int waterId = globalConfig.waterBlockId;
        int saltWaterId = globalConfig.saltWaterBlockId;
        int rockId = globalConfig.defaultRockId;
        int lakeIceId = globalConfig.lakeIceBlockId;

        GenerationData generationData = GenerateData.GetOrCreateErodedData(request.ChunkX, request.ChunkZ, chunkMapWidth, sapi);

        //Multithread 3d data
        Parallel.For(0, chunkSize * chunkSize, chunkIndex2D =>
        {
            int localX = chunkIndex2D % chunkSize;
            int localZ = chunkIndex2D / chunkSize;

            Column column = columns[chunkIndex2D];
            ushort height = generationData.heightLevels[localX, localZ];
            double gradient = generationData.heightGradient[localX, localZ];

            for (int worldY = 1; worldY < mapHeight - 1; worldY++)
            {
                column.signs[worldY] = true;
            }
        });

        //Climate for lake
        /*
        int rlX = request.ChunkX % regionChunkSize;
        int rlZ = request.ChunkZ % regionChunkSize;
        IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
        float factor = (float)climateMap.InnerSize / regionChunkSize;
        int climateUpLeft = climateMap.GetUnpaddedInt((int)(rlX * factor), (int)(rlZ * factor));
        int climateUpRight = climateMap.GetUnpaddedInt((int)(rlX * factor + factor), (int)(rlZ * factor));
        int climateBotLeft = climateMap.GetUnpaddedInt((int)(rlX * factor), (int)(rlZ * factor + factor));
        int climateBotRight = climateMap.GetUnpaddedInt((int)(rlX * factor + factor), (int)(rlZ * factor + factor));
        float chunkBlockDelta = 1.0f / chunkSize;
        */

        //Iterate over every X Z coordinate in the chunk and generate columns
        for (int localX = 0; localX < chunkSize; localX++)
        {
            for (int localZ = 0; localZ < chunkSize; localZ++)
            {
                int localY = 1;
                int chunkY = 0;
                chunkBlockData = chunks[0].Data;

                int mapIndex = LocalChunkIndex2D(localX, localZ);

                int heightLevel = generationData.heightLevels[localX, localZ];

                bool sea = generationData.continentalness[localX, localZ] < seaWaterContinentalness;

                Column column = columns[LocalChunkIndex2D(localX, localZ)];

                for (int worldY = 1; worldY < mapHeight - 1; worldY++)
                {
                    int index3d = LocalChunkIndex3D(localX, localY, localZ);
                    
                    if (worldY < heightLevel && column.signs[worldY]) //Generate land
                    {
                        //Terrain and rain map set where rain can fall and where the top of terrain is. Changes when you place a block too.
                        mapChunk.WorldGenTerrainHeightMap[mapIndex] = (ushort)worldY;
                        mapChunk.RainHeightMap[mapIndex] = (ushort)worldY;

                        //Set rock
                        chunkBlockData[index3d] = rockId;
                    }
                    else if (worldY < seaLevel) //Generate ocean / rivers / lakes
                    {
                        //Don't set max solid block
                        mapChunk.RainHeightMap[mapIndex] = (ushort)worldY;

                        //Set water
                        if (sea)
                        {
                            chunkBlockData.SetFluid(index3d, saltWaterId);
                        }
                        else
                        {
                            chunkBlockData.SetFluid(index3d, waterId);
                        }
                    }

                    //Iterate locals
                    localY++;
                    if (localY == chunkSize)
                    {
                        localY = 0;
                        chunkY++;
                        chunkBlockData = chunks[chunkY].Data;
                    }
                }
            }
        }

        //For oceans/caves
        ushort yMax = 0;

        for (int i = 0; i < mapChunk.RainHeightMap.Length; i++)
        {
            yMax = Math.Max(yMax, mapChunk.RainHeightMap[i]);
        }

        chunks[0].MapChunk.YMax = yMax;
    }
}