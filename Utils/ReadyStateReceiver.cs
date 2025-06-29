using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using HarmonyLib;

namespace ExtendedExile.Utils;

public class ReadyStateReceiver: IOnEventCallback
{
    public static readonly ReadyStateReceiver Instance = new ReadyStateReceiver();

    private ReadyStateReceiver() { }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ExtendedExileEvents.SyncReadyStates)
            return;

        var data   = (int[])photonEvent.CustomData;

        var list   = PhotonNetwork.PlayerList;
        int count  = list.Length;

        var states = new int[count];
        for (int i = 0; i < Math.Min(data.Length, count); i++)
            states[i] = data[i];

        var field = AccessTools.Field(typeof(UI_Lobby_State), "ƩńćŴǗ");
        var lobby = UnityEngine.Object.FindFirstObjectByType<UI_Lobby_State>();
        field.SetValue(lobby, states);

        UI_Lobby_Ready.instance ??= UnityEngine.Object.FindFirstObjectByType<UI_Lobby_Ready>();
        UI_Lobby_Ready.instance?.UpdateLobbyPlayerList();
    }
}
