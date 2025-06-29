using HarmonyLib;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(
        typeof(UI_Lobby_State),
        "Rpc_SyncReadysState",
        new[] { typeof(int), typeof(int), typeof(int), typeof(int) }
    )]
    static class PatchBlockRpcSyncReady
    {
        static bool Prefix()
        {
            return false;
        }
    }
}