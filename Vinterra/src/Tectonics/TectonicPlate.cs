using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

public class TectonicPlate
{
    public ICoreServerAPI sapi;

    public int zoneSize; //Multiplier of each "region" (biome) in a plate
    public int zonesInPlate; //Number of zones in plate
    public int zonePadding;

    public int plateSize; //Plate size in blocks
    public Vec2d localPlateCenterPosition = new(); //Local center (plate size / 2)
    public Vec2d globalPlateStart = new();

    public TectonicZone[,] zones; //All regions in the plate

    public LCGRandom rand;

    public ContinentNoise continentNoise; //Used to determine shape of continent

    public BiomeSystem biomeSystem; //Biome system

    //CONFIG
    public int riverGrowth;
    public int riverSpawnChance;
    public int riverSplitChance;
    public int lakeChance;
    public int segmentsInRiver;
    public double segmentOffset;

    public ushort oceanId = 60000;

    //For climate
    public float tempMul;
    public float rainMul;
    public double halfRange;
    public double ZOffSet;

    public TectonicPlate(ICoreServerAPI sapi, int plateX, int plateZ)
    {
        this.sapi = sapi;

        //Multipliers
        ITreeAttribute worldConfig = sapi.WorldManager.SaveGame.WorldConfiguration;
        tempMul = worldConfig.GetString("globalTemperature", "1").ToFloat(1);
        rainMul = worldConfig.GetString("globalPrecipitation", "1").ToFloat(1);

        halfRange = ClimateNoisePatch.halfRange;
        ZOffSet = ClimateNoisePatch.ZOffset;

        //Zone config
        zoneSize = VConfig.Loaded.zoneSize;
        zonesInPlate = VConfig.Loaded.zonesInPlate;
        zonePadding = VConfig.Loaded.zonePadding;

        //River config
        riverGrowth = VConfig.Loaded.riverGrowth;
        riverSpawnChance = VConfig.Loaded.riverSpawnChance;
        riverSplitChance = VConfig.Loaded.riverSplitChance;
        lakeChance = VConfig.Loaded.lakeChance;
        segmentsInRiver = VConfig.Loaded.segmentsInRiver;
        segmentOffset = VConfig.Loaded.segmentOffset;

        plateSize = zoneSize * zonesInPlate;
        localPlateCenterPosition.X = plateSize / 2;
        localPlateCenterPosition.Y = plateSize / 2;

        zones = new TectonicZone[zonesInPlate, zonesInPlate];

        rand = new LCGRandom(sapi.WorldManager.Seed);

        continentNoise = new ContinentNoise(sapi.World.Seed, plateSize);

        biomeSystem = BiomeSystem.Get(sapi);

        //Initialize all zones
        GenerateZones(plateX, plateZ);
    }

    public void GenerateZones(int plateX, int plateZ)
    {
        rand.InitPositionSeed(plateX, plateZ);

        //Width and depth in zones
        int width = zones.GetLength(0);
        int depth = zones.GetLength(1);

        //Global plate coordinates
        globalPlateStart.X = plateX * plateSize;
        globalPlateStart.Y = plateZ * plateSize - (plateX + 1) % 2 * (plateSize / 2); //Shifted continents

        continentNoise.globalPlateCenterPosition = new Vec2d(globalPlateStart.X + plateSize / 2, globalPlateStart.Y + plateSize / 2);
        continentNoise.plateStartX = (int)globalPlateStart.X;
        continentNoise.plateStartZ = (int)globalPlateStart.Y;
        continentNoise.plateSize = plateSize;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                //Initializes the regions center position as a random point in the grid. 100 block padding
                zones[x, z] = new TectonicZone(x * zoneSize + rand.NextInt(zoneSize - zonePadding) + zonePadding,
                                                    z * zoneSize + rand.NextInt(zoneSize - zonePadding) + zonePadding);

                TectonicZone zone = zones[x, z];
                zone.xIndex = x;
                zone.zIndex = z;

                //Sample continentalness at the center of the zone
                double zoneContinentalness = continentNoise.GetContinentNoise(new Vec2d(zone.localZoneCenterPosition.X + globalPlateStart.X, zone.localZoneCenterPosition.Y + globalPlateStart.Y));

                zone.height = zoneContinentalness;

                //Rivers will flow out to areas below 0 continentalness
                if (zoneContinentalness < 0)
                {
                    zone.biomeId = oceanId;
                }
                else
                {
                    zone.biomeId = 0;
                }
            }
        }

        //Check if a zone is coastal
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                TectonicZone zone = zones[x, z];

                if (zone.biomeId == oceanId)
                {
                    zone.coastal = true;
                    zone.oceanic = true;
                    continue;
                }

                for (int xz = -1; xz < 2; xz++)
                {
                    for (int zz = -1; zz < 2; zz++)
                    {
                        if (zones[Math.Clamp(x + xz, 0, zonesInPlate - 1), Math.Clamp(z + zz, 0, zonesInPlate - 1)].biomeId == oceanId)
                        {
                            zone.coastal = true;
                            break;
                        }
                    }

                    if (zone.coastal) break;
                }
            }
        }

        //Set zone height based on distance from ocean
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (zones[x, z].biomeId == oceanId) continue;

                //For every ocean region, calculate distance to the center of the tile
                double closestDistance = double.MaxValue;
                for (int j = 0; j < width; j++)
                {
                    for (int i = 0; i < depth; i++)
                    {
                        if (zones[j, i].biomeId != oceanId) continue;
                        double distance = zones[x, z].localZoneCenterPosition.DistanceTo(zones[j, i].localZoneCenterPosition);
                        if (distance < closestDistance) closestDistance = distance;
                    }
                }

                zones[x, z].height = closestDistance / (plateSize / 2);
            }
        }

        //Pick biomes
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                TectonicZone zone = zones[x, z];
                zone.biomeId = PickBiome((int)globalPlateStart.X + (int)zone.localZoneCenterPosition.X, (int)globalPlateStart.Y + (int)zone.localZoneCenterPosition.Y, zone.coastal);
                zone.biome = biomeSystem.biomes[zones[x, z].biomeId];
                zone.terrainType = zones[x, z].biome.terrainList[rand.NextInt(zones[x, z].biome.terrainList.Count)]; //Get a random terrain type to use. This should check height and coastal parameters
            }
        }

        //Generate rivers
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                TectonicZone zone = zones[x, z];

                //Only generate from oceans
                if (!zone.oceanic)
                {
                    continue;
                }

                //Check if the ocean borders a coastal zone
                bool nearLand = false;
                for (int xz = -1; xz < 2; xz++)
                {
                    for (int zz = -1; zz < 2; zz++)
                    {
                        if (zones[Math.Clamp(x + xz, 0, zonesInPlate - 1), Math.Clamp(z + zz, 0, zonesInPlate - 1)].oceanic == false)
                        {
                            nearLand = true;
                            break;
                        }
                    }

                    if (nearLand) break;
                }

                //Chance to generate a river here
                if (nearLand && rand.NextInt(100) < riverSpawnChance && zone.biome.riversAllowed)
                {
                    List<River> riverList = new();
                    List<TectonicZone> pathedZones = new();
                    GenerateRiver(width, depth, zone, 30, 2, null, riverList, pathedZones);

                    //Invalid number of rivers
                    if (riverList.Count < 3)
                    {
                        foreach (TectonicZone pathedZone in pathedZones)
                        {
                            pathedZone.rivers.Clear();
                            pathedZone.pathedTo = false;
                        }
                        continue;
                    }

                    AssignRiverSizes(riverList);
                    BuildRiverSegments(riverList);
                    ConnectSegments(riverList);
                    ValidateSegments(riverList);
                }
            }
        }

        //Generate lakes
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                BiomeType biome = zones[x, z].biome;
                int lakeTries = biome.lakeTries;
                
                for (int i = 0; i < lakeTries; i++)
                {
                    if (rand.NextFloat() < biome.lakeChance)
                    {
                        AddLake(zones[x, z], biome.lakeOffset, biome.lakeMinSize, biome.lakeMaxSize);
                    }
                }
            }
        }
    }

    public void AssignRiverSizes(List<River> riverList)
    {
        List<River> riverEndList = riverList.Where(river => river.end == true).ToList();

        foreach (River river in riverEndList)
        {
            AssignRiverSize(river, 2);
        }
    }

    public void AssignRiverSize(River river, int startSize)
    {
        if (river.startSize <= startSize) //If the endSize is less than the startSize this river hasn't been generated yet or a bigger river is ready to generate
        {
            river.endSize = startSize;
            river.startSize = startSize + riverGrowth;

            if (river.parentRiver != null)
            {
                AssignRiverSize(river.parentRiver, river.startSize);
            }
        }
    }

    public void BuildRiverSegments(List<River> riverList)
    {
        //Assign segments to each river
        foreach (River river in riverList)
        {
            river.segments = new RiverSegment[segmentsInRiver];

            Vec2d offsetVector = new Vec2d(river.endPoint.X - river.startPoint.X, river.endPoint.Y - river.startPoint.Y).Normalize();
            offsetVector = new Vec2d(-offsetVector.Y, offsetVector.X);

            for (int i = 0; i < river.segments.Length; i++) //For each segment
            {
                double offset = -segmentOffset + rand.NextDouble() * segmentOffset * 2; //Offset segment by up to 200

                river.segments[i] = new RiverSegment();

                if (i == 0)
                {
                    river.segments[i].startPoint = river.startPoint;
                }
                else
                {
                    river.segments[i].startPoint = river.segments[i - 1].endPoint;
                }

                if (i == segmentsInRiver - 1)
                {
                    river.segments[i].endPoint = river.endPoint;
                }
                else
                {
                    river.segments[i].endPoint = new Vec2d(
                        GameMath.Lerp(river.startPoint.X, river.endPoint.X, (double)(i + 1) / segmentsInRiver),
                        GameMath.Lerp(river.startPoint.Y, river.endPoint.Y, (double)(i + 1) / segmentsInRiver)
                        );

                    river.segments[i].endPoint.X += offset * offsetVector.X;
                    river.segments[i].endPoint.Y += offset * offsetVector.Y;
                }

                river.segments[i].river = river;

                river.segments[i].midPoint = river.segments[i].startPoint + (river.segments[i].endPoint - river.segments[i].startPoint) * 0.5;
            }
        }
    }

    public void ConnectSegments(List<River> riverList)
    {
        foreach (River river in riverList)
        {
            //Make sure all rivers flow into eachother smoothly
            if (river.parentRiver?.endSize > river.startSize)
            {
                river.startSize = river.parentRiver.endSize;
            }

            for (int i = 0; i < river.segments.Length; i++)
            {
                if (i == 0)
                {
                    if (river.parentRiver != null)
                    {
                        river.segments[i].parent = river.parentRiver.segments[river.parentRiver.segments.Length - 1]; //Make the last segment in the parent river the parent of this
                        river.parentRiver.segments[segmentsInRiver - 1].children.Add(river.segments[i]); //Add this to the children of that river
                    }
                    else
                    {
                        river.segments[i].parent = river.segments[i];
                    }

                    continue;
                }

                river.segments[i].parent = river.segments[i - 1]; //Make the parent the last segment
                river.segments[i - 1].children.Add(river.segments[i]); //Add this to it's children
            }
        }

        //If the river segment has no children, make it it's own child
        foreach (River river in riverList)
        {
            for (int i = 0; i < river.segments.Length; i++)
            {
                if (river.segments[i].children.Count == 0)
                {
                    river.segments[i].children.Add(river.segments[i]);
                }
            }
        }
    }

    //Checks if a curve is too steep to interpolate
    public void ValidateSegments(List<River> riverList)
    {
        foreach (River river in riverList)
        {
            foreach (RiverSegment segment in river.segments)
            {
                //If it's the last segment invalidate it
                if (segment.children[0] == segment)
                {
                    segment.parentInvalid = true;
                    continue;
                }

                if (segment.parent == segment) continue;

                int index = river.segments.IndexOf(segment);

                if (index == 0)
                {
                    float projection = VMath.GetProjection(segment.startPoint, segment.midPoint, river.parentRiver.segments[segmentsInRiver - 1].midPoint);

                    if (projection < 0.2 || projection > 0.8)
                    {
                        segment.parentInvalid = true;
                    }

                    continue;
                }

                float projection2 = VMath.GetProjection(segment.startPoint, segment.midPoint, river.segments[index - 1].midPoint);

                if (projection2 < 0.2 || projection2 > 0.8)
                {
                    segment.parentInvalid = true;
                }
            }
        }
    }

    /// <summary>
    /// Threshold is how many river steps are taken before splitting can occur.
    /// </summary>
    public void GenerateRiver(int width, int depth, TectonicZone zone, int stage, int threshold, River parentRiver, List<River> riverList, List<TectonicZone> pathedZones)
    {
        threshold--;

        pathedZones.Add(zone);

        //Get all 8 surrounding regions
        List<TectonicZone> closeZonesUnsorted = new()
        {
            zones[Math.Clamp(zone.xIndex + 1, 0, zonesInPlate - 1), zone.zIndex],
            zones[Math.Clamp(zone.xIndex - 1, 0, zonesInPlate - 1), zone.zIndex],
            zones[zone.xIndex, Math.Clamp(zone.zIndex + 1, 0, zonesInPlate - 1)],
            zones[zone.xIndex, Math.Clamp(zone.zIndex - 1, 0, zonesInPlate - 1)]
        };

        //Filter out matching zones
        List<TectonicZone> closeZones = new();
        foreach (TectonicZone tecZone in closeZonesUnsorted)
        {
            if (closeZones.Contains(tecZone)) continue;
            closeZones.Add(tecZone);
        }

        //Get regions that are higher, sorts height difference from highest to lowest
        closeZones = closeZones
            .Where(closeZone => closeZone.height > zone.height && closeZone.pathedTo == false)
            .Where(closeZone => closeZone.biome.riversAllowed) //Only make rivers to zones where rivers can spawn
            .OrderByDescending(closeRegion => closeRegion.height - zone.height)
            .ToList();

        //Don't split until threshold met. Don't split at the start. Only split if 2 valid zones
        if (closeZones.Count > 1 && rand.NextInt(100) < riverSplitChance && threshold < 0 && stage > 0)
        {
            River newRiver = new(zone.localZoneCenterPosition, closeZones[0].localZoneCenterPosition);
            River secondRiver = new(zone.localZoneCenterPosition, closeZones[1].localZoneCenterPosition);

            closeZones[0].pathedTo = true;
            closeZones[1].pathedTo = true;

            newRiver.parentRiver = parentRiver;
            secondRiver.parentRiver = parentRiver;

            riverList.Add(newRiver);
            riverList.Add(secondRiver);

            zone.rivers.Add(newRiver);
            zone.rivers.Add(secondRiver);

            GenerateRiver(width, depth, closeZones[0], stage - 1, threshold, newRiver, riverList, pathedZones);
            GenerateRiver(width, depth, closeZones[1], stage - 1, threshold, secondRiver, riverList, pathedZones);
        }
        else if (closeZones.Count > 0 && stage > 0)
        {
            River newRiver = new(zone.localZoneCenterPosition, closeZones[0].localZoneCenterPosition);

            closeZones[0].pathedTo = true;

            newRiver.parentRiver = parentRiver;

            riverList.Add(newRiver);

            zone.rivers.Add(newRiver);

            GenerateRiver(width, depth, closeZones[0], stage - 1, threshold, newRiver, riverList, pathedZones);
        }
        else if (parentRiver != null)
        {
            parentRiver.end = true;

            //Need sin wave distortion for lakes
            if (rand.NextInt(100) < lakeChance)
            {
                AddLake(zone, 0, 50, 100);
            }
        }
    }

    public void AddLake(TectonicZone zone, int offset, int minSize, int maxSize)
    {
        int lakeSize = rand.NextInt(maxSize - minSize) + minSize;
        Vec2d riverPosition = new(zone.localZoneCenterPosition.X + (-offset + rand.NextInt(offset * 2 + 1)), zone.localZoneCenterPosition.Y + (-offset + rand.NextInt(offset * 2 + 1))); //Offset it randomly up to a point
        River lake = new(riverPosition, new Vec2d(riverPosition.X + (-lakeSize + rand.NextInt(lakeSize * 2)), riverPosition.Y + (-lakeSize + rand.NextInt(lakeSize * 2)))); //End is 100 blocks in a random direction
        lake.startSize = lakeSize;
        lake.endSize = lakeSize;

        lake.segments = new RiverSegment[1];
        lake.segments[0] = new RiverSegment(lake.startPoint, lake.endPoint, lake.startPoint + (lake.endPoint - lake.startPoint) / 2);
        lake.segments[0].parent = lake.segments[0];
        lake.segments[0].children.Add(lake.segments[0]);
        lake.segments[0].parentInvalid = true;

        lake.speed = 0;

        lake.segments[0].river = lake;

        zone.rivers.Add(lake);
    }

    /// <summary>
    /// Gets zones in a radius around a zone.
    /// </summary>
    public List<TectonicZone> GetZonesAround(int localZoneX, int localZoneZ, int radius = 1)
    {
        List<TectonicZone> zonesListerino = new();

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                if (localZoneX + x < 0 || localZoneX + x > zonesInPlate - 1 || localZoneZ + z < 0 || localZoneZ + z > zonesInPlate - 1) continue;

                zonesListerino.Add(zones[localZoneX + x, localZoneZ + z]);
            }
        }

        return zonesListerino;
    }

    /// <summary>
    /// Picks a biome from the climate of the nearest region (512 * 512).
    /// Actually should calculate the noise regularly?
    /// </summary>
    public ushort PickBiome(double worldX, double worldZ, bool coastal)
    {
        double A = 255;
        double P = halfRange;
        double z = worldZ / 512 + ZOffSet;

        //Sharktooth temperature generation
        int preTemp = (int)(A / P * (P - Math.Abs(Math.Abs(z) % (2 * P) - P)));
        int temperature = GameMath.Clamp((int)(preTemp * tempMul), 0, 255);

        //Samples rain noise. 0-1 * 255
        int rain = (int)(ClimateNoisePatch.NoiseClimateRealisticPrefix.rainNoise.GetPosNoise((int)worldX, (int)worldZ) * 255 * rainMul);

        return (ushort)biomeSystem.SelectBiome(temperature, rain, (int)worldX, (int)worldZ, coastal);
    }
}