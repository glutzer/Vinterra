using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods;

public class ClimateNoisePatch
{
    [HarmonyPatch(typeof(NoiseClimateRealistic))]
    [HarmonyPatch("GetRandomClimate")]
    public static class NoiseClimateRealisticPrefix
    {
        public static Noise rainNoise;

        //16-23 bits = Red = temperature                        >> 16 & 0xFF00
        //8-15 bits = Green = rain                              >> 8 & 0xFF00
        //0-7 bits = Blue = geologic activity                   >> 0 & 0xFF00

        //Rough estimates
        //0 - Very rare, -16c
        //50 - Uncommon, -2c
        //100 - Common, 9c
        //150 - Very common, 22c
        //200 - All the time, 34c
        //255 - All the time, 46c

        //Rain being very common almost half the time and rare being rare might be a problem
        //Look into making rain based on distance from center of plates

        //Z Offset is the distance from the pole
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, double posX, double posZ, NoiseClimateRealistic __instance, ref double ___halfRange, ref float ___tempMul, ref float ___rainMul)
        {
            double A = 255;
            double P = ___halfRange;
            double z = posZ + __instance.ZOffset;

            //Sharktooth temperature generation
            int preTemp = (int)(A / P * (P - Math.Abs(Math.Abs(z) % (2 * P) - P)));
            int temperature = GameMath.Clamp((int)(preTemp * ___tempMul), 0, 255);

            //Samples rain noise. 0-1 * 255
            int rain = (int)(rainNoise.GetPosNoise(posX * 512, posZ * 512) * 255 * ___rainMul);

            //int geo = (int)(geoNoise.GetPosNoise(posX, posZ) * 255);
            int geo = (int)Math.Max(0, Math.Pow(__instance.NextInt(256) / 255f, __instance.GetField<float>("geologicActivityInv")) * 255); //Vanilla geologic activity calculation

            __result = (temperature << 16) + (rain << 8) + geo; //Geologic activity spawns hot springs (for now)
            //__result = (50 << 16) + (50 << 8) + geo;
            return false;
        }
    }

    public static double ZOffset;
    public static double halfRange;

    //Default 1.5. Subtracted by distance above sea level divided by this
    //How much slower it will get colder as y increases
    public static int newHeightDivision = 10;

    //How much slower it will get rainer as y increases
    public static int rainDivisor = 10;

    //Sea level, probably shouldn't be here
    public static int seaLevel = 100;

    //Terragen config. Used for worldgen climate

    [HarmonyPatch(typeof(NoiseClimateRealistic))]
    [HarmonyPatch(new Type[] { typeof(long), typeof(double), typeof(int), typeof(int), typeof(int) })]
    [HarmonyPatch(MethodType.Constructor)]
    public static class NoiseClimateRealisticPostfix
    {
        [HarmonyPostfix]
        public static void Postfix(NoiseClimateRealistic __instance)
        {
            ZOffset = __instance.ZOffset;
            halfRange = __instance.GetField<double>("halfRange");
        }
    }

    [HarmonyPatch(typeof(TerraGenConfig))]
    [HarmonyPatch("GetScaledAdjustedTemperature")]
    public static class TGCGetScaledAdjustedTemperaturePrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int unscaledTemp, int distToSealevel)
        {
            __result =  GameMath.Clamp((int)(((float)unscaledTemp - (float)distToSealevel / newHeightDivision) / 4.25f) - 20, -20, 40);

            return false;
        }
    }

    [HarmonyPatch(typeof(TerraGenConfig))]
    [HarmonyPatch("GetScaledAdjustedTemperatureFloat")]
    public static class TGCGetScaledAdjustedTemperatureFloatPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result, int unscaledTemp, int distToSealevel)
        {
            __result = GameMath.Clamp(((float)unscaledTemp - (float)distToSealevel / newHeightDivision) / 4.25f - 20f, -20f, 40f);

            return false;
        }
    }

    [HarmonyPatch(typeof(TerraGenConfig))]
    [HarmonyPatch("GetAdjustedTemperature")]
    public static class TGCGetAdjustedTemperaturePrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int unscaledTemp, int distToSealevel)
        {
            __result = (int)GameMath.Clamp((float)unscaledTemp - (float)distToSealevel / newHeightDivision, 0f, 255f);

            return false;
        }
    }

    [HarmonyPatch(typeof(TerraGenConfig))]
    [HarmonyPatch("GetRainFall")]
    public static class TGCGetRainfallPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int rainfall, int y)
        {
            __result = Math.Min(255, rainfall + (y - seaLevel) / rainDivisor + 5 * GameMath.Clamp(8 + seaLevel - y, 0, 8)); //Divided by 10 instead of 2. Maybe divide by 20 here too

            return false;
        }
    }

    //Used for real time climate

    //Client World Map

    [HarmonyPatch(typeof(ClientWorldMap))]
    [HarmonyPatch("GetScaledAdjustedTemperatureFloat")]
    public static class CWMGetScaledAdjustedTemperatureFloatPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result, int temp, int distToSealevel)
        {
            __result = GameMath.Clamp(((float)temp - (float)distToSealevel / newHeightDivision) / 4.25f - 20f, -50f, 40f);
            return false;
        }
    }

    [HarmonyPatch(typeof(ClientWorldMap))]
    [HarmonyPatch("GetAdjustedTemperature")]
    public static class CWMGetAdjustedTemperaturePrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int temp, int distToSealevel)
        {
            __result = GameMath.Clamp(temp - distToSealevel / newHeightDivision, 0, 255);
            return false;
        }
    }

    [HarmonyPatch(typeof(ClientWorldMap))]
    [HarmonyPatch("GetRainFall")]
    public static class CWMGetRainfallPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int rainfall, int y)
        {
            __result = GameMath.Clamp(rainfall + (y - seaLevel) / rainDivisor + 5 * GameMath.Clamp(8 + seaLevel - y, 0, 8), 0, 255);
            return false;
        }
    }

    //Server world map

    [HarmonyPatch(typeof(ServerWorldMap))]
    [HarmonyPatch("GetScaledAdjustedTemperatureFloat")]
    public static class SWMGetScaledAdjustedTemperatureFloatPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result, int temp, int distToSealevel)
        {
            __result = GameMath.Clamp(((float)temp - (float)distToSealevel / newHeightDivision) / 4.25f - 20f, -50f, 40f);
            return false;
        }
    }

    [HarmonyPatch(typeof(ServerWorldMap))]
    [HarmonyPatch("GetRainFall")]
    public static class SWMGetRainfallPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int __result, int rainfall, int y)
        {
            __result = GameMath.Clamp(rainfall + (y - seaLevel) / rainDivisor + 5 * GameMath.Clamp(8 + seaLevel - y, 0, 8), 0, 255);
            return false;
        }
    }

    [HarmonyPatch(typeof(BlockSoil))]
    [HarmonyPatch("getClimateSuitedGrowthStage")]
    public static class getClimateSuitedGrowthStagePrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(BlockSoil __instance, ref int __result, IWorldAccessor world, BlockPos pos, ClimateCondition climate)
        {
            if (climate == null)
            {
                __result = __instance.GetField<int>("currentStage");
                return false;
            }

            float rainNormal = climate.WorldgenRainfall;

            int coverageIndex = 1;
            if (rainNormal >= 0.33f)
            {
                coverageIndex = 3;
            }
            else if (rainNormal >= 0.12f)
            {
                coverageIndex = 2;
            }

            __result = coverageIndex;
            return false;
        }
    }
}