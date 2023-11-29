using HarmonyLib;
using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

public class VinterraMod : ModSystem
{
    public ICoreClientAPI capi;
    public ICoreServerAPI sapi;

    public static bool patched = false;

    public AmbientModifier biomeAmbient;
    public static string lastBiome = "None";
    public BiomeSystem biomeSystem;

    public Harmony harmony;

    public IServerNetworkChannel serverChannel;

    //If in a dev environent the config is overwritten every time
    public bool devEnvironment = true;

    public static int aboveSeaHeight;

    public static float GetFactorFromBlocks(int blocks)
    {
        return (float)blocks / aboveSeaHeight;
    }

    public override double ExecuteOrder()
    {
        return 0.00001;
    }

    //Client-side biome ids
    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;

        if (VConfig.Loaded.biomeGui) (capi.World as ClientMain).RegisterDialog(new HudElementBiome(capi));

        capi.Ambient.CurrentModifiers["biome"] = new AmbientModifier().EnsurePopulated();
        biomeAmbient = capi.Ambient.CurrentModifiers["biome"];

        capi.Network.RegisterChannel("vinterra")
            .RegisterMessageType(typeof(BiomeDataPacket))
            .SetMessageHandler<BiomeDataPacket>(UpdateBiomeData);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        biomeSystem = BiomeSystem.Get(sapi);

        serverChannel = sapi.Network.RegisterChannel("vinterra")
            .RegisterMessageType(typeof(BiomeDataPacket));

        sapi.Event.RegisterGameTickListener((dt) =>
        {
            foreach (IPlayer player in sapi.World.AllOnlinePlayers)
            {
                if (player.Entity == null) continue;
                Vec3d playerPos = player.Entity.ServerPos.XYZ;
                ushort[] biomeData = sapi.World.BlockAccessor.GetChunk((int)(playerPos.X / 32), 0, (int)(playerPos.Z / 32))?.GetModdata<ushort[]>("biomeMap");
                if (biomeData != null)
                {
                    BiomeType biome = biomeSystem.biomes[biomeData[(int)(playerPos.Z % 32) * 32 + (int)(playerPos.X % 32)]];
                    BiomeDataPacket packet = new()
                    {
                        name = biome.name,
                        fogColor = biome.fogColor,
                        fogWeight = biome.fogWeight,
                        fogDensity = biome.fogDensity,
                        ambientColor = biome.ambientColor,
                        ambientWeight = biome.ambientWeight
                    };
                    serverChannel.SendPacket(packet, (IServerPlayer)player);
                }
            }
        }, 1000);

        ClimateNoisePatch.NoiseClimateRealisticPrefix.rainNoise = new Noise(sapi.WorldManager.Seed, 0.00005f, 2);
        aboveSeaHeight = sapi.WorldManager.MapSizeY - 100 - 4; //384 = 280

        //Load initial assets if the files don't exist yet
        sapi.Assets.Reload(AssetCategory.config);
        List<IAsset> initials = api.Assets.GetMany("config/vinterra", "game");
        foreach (IAsset asset in initials)
        {
            try
            {
                string path = GamePaths.DataPath + '/' + Path.GetRelativePath("config", asset.Location.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path)) continue;
                File.WriteAllText(path, asset.ToText());
            }
            catch {}
        }
    }

    public void UpdateBiomeData(BiomeDataPacket packet)
    {
        biomeAmbient.FogColor.Set(packet.fogColor, packet.fogWeight);
        biomeAmbient.FogDensity.Set(packet.fogDensity, packet.fogWeight);
        biomeAmbient.AmbientColor.Set(packet.ambientColor, packet.ambientWeight);

        lastBiome = packet.name;
    }

    public override void StartPre(ICoreAPI api)
    {
        if (!patched)
        {
            harmony = new Harmony("vinterra");
            harmony.PatchAll();
            patched = true;
        }

        string cfgFileName = "vinterra.json";
        try
        {
            VConfig fromDisk;
            if ((fromDisk = api.LoadModConfig<VConfig>(cfgFileName)) == null || devEnvironment)
            {
                api.StoreModConfig(VConfig.Loaded, cfgFileName);
            }
            else
            {
                VConfig.Loaded = fromDisk;
            }
        }
        catch
        {
            api.StoreModConfig(VConfig.Loaded, cfgFileName);
        }

        ClimateNoisePatch.newHeightDivision = VConfig.Loaded.temperatureDivisor;
        ClimateNoisePatch.rainDivisor = VConfig.Loaded.rainDivisor;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterEntity("SandwormHead", typeof(SandwormHead));
        api.RegisterEntity("SandwormSegment", typeof(SandwormSegment));
    }
}

/// <summary>
/// Used to give fog, ambient, and biome name to the client.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BiomeDataPacket
{
    public string name;
    public float[] fogColor;
    public float fogWeight;
    public float fogDensity;
    public float[] ambientColor;
    public float ambientWeight;
}