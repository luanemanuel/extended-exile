using System;
using HarmonyLib;
using UnityEngine;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_State), "Awake")]
    public static class PatchReadyArrayResize
    {
        static void Postfix(UI_Lobby_State __instance)
        {
            var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
            var array = (int[])field.GetValue(__instance);

            if (array.Length < ExtendedExilePlugin.Config.MaxPlayers)
            {
                var newArray = new int[ExtendedExilePlugin.Config.MaxPlayers];
                Array.Copy(array, newArray, array.Length);
                field.SetValue(__instance, newArray);
                Debug.Log($"[ExtendedExile] 🔁 Redimensionou readyStates: {array.Length} -> {newArray.Length}");
            }
        }
    }
}