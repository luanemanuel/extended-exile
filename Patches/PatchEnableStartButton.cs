using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_Ready), "UpdateLobbyPlayerList")]
    static class PatchEnableStartButton
    {
        static void Postfix(UI_Lobby_Ready __instance)
        {
            var btnField = AccessTools.Field(typeof(UI_Lobby_Ready), "StartButton");
            if (btnField == null)
            {
                Debug.LogWarning("[ExtendedExile] Campo StartButton não encontrado em UI_Lobby_Ready");
                return;
            }

            var startButton = btnField.GetValue(__instance) as Button;
            if (startButton != null)
            {
                startButton.interactable = UI_Lobby_State.instance.CanStartMission();
                Debug.Log($"[ExtendedExile] StartButton interactivity set to {startButton.interactable}");
            }
        }
    }
}