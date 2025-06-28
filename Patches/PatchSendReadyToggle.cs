using System;
using ExitGames.Client.Photon;
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
            int idx   = Array.IndexOf(list, PhotonNetwork.LocalPlayer);
            if (idx < 0) return false;
            
            if (idx >= states.Length)
            {
                var expanded = new int[idx + 1];
                Array.Copy(states, expanded, states.Length);
                states = expanded;
            }

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