using System.Threading.Tasks;
using System;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generate data for a chunk to use before anything.
/// </summary>
public class GenerateData : WorldGenBase
{
    public ICoreServerAPI sapi;
    public BiomeSystem biomeSystem;

    //Noise used to distort borders and rivers
    public EdgeNoise edgeNoise;

    public static Dictionary<long, GenerationData> generationDataCache;
    public static Dictionary<long, GenerationData> erodedGenerationDataCache;

    public RiverGenerator riverGenerator;
    public ErosionGenerator erosionGenerator;

    public override double ExecuteOrder() => 0.0001;

    public Noise beachNoise;

    public int borderDistanceThreshold;

    public double blockFactor = 0;

    public double continentalnessBoost;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        generationDataCache = new Dictionary<long, GenerationData>();
        erodedGenerationDataCache = new Dictionary<long, GenerationData>();

        sapi.Event.InitWorldGenerator(InitWorldGenerator, "standard");
        sapi.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, ServerRunPhase);
        sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");

        borderDistanceThreshold = VConfig.Loaded.borderDistanceThreshold;
        continentalnessBoost = VinterraMod.GetFactorFromBlocks(VConfig.Loaded.continentalnessBoost);
    }

    public void ServerRunPhase()
    {
        if (sapi.WorldManager.SaveGame.WorldType != "standard") return;

        TerraGenConfig.seaLevel = 100;
        sapi.WorldManager.SetSeaLevel(TerraGenConfig.seaLevel);
    }

    public void InitWorldGenerator()
    {
        LoadGlobalConfig(sapi);

        biomeSystem = BiomeSystem.Get(sapi);

        edgeNoise = new EdgeNoise(sapi.World.Seed);

        riverGenerator = new RiverGenerator();

        erosionGenerator = new ErosionGenerator(chunkMapWidth, chunkSize, seaLevel, sapi.WorldManager.MapSizeY, sapi);

        beachNoise = new Noise(sapi.World.Seed, VConfig.Loaded.beachFrequency, 1);

        blockFactor = VinterraMod.GetFactorFromBlocks(1);
    }

    public void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        //Biome permanent data storage
        GenerationData normalData = GetOrCreateData(request.ChunkX, request.ChunkZ, chunkMapWidth, sapi);

        //Set chunk data for client/mob spawns before scattering
        ushort[] biomeData = new ushort[chunkSize * chunkSize];
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                biomeData[LocalChunkIndex2D(x, z)] = normalData.biomeIds[x, z];
            }
        }
        request.Chunks[0].SetModdata("biomeMap", biomeData);

        //Add river flow data
        if (normalData.flowVectorsX != null)
        {
            request.Chunks[0].SetModdata<float[]>("flowVectorsX", normalData.flowVectorsX);
            request.Chunks[0].SetModdata<float[]>("flowVectorsZ", normalData.flowVectorsZ);
        }

        GetOrCreateErodedData(request.ChunkX, request.ChunkZ, chunkMapWidth, sapi);
    }

    public static GenerationData GetOrCreateData(int chunkX, int chunkZ, int chunkMapWidth, ICoreServerAPI sapi)
    {
        long index = GlobalChunkIndex2D(chunkX, chunkZ, chunkMapWidth);
        if (generationDataCache.ContainsKey(index)) return generationDataCache[index];

        sapi.ModLoader.GetModSystem<GenerateData>().MakeData(chunkX, chunkZ);

        return generationDataCache[index];
    }

    public static GenerationData GetOrCreateErodedData(int chunkX, int chunkZ, int chunkMapWidth, ICoreServerAPI sapi)
    {
        long index = GlobalChunkIndex2D(chunkX, chunkZ, chunkMapWidth);
        if (erodedGenerationDataCache.ContainsKey(index)) return erodedGenerationDataCache[index];

        GenerationData data = GetOrCreateData(chunkX, chunkZ, chunkMapWidth, sapi);

        sapi.ModLoader.GetModSystem<GenerateData>().erosionGenerator.Erode(chunkX, chunkZ);

        erodedGenerationDataCache.Add(index, data);

        return erodedGenerationDataCache[index];
    }

    public void MakeData(int chunkX, int chunkZ)
    {
        int plateX = chunkX / chunksInPlate;
        int plateZ = (chunkZ - (plateX + 1) % 2 * chunksInPlate / 2) / chunksInPlate; //Shifted continents

        TectonicPlate plate = ObjectCacheUtil.GetOrCreate(sapi, plateX.ToString() + "+" + plateZ.ToString(), () =>
        {
            return new TectonicPlate(sapi, plateX, plateZ);
        });

        GenerationData generationData = new(chunkSize);

        int localChunkX = chunkX % chunksInPlate;
        int localChunkZ = (chunkZ - (plateX + 1) % 2 * chunksInPlate / 2) % chunksInPlate; //Shifted continents

        int localZoneX = localChunkX / chunksInZone;
        int localZoneZ = localChunkZ / chunksInZone;

        Vec2d plateStart = plate.globalPlateStart;

        //Get zones. Rivers sampled using normal coordinates
        List<TectonicZone> riverZoneList = plate.GetZonesAround(localZoneX, localZoneZ, 2);
        List<RiverSegment> validSegments = new();
        Vec2d localStart = new(localChunkX * chunkSize, localChunkZ * chunkSize);
        foreach (TectonicZone riverZone in riverZoneList)
        {
            foreach (River river in riverZone.rivers)
            {
                if (VMath.DistanceToLine(localStart, river.startPoint, river.endPoint) < 1000)
                {
                    foreach (RiverSegment segment in river.segments)
                    {
                        if (VMath.DistanceToLine(localStart, segment.startPoint, segment.endPoint) < 1000)
                        {
                            validSegments.Add(segment); //Later check for duplicates. If the distance to another segment is too great it shouldn't have to be here
                        }
                    }
                }
            }
        }
        validSegments = validSegments.OrderBy(p => VMath.DistanceToLine(localStart, p.startPoint, p.endPoint)).ToList();
        if (validSegments.Count > 2)
        {
            double secondSegmentDistance = VMath.DistanceToLine(localStart, validSegments[1].startPoint, validSegments[1].endPoint);
            validSegments = validSegments.Where(p => VMath.DistanceToLine(localStart, p.startPoint, p.endPoint) < secondSegmentDistance + 700).ToList(); //Anything 100 farther away than the second segment is invalid
        }
        
        //Local chunk coordinates before distortion
        int localChunkCenterX = localChunkX * chunkSize + chunkSize / 2;
        int localChunkCenterZ = localChunkZ * chunkSize + chunkSize / 2;

        //Distort them, world coordinates as input
        double distortedCenterX = localChunkCenterX + edgeNoise.GetX((int)plateStart.X + localChunkCenterX, (int)plateStart.Y + localChunkCenterZ);
        double distortedCenterZ = localChunkCenterZ + edgeNoise.GetZ((int)plateStart.X + localChunkCenterX, (int)plateStart.Y + localChunkCenterZ);
        Vec2d distortedCenterCoords = new(distortedCenterX, distortedCenterZ);
        List<TectonicZone> closestDistortedZones = plate.GetZonesAround(Math.Clamp((int)distortedCenterX / chunkSize / chunksInZone, 0, plate.zonesInPlate - 1), Math.Clamp((int)distortedCenterZ / chunkSize / chunksInZone, 0, plate.zonesInPlate - 1), 2);
        closestDistortedZones = closestDistortedZones.OrderBy(p => p.localZoneCenterPosition.DistanceTo(distortedCenterCoords)).ToList();
        double firstZoneDistance = closestDistortedZones[0].localZoneCenterPosition.DistanceTo(distortedCenterCoords);
        double secondZoneDistance = closestDistortedZones[1].localZoneCenterPosition.DistanceTo(distortedCenterCoords);
        closestDistortedZones = closestDistortedZones
            .Where(p => p.localZoneCenterPosition.DistanceTo(distortedCenterCoords) < secondZoneDistance + 200 || p.localZoneCenterPosition.DistanceTo(distortedCenterCoords) - firstZoneDistance <= borderDistanceThreshold + 200)
            .ToList();

        //Data for river flow direction
        float[] flowVectorsX = new float[chunkSize * chunkSize];
        float[] flowVectorsZ = new float[chunkSize * chunkSize];
        bool riverBank = false;
        object riverLock = new();

        Parallel.For(0, (chunkSize + 2) * (chunkSize + 2), chunkIndex2D =>
        {
            //Chunk coordinates with a padding of 2 for gradients
            int localX = chunkIndex2D % (chunkSize + 2) - 1;
            int localZ = chunkIndex2D / (chunkSize + 2) - 1;

            //Global world coordinates
            int worldX = chunkX * chunkSize + localX;
            int worldZ = chunkZ * chunkSize + localZ;

            double xDistortion = edgeNoise.GetX(worldX, worldZ);
            double zDistortion = edgeNoise.GetZ(worldX, worldZ);

            //Gets the coordinates relative to the plate and adds the distortion
            Vec2d plateDistortedCoords = new(localChunkX * chunkSize + localX + xDistortion, localChunkZ * chunkSize + localZ + zDistortion);

            //Samples continent noise with world coordinates
            //double continentalness = plate.continentNoise.GetContinentNoise(plateStart + plateDistortedCoords); //Continentalness > 0 land < 0 sea
            double continentalness = plate.continentNoise.GetContinentNoise(new Vec2d(worldX, worldZ));

            double heightFactor = 0;

            //Order distorted zones
            List<TectonicZone> closestZones = closestDistortedZones.OrderBy(p => p.localZoneCenterPosition.DistanceTo(plateDistortedCoords)).ToList();

            //Get nearest zone
            TectonicZone zone = closestZones[0];

            //Sample all rivers in that zone
            //RiverSample riverSample = riverGenerator.SampleRiver(validSegments, plateDistortedCoords.X, plateDistortedCoords.Y);
            RiverSample riverSample = riverGenerator.SampleRiver(validSegments, localStart.X + localX, localStart.Y + localZ);

            //d0 is closest zone, d1 is next closest. Calculate border distance by d1 - d0
            double d0 = plateDistortedCoords.DistanceTo(closestZones[0].localZoneCenterPosition);
            double d1 = plateDistortedCoords.DistanceTo(closestZones[1].localZoneCenterPosition);
            double borderDistance = d1 - d0;

            SampleData sampleData = new(d0, borderDistance, riverSample.riverDistance);

            //Border distance is 50 but I'm manually substituting for now
            if (borderDistance <= borderDistanceThreshold)
            {
                closestZones = closestZones.Where(p =>
                {
                    return p.localZoneCenterPosition.DistanceTo(plateDistortedCoords) - d0 <= borderDistanceThreshold; //Check if zone type is ocean later
                }).ToList();

                //1 weight for every zone
                double totalWeight = 0;
                double[] weights = new double[closestZones.Count];

                //Weight of closest zone
                weights[0] = GameMath.SmoothStep(VMath.InverseLerp(borderDistance, -borderDistanceThreshold, borderDistanceThreshold));

                //Weight of 2nd closest zone
                weights[1] = GameMath.SmoothStep(VMath.InverseLerp(borderDistance, borderDistanceThreshold, -borderDistanceThreshold));

                //Weight of other zones
                for (int i = 2; i < weights.Length; i++)
                {
                    double dn = plateDistortedCoords.DistanceTo(closestZones[i].localZoneCenterPosition) - d0;
                    weights[i] = GameMath.SmoothStep(VMath.InverseLerp(dn, borderDistanceThreshold, -borderDistanceThreshold));
                }

                for (int i = 0; i < weights.Length; i++) totalWeight += weights[i];

                //Calculate height weights[zone index] / totalWeight
                for (int i = 0; i < weights.Length; i++)
                {
                    if (i > 0) sampleData.borderDistance = 0; //Other zones have a border distance of zero in other zones

                    double factor = closestZones[i].terrainType.Sample(worldX, worldZ, sampleData);
                    heightFactor += factor * (weights[i] / totalWeight);
                }
            }
            else
            {
                heightFactor = zone.terrainType.Sample(worldX, worldZ, sampleData);
            }

            //Boost base height based on continent value
            double boost = continentalnessBoost * continentalness;
            heightFactor += boost;

            //Start subtracting when continentalness dips below 1
            bool isBeach = false;
            if (continentalness < 1)
            {
                heightFactor -= 1 - continentalness; //Subtract height. 0 subtracted at 1 and 1 subtracted at 0

                //Curve height below sea level
                if (heightFactor < 0)
                {
                    heightFactor = plate.continentNoise.oceanCurve.Get(Math.Max(heightFactor, -1));
                }

                double maxBeachHeight = zone.biome.beachHeight + beachNoise.GetNormalNoise(worldX, worldZ) * zone.biome.beachHeight * (continentalness * continentalness); //Varying beach height with flatter areas getting higher beaches
                if (heightFactor < maxBeachHeight)
                {
                    isBeach = true;
                }
            }

            //Set height to atleast the bank factor to form the river bank
            if (riverSample.bankFactor > 0) heightFactor = Math.Min(heightFactor, -riverSample.bankFactor + boost);

            //Set data
            if (localX > -1 && localX < chunkSize && localZ > -1 && localZ < chunkSize)
            {
                generationData.erosionMap[localX, localZ] = (float)heightFactor; //Data used to generate erosion which outputs to heightLevels

                generationData.continentalness[localX, localZ] = continentalness;
                generationData.riverDistance[localX, localZ] = riverSample.riverDistance;

                generationData.biomeIds[localX, localZ] = zone.biomeId;

                generationData.isBeach[localX, localZ] = isBeach;

                if (sampleData.specialArea) generationData.specialArea[localX, localZ] = true;

                //Add flow direction data from the sample
                //-10 is the default value
                if (riverSample.flowVectorX > -100)
                {
                    flowVectorsX[LocalChunkIndex2D(localX, localZ)] = riverSample.flowVectorX;
                    flowVectorsZ[LocalChunkIndex2D(localX, localZ)] = riverSample.flowVectorZ;

                    if (riverBank == false)
                    {
                        lock (riverLock)
                        {
                            riverBank = true;
                        }
                    }
                }
            }
        });

        //If a chunk has river data, add it to the generation object
        if (riverBank)
        {
            generationData.flowVectorsX = flowVectorsX;
            generationData.flowVectorsZ = flowVectorsZ;
        }

        generationData.plate = plate;

        generationDataCache.Add(GlobalChunkIndex2D(chunkX, chunkZ, chunkMapWidth), generationData);
    }
}