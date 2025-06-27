using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Photon.Realtime;
using UnityEngine;

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

[HarmonyPatch]
public class PatchSteamNetworkManagerStart
{
    static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("SteamNetworkManager");
        return AccessTools.Method(type, "Start");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            if (code.opcode == OpCodes.Ldc_I4_4)
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.PropertyGetter(typeof(Config),
                        nameof(Config.MaxPlayers)));
            else
                yield return code;
        }
    }
}