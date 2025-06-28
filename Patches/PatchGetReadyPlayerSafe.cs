using HarmonyLib;
using UnityEngine;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_State), "GetReadyPlayer")]
    public static class PatchGetReadyPlayerSafe
    {
        static bool Prefix(UI_Lobby_State __instance, int ƛƪƇůǀ, ref bool __result)
        {
            var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
            var array = (int[])field.GetValue(__instance);

            if (ƛƪƇůǀ >= 0 && ƛƪƇůǀ < array.Length)
                __result = array[ƛƪƇůǀ] == 1;
            else
            {
                __result = false;
                Debug.LogWarning($"[ExtendedExile] ⚠️ Índice inválido em GetReadyPlayer({ƛƪƇůǀ})");
            }

            return false;
        }
    }
}