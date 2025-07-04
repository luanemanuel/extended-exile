using HarmonyLib;
using System.Linq;
using Photon.Pun;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_State), "CanStartMission")]
    public static class PatchCanStartMission
    {
        static bool Prefix(UI_Lobby_State __instance, ref bool __result)
        {
            if (ForceStartFlag.SkipAllReady)
            {
                __result = true;
                return false;
            }
            
            var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
            var states = (int[])field.GetValue(__instance);
            int players = PhotonNetwork.CurrentRoom.PlayerCount;
            __result = states.Take(players).All(s => s == 1);
            return false;
        }
    }
}