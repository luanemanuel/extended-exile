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
using System;
using System.Linq;

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

// 5) Postfix no UI_Lobby_State.Start para redimensionar readyStates
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

// 6) Prefix no UI_Lobby_State.GetReadyPlayer para evitar IndexOutOfRangeException
[HarmonyPatch(typeof(UI_Lobby_State), "GetReadyPlayer")]
public static class PatchGetReadyPlayerSafe
{
    static bool Prefix(UI_Lobby_State __instance, int ƛƪƇůǀ, ref bool __result)
    {
        var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        var array = (int[])field.GetValue(__instance);

        if (ƛƪƇůǀ >= 0 && ƛƪƇůǀ < array.Length)
            __result = array[ƛƪƇůǀ] == 1;
        else
        {
            __result = false;
            Debug.LogWarning($"[ExtendedExile] ⚠️ Índice inválido em GetReadyPlayer({ƛƪƇůǀ})");
        }

        return false;
    }
}

// 7) Patch no método Rpc_SyncReadysState para expandir o array de estados

[HarmonyPatch]
static class PatchSyncReadysStateExpand
{
    // Aponta corretamente para o RPC que recebe params object[]
    static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(UI_Lobby_State),
            "Rpc_SyncReadysState",
            new[] { typeof(object[]) });

    static bool Prefix(UI_Lobby_State __instance, object[] __args)
    {
        int maxPlayers = ExtendedExilePlugin.Config.MaxPlayers;
        var expandedStates = new int[maxPlayers];

        int toCopy = Math.Min(__args.Length, maxPlayers);
        for (int i = 0; i < toCopy; i++)
            expandedStates[i] = (int)__args[i];

        var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        field.SetValue(__instance, expandedStates);

        Debug.Log("[ExtendedExile] 📡 Estados de prontos sincronizados e expandidos.");

        if (UI_Lobby_Ready.instance == null)
            UI_Lobby_Ready.instance =
                UnityEngine.Object.FindFirstObjectByType<UI_Lobby_Ready>();

        UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();

        return false;
    }
}

[HarmonyPatch(typeof(UI_Lobby_State), "SendReadyToggle")]
public static class PatchSendReadyToggle
{
    static bool Prefix(UI_Lobby_State __instance)
    {
        // 1) atualiza localmente o array de estados
        var fi = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        var states = (int[])fi.GetValue(__instance);
        int idx = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        if (idx < 0 || idx >= states.Length)
            return false;

        states[idx] = states[idx] == 1 ? 0 : 1;
        fi.SetValue(__instance, states);

        // 2) faz o RPC para todo mundo com buffer
        var pv = __instance.GetComponent<PhotonView>();
        // envia todos os estados (ou só os 4 originais, conforme implementação original)
        object[] payload = states.Cast<object>().ToArray();
        pv.RPC("Rpc_SyncReadysState", RpcTarget.AllBuffered, payload);

        // 3) atualiza a UI local
        UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();
        return false; // impede o original
    }
}

[HarmonyPatch(typeof(UI_Lobby_State), "AllReady")]
public static class PatchAllReady
{
    static bool Prefix(UI_Lobby_State __instance, ref bool __result)
    {
        var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        var states = (int[])field.GetValue(__instance);
        __result = states.All(s => s == 1);
        return false;
    }
}

[HarmonyPatch(typeof(UI_Lobby_State), "CanStartMission")]
public static class PatchCanStartMission
{
    static bool Prefix(UI_Lobby_State __instance, ref bool __result)
    {
        var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        var states = (int[])field.GetValue(__instance);
        int players = PhotonNetwork.CurrentRoom.PlayerCount;
        __result = states.Take(players).All(s => s == 1);
        return false;
    }
}