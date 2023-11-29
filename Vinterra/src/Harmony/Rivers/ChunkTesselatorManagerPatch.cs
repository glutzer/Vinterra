﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.Client.NoObf;

public class ChunkTesselatorManagerPatch
{
    //Set this to use an extended version of the cache
    [HarmonyPatch(typeof(ChunkTesselator), "Start")]
    public static class StartPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(ChunkTesselator __instance)
        {
            __instance.SetField("vars", new TCTCacheTwo(__instance));
            return true;
        }
    }

    public static ClientChunk bottomChunk = null;

    [HarmonyPatch(typeof(ChunkTesselatorManager))]
    [HarmonyPatch("TesselateChunk")]
    public static class TesselateChunkPrefix
    {
        [HarmonyPrefix]
        public static bool Prefix(int chunkX, int chunkZ, ref ClientMain ___game)
        {
            bottomChunk = ___game.GetField<ClientWorldMap>("WorldMap").CallAmbigMethod<ClientChunk>("GetClientChunk", new System.Type[] { typeof(int), typeof(int), typeof(int) }, chunkX, 0, chunkZ);
            return true;
        }
    }

    [HarmonyPatch(typeof(ChunkTesselatorManager))]
    [HarmonyPatch("TesselateChunk")]
    public static class TesselateChunkPostfix
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            bottomChunk = null;
        }
    }

    [HarmonyPatch(typeof(ChunkTesselatorManager))]
    [HarmonyPatch("TesselateChunk")]
    public static class TesselateChunkTranspiler
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new(instructions);
            int insertionIndex = -1;

            for (int i = 4; i < code.Count - 4; i++) //-1 since checking i + 1
            {
                if (code[i].opcode == OpCodes.Ldloc_0 && code[i + 1].opcode == OpCodes.Ldc_I4_1 && code[i + 2].opcode == OpCodes.Stfld && code[i + 2].operand == AccessTools.Field(typeof(ClientChunk), "queuedForUpload"))
                {
                    insertionIndex = i;
                    break;
                }
            }

            List<CodeInstruction> ins = new()
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ChunkTesselator), "vars")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ClientSystem), "game")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_3),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ChunkTesselatorManagerPatch), "SetFlowVectors"))
            };

            if (insertionIndex != -1)
            {
                code.InsertRange(insertionIndex, ins);
            }

            return code;
        }
    }

    public static void SetFlowVectors(TCTCache vars, ClientMain game, int chunkX, int chunkZ)
    {
        TCTCacheTwo varsTwo = (TCTCacheTwo)vars;

        if (bottomChunk != null && !bottomChunk.Empty)
        {
            varsTwo.flowVectorsX = bottomChunk.GetModdata<float[]>("flowVectorsX");
            varsTwo.flowVectorsZ = bottomChunk.GetModdata<float[]>("flowVectorsZ");
        }

        //bottomChunk = null;
    }
}