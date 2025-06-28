using System.IO;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Reflection;

namespace ExtendedExile
{
    [BepInPlugin("br.com.luanemanuel.extendedexile", "Extended Exile", "1.0.0")]
    public class ExtendedExilePlugin : BaseUnityPlugin
    {
        internal new static Config Config { get; private set; }

        private void Awake()
        {
            Config = new Config(Path.Combine(Paths.ConfigPath, "ExtendedExile.cfg"));
            Config.Load();

            PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
            PhotonNetwork.AutomaticallySyncScene = true;

            new Harmony("br.com.luanemanuel.extendedexile").PatchAll();
            Logger.LogInfo(
                $"Extended Exile v{Info.Metadata.Version} loaded with MaxPlayers = {Config.MaxPlayers}"
            );
        }

        private void OnEventReceived(EventData photonEvent)
        {
            if (photonEvent.Code != ExtendedExileEvents.SyncReadyStates)
                return;

            var states = (int[])photonEvent.CustomData;
            var lobbyType = AccessTools.TypeByName("UI_Lobby_State");
            var field = AccessTools.Field(lobbyType, "ƩńćŴǗ");
            var instance = FindFirstObjectByType(lobbyType);
            field.SetValue(instance, states);

            UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();
        }
    }
}