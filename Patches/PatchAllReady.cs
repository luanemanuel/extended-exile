using HarmonyLib;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_State), "AllReady")]
    public static class PatchAllReady
    {
        static bool Prefix(UI_Lobby_State __instance, ref bool __result)
        {
            if (ForceStartFlag.SkipAllReady)
            {
                ForceStartFlag.SkipAllReady = false;
                __result = true;
                return false;
            }
            
            __result = __instance.CanStartMission();
            return false;
        }
    }
}