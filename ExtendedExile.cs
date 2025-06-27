using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

namespace ExtendedExile;

[BepInPlugin("br.com.luanemanuel.extendedexile", "Extended Exile", "1.0.0")]
public class ExtendedExilePlugin : BaseUnityPlugin
{
    internal new static Config Config;

    private void Awake()
    {
        Config = new Config(Path.Combine(Paths.ConfigPath, "ExtendedExile.cfg"));
        Config.Load();

        var harmony = new Harmony("br.com.luanemanuel.extendedexile");
        harmony.PatchAll();

        Logger.LogInfo($"Extended Exile v{Info.Metadata.Version} loaded with MaxPlayers = {Config.MaxPlayers}");
    }
}

public class Config(string path)
{
    private string Path { get; } = path;
    public int MaxPlayers { get; private set; } = 8;

    public void Load()
    {
        if (File.Exists(Path))
        {
            foreach (var line in File.ReadAllLines(Path))
            {
                if (line.StartsWith("MaxPlayers=") && int.TryParse(line.Split('=')[1], out var v))
                    MaxPlayers = Mathf.Clamp(v, 1, 20);
            }
        }
        else
        {
            File.WriteAllText(Path, "# Max allowed players (1–20)\nMaxPlayers=" + MaxPlayers);
        }
    }
}

// 1) Transpiler no SteamNetworkManager.Start
[HarmonyPatch]
public class PatchSteamNetworkManagerStart
{
    static MethodBase TargetMethod()
    {
        // pega SteamNetworkManager.Start()
        var type = AccessTools.TypeByName("SteamNetworkManager");
        return AccessTools.Method(type, "Start");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // preparos para carregar nossa Config
        var cfgField = AccessTools.Field(typeof(ExtendedExilePlugin), nameof(ExtendedExilePlugin.Config));
        var getter = AccessTools.PropertyGetter(typeof(Config), nameof(Config.MaxPlayers));

        foreach (var ci in instructions)
        {
            if (ci.opcode == OpCodes.Ldc_I4_4)
            {
                // substitui o literal 4 por:
                //   ldsfld ExtendedExilePlugin::Config
                //   callvirt instance int32 Config::get_MaxPlayers()
                yield return new CodeInstruction(OpCodes.Ldsfld, cfgField);
                yield return new CodeInstruction(OpCodes.Callvirt, getter);
            }
            else
            {
                yield return ci;
            }
        }
    }
}

// 2) Prefix em PhotonNetwork.CreateRoom
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

// 3) Prefix em PhotonNetwork.JoinOrCreateRoom (caso o jogo use)
[HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.JoinOrCreateRoom),
    new[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) })]
public class PatchJoinOrCreateRoom
{
    static void Prefix(
        string roomName,
        ref RoomOptions roomOptions,
        TypedLobby typedLobby,
        string[] expectedUsers)
    {
        roomOptions.MaxPlayers = ExtendedExilePlugin.Config.MaxPlayers;
        Debug.Log($"[ExtendedExile] JoinOrCreateRoom patched ({roomName}): MaxPlayers = {roomOptions.MaxPlayers}");
    }
}

// 4) Patch TMP_Text.text setter
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
        Debug.Log($"[ExtendedExile] TMPPro text patched: {value}");
    }
}