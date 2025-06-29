using System.IO;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using ExtendedExile.Utils;

namespace ExtendedExile
{
    [BepInPlugin("br.com.luanemanuel.extendedexile", "Extended Exile", "1.2.0")]
    public class ExtendedExilePlugin : BaseUnityPlugin
    {
        internal new static Config Config { get; private set; }

        private void Awake()
        {
            Config = new Config(Path.Combine(Paths.ConfigPath, "ExtendedExile.cfg"));
            Config.Load();

            PhotonNetwork.NetworkingClient.EventReceived += ReadyStateReceiver.Instance.OnEvent;
            PhotonNetwork.AutomaticallySyncScene = true;

            new Harmony("br.com.luanemanuel.extendedexile").PatchAll();
            Logger.LogInfo(
                $"Extended Exile v{Info.Metadata.Version} loaded with MaxPlayers = {Config.MaxPlayers}"
            );
        }
        
        private void OnDestroy()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= ReadyStateReceiver.Instance.OnEvent;
        }
    }
}