using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(
        typeof(UI_Lobby_State),
        "Rpc_SyncReadysState",
        new[] { typeof(int), typeof(int), typeof(int), typeof(int) }
    )]
    static class PatchSyncReadysStateExpand
    {
        // Prefix substitui o array interno pelos valores expandidos
        static bool Prefix(
            UI_Lobby_State __instance,
            int ƛƸƐśǳ,
            int ƿźŮŉ\u01C8,
            int łƞġǷŗ,
            int ĊǣƕƩ\u01CB
        )
        {
            int maxPlayers = ExtendedExilePlugin.Config.MaxPlayers;
            var expanded = new int[maxPlayers];

            // copia dos 4 valores originais
            expanded[0] = ƛƸƐśǳ;
            expanded[1] = ƿźŮŉ\u01C8;
            expanded[2] = łƞġǷŗ;
            expanded[3] = ĊǣƕƩ\u01CB;

            // escreve no campo interno
            var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
            field.SetValue(__instance, expanded);

            // força atualização da UI
            UI_Lobby_Ready.instance ??= UnityEngine.Object.FindFirstObjectByType<UI_Lobby_Ready>();
            UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();

            return false; // cancela execução original
        }
    }
}