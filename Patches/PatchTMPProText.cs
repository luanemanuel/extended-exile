using HarmonyLib;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
    public class PatchTMPProText
    {
        static void Prefix(ref string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("Lobby "))
                return;

            var parts = value.Split(' ');
            if (parts.Length < 2)
                return;

            var nums = parts[1].Split('/');
            if (nums.Length != 2 || !int.TryParse(nums[0], out var current))
                return;

            var room = PhotonNetwork.CurrentRoom;
            var max = room?.MaxPlayers ?? 4;
            value = $"Lobby {current}/{max}";
        }
    }
}