using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

/// <summary>
/// Disable surface caves for flats. Maybe this needs to be put in a more convenient place because it's still being checked thousands of times.
/// </summary>
public class SurfaceCavesPatch
{
    [HarmonyPatch(typeof(GenCaves))]
    [HarmonyPatch("SetBlocks")]
    public static class SetBlocksTranspiler
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = new(instructions);
            int insertionIndex = -1;

            //Need label if it needs to jump to something
            Label nextLabel = il.DefineLabel();

            bool found = false;

            for (int i = 0; i < code.Count - 4; i++) //-1 since checking i + 1
            {
                if (!found && code[i].opcode == OpCodes.Bne_Un_S && code[i - 1].opcode == OpCodes.Ldloc_S && code[i - 2].opcode == OpCodes.Ldelem_U2 && code[i - 3].opcode == OpCodes.Add && code[i - 4].opcode == OpCodes.Ldloc_S)
                {
                    insertionIndex = i + 1;
                    found = true;
                }

                if (code[i].opcode == OpCodes.Ldloc_S && code[i - 1].opcode == OpCodes.Callvirt && code[i - 1].operand == AccessTools.Method(typeof(IChunkBlocks), "SetBlockAir"))
                {
                    code[i].labels.Add(nextLabel);
                    break;
                }
            }

            List<CodeInstruction> ins = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GenPartial), "api")),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SurfaceCavesPatch), "CanCarve")),
                new CodeInstruction(OpCodes.Brfalse, nextLabel) //Operand is the label to jump to
            };

            if (insertionIndex != -1)
            {
                code.InsertRange(insertionIndex, ins);
            }

            return code;
        }
    }

    public static bool CanCarve(ICoreServerAPI sapi, IServerChunk[] chunks)
    {
        ushort biomeId = chunks[0].GetModdata<ushort[]>("biomeMap", null)[512];
        return BiomeSystem.Get(sapi).biomes[biomeId].surfaceCaves;
    }
}