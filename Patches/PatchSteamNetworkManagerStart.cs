using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ExtendedExile;
using ExtendedExile.Utils;

namespace ExtendedExile.Patches
{
    [HarmonyPatch]
    public class PatchSteamNetworkManagerStart
    {
        static MethodBase TargetMethod()
        {
            // pega SteamNetworkManager.Start()
            var type = AccessTools.TypeByName("SteamNetworkManager");
            return AccessTools.Method(type, "Start");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var configProp = typeof(ExtendedExilePlugin)
                                 .GetProperty(nameof(ExtendedExilePlugin.Config),
                                     BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             ?? throw new InvalidOperationException("Propriedade Config não encontrada");
            var configGetter = configProp.GetGetMethod(true)
                               ?? throw new InvalidOperationException("Getter de Config não encontrado");

            var maxPlayersProp = typeof(Config)
                                     .GetProperty(nameof(Config.MaxPlayers),
                                         BindingFlags.Instance | BindingFlags.Public)
                                 ?? throw new InvalidOperationException("Propriedade MaxPlayers não encontrada");
            var maxPlayersGetter = maxPlayersProp.GetGetMethod(true)
                                   ?? throw new InvalidOperationException("Getter de MaxPlayers não encontrado");

            foreach (var ci in instructions)
            {
                if (ci.opcode == OpCodes.Ldc_I4_4)
                {
                    // substitui o literal 4 por:
                    //   ldsfld ExtendedExilePlugin::Config
                    //   callvirt instance int32 Config::get_MaxPlayers()
                    yield return new CodeInstruction(OpCodes.Ldsfld, configGetter);
                    yield return new CodeInstruction(OpCodes.Callvirt, maxPlayersGetter);
                }
                else
                {
                    yield return ci;
                }
            }
        }
    }
}