using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.CreateRoom),
        new[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) })]
    public class PatchCreateRoom
    {
        static void Prefix(
            string roomName,
            ref RoomOptions roomOptions,
            TypedLobby typedLobby,
            string[] expectedUsers)
        {
            roomOptions.MaxPlayers = ExtendedExilePlugin.Config.MaxPlayers;
            Debug.Log($"[ExtendedExile] CreateRoom patched ({roomName}): MaxPlayers = {roomOptions.MaxPlayers}");
        }
    }
}