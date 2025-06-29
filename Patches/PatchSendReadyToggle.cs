using System;
using ExitGames.Client.Photon;
using ExtendedExile.Utils;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(UI_Lobby_State), "SendReadyToggle")]
    public static class PatchSendReadyToggle
    {
        static bool Prefix(UI_Lobby_State __instance)
        {
            var fi     = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
            var states = (int[])fi.GetValue(__instance);
            
            var list = PhotonNetwork.PlayerList;
            var count = list.Length;
            
            if (states == null || states.Length != count)
            {
                states = new int[count];
            }
            
            var idx   = Array.IndexOf(list, PhotonNetwork.LocalPlayer);
            if (idx < 0) return false;

            states[idx] = states[idx] == 1 ? 0 : 1;
            fi.SetValue(__instance, states);

            // envia via evento customizado todos os slots
            PhotonNetwork.RaiseEvent(
                ExtendedExileEvents.SyncReadyStates,
                states,
                new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.All,
                    CachingOption = EventCaching.AddToRoomCache
                },
                new SendOptions { Reliability = true }
            );

            UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();
            return false;
        }
    }
}