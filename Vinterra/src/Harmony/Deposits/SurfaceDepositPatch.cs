using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

/// <summary>
/// If patches are disabled in a biome, don't spawn them here.
/// </summary>
public class SurfaceDepositPatch
{
    [HarmonyPatch(typeof(DiscDepositGenerator))]
    [HarmonyPatch("GenDeposit")]
    public static class GenDepositTranspiler
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new(instructions);
            int insertionIndex = -1;

            object operand = null;

            for (int i = 0; i < code.Count - 4; i++)
            {
                if (code[i].opcode == OpCodes.Brtrue_S && code[i - 1].opcode == OpCodes.Callvirt && code[i - 1].operand == AccessTools.Method(typeof(IChunkBlocks), "GetBlockIdUnsafe"))
                {
                    insertionIndex = i + 1;
                    operand = code[i].operand; //Get jump operand
                    break;
                }
            }

            List<CodeInstruction> ins = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "Api")),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SurfaceDepositPatch), "CanGen")),
                new CodeInstruction(OpCodes.Brfalse_S, operand)
            };

            if (insertionIndex != -1)
            {
                code.InsertRange(insertionIndex, ins);
            }

            return code;
        }
    }

    public static bool CanGen(ICoreServerAPI sapi, IServerChunk[] chunks)
    {
        ushort biomeId = chunks[0].GetModdata<ushort[]>("biomeMap", null)[512];
        return BiomeSystem.Get(sapi).biomes[biomeId].spawnPatches;
    }
}