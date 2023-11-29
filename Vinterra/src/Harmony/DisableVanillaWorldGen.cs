using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

public class DisableVanillaWorldGen
{
    //Reworked
    [HarmonyPatch(typeof(GenTerra))]
    [HarmonyPatch("StartServerSide")]
    public static class GenTerraDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    //Removes floating blocks, however I use my own methods for density
    [HarmonyPatch(typeof(GenTerraPostProcess))]
    [HarmonyPatch("StartServerSide")]
    public static class GenTerraPostProcessDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    //Rock strata, this can stay vanilla
    [HarmonyPatch(typeof(GenRockStrataNew))]
    [HarmonyPatch("StartServerSide")]
    public static class GenRockStrataNewDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Cave gen, for now this can stay vanilla
    [HarmonyPatch(typeof(GenCaves))]
    [HarmonyPatch("StartServerSide")]
    public static class GenCavesDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Reworked
    [HarmonyPatch(typeof(GenBlockLayers))]
    [HarmonyPatch("StartServerSide")]
    public static class GenBlockLayersDisable
    {
        [HarmonyPrefix]
        public static bool Prefix(GenBlockLayers __instance, ICoreServerAPI api)
        {
            __instance.SetField("api", api);

            //Block layers for climate growth stage
            __instance.GetField<ICoreServerAPI>("api").Event.InitWorldGenerator(__instance.InitWorldGen, "standard");
            __instance.GetField<ICoreServerAPI>("api").Event.InitWorldGenerator(__instance.InitWorldGen, "superflat");

            //Used in structure generation for some reason
            __instance.distort2dx = new SimplexNoise(new double[] { 14, 9, 6, 3 }, new double[] { 1 / 100.0, 1 / 50.0, 1 / 25.0, 1 / 12.5 }, 100 + 20980);

            return false;
        }
    }

    //This can stay vanilla
    [HarmonyPatch(typeof(GenDeposits))]
    [HarmonyPatch("StartServerSide")]
    public static class GenDepositsDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //This can stay vanilla
    [HarmonyPatch(typeof(GenDungeons))]
    [HarmonyPatch("StartServerSide")]
    public static class GenDungeonsDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Story structures disabled
    [HarmonyPatch(typeof(GenStoryStructures))]
    [HarmonyPatch("StartServerSide")]
    public static class GenStoryStructuresDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    //I don't know what this is for but it's probably for structures
    [HarmonyPatch(typeof(GenStructuresPosPass))]
    [HarmonyPatch("StartServerSide")]
    public static class GenStructuresPosPassDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Structures stay vanilla
    [HarmonyPatch(typeof(GenStructures))]
    [HarmonyPatch("StartServerSide")]
    public static class GenStructuresDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Can stay vanilla
    [HarmonyPatch(typeof(GenHotSprings))]
    [HarmonyPatch("StartServerSide")]
    public static class GenHotSpringsDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Reworked
    [HarmonyPatch(typeof(GenPonds))]
    [HarmonyPatch("StartServerSide")]
    public static class GenPondsDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    //Reworked
    [HarmonyPatch(typeof(GenVegetationAndPatches))]
    [HarmonyPatch("StartServerSide")]
    public static class GenVegetationAndPatchesDisable
    {
        [HarmonyPrefix]
        public static bool Prefix(GenVegetationAndPatches __instance, ICoreServerAPI api)
        {
            __instance.SetField("sapi", api);
            __instance.SetField("treeSupplier", new WgenTreeSupplier(api));
            api.Event.InitWorldGenerator(__instance.initWorldGen, "standard"); //Load config so forest floor can work
            return false;
        }
    }

    //1 block water streams
    [HarmonyPatch(typeof(GenRivulets))]
    [HarmonyPatch("StartServerSide")]
    public static class GenRivuletsDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    //Generates the sun
    [HarmonyPatch(typeof(GenLightSurvival))]
    [HarmonyPatch("StartServerSide")]
    public static class GenLightDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //This is for generating snow on high peaks?
    [HarmonyPatch(typeof(GenSnowLayer))]
    [HarmonyPatch("StartServerSide")]
    public static class GenSnowLayerDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }

    //Vanilla creature gen. I should harmony the actual class to check biome type
    [HarmonyPatch(typeof(GenCreatures))]
    [HarmonyPatch("StartServerSide")]
    public static class GenCreaturesDisable
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return true;
        }
    }
}