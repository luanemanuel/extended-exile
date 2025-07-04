using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ExtendedExile.Patches
{
    public static class ForceStartFlag
    {
        public static bool SkipAllReady;
    }

    public class PatchForceStartButton : MonoBehaviour
    {
        private static AccessTools.FieldRef<UI_Lobby_Ready, UI_Lobby_State> _getState;
        private Button _forceStartBtn;
        private bool _blockInput;

        void Awake()
        {
            DontDestroyOnLoad(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(InjectOnce());
        }

        private IEnumerator InjectOnce()
        {
            yield return new WaitForEndOfFrame();

            Debug.Log("[ExtendedExile] PatchForceStartButton: Iniciando injeção do botão Force Start...");
            // aguarda UI_Lobby_Ready ou UI_Lobby_State aparecer
            Type readyType = FindTypeBySimpleName("UI_Lobby_Ready");
            Debug.Log($"[ExtendedExile] PatchForceStart readyType = {readyType}");
            Component readyComp = null;
            float timer = 0f, lastReadyLog = 0f;
            while (readyComp == null && timer < 60 * 5) // 5 minutos
            {
                yield return null;
                readyComp = (Component)FindFirstObjectByType(readyType);
                timer += Time.deltaTime;
                if (timer - lastReadyLog >= 5f)
                {
                    Debug.Log($"[ExtendedExile] Aguardando UI_Lobby_Ready... {timer:F2}s");
                    lastReadyLog = timer;
                }
            }

            if (readyComp == null) yield break;
            Debug.Log("[ExtendedExile] UI_Lobby_Ready encontrado.");

            // prepara FieldRef para o campo obfuscado ƼǰĀžƳ
            float fieldTimer = 0f, lastFieldLog = 0f;
            while (_getState == null && fieldTimer < 300f)
            {
                yield return null;
                var field = AccessTools.Field(readyType, "ƼǰĀžƳ");
                if (field != null)
                    _getState = AccessTools.FieldRefAccess<UI_Lobby_Ready, UI_Lobby_State>(field);

                fieldTimer += Time.deltaTime;
                if (fieldTimer - lastFieldLog >= 5f)
                {
                    Debug.Log($"[ExtendedExile] Aguardando campo ƼǰĀžƳ... {fieldTimer:F2}s");
                    lastFieldLog = fieldTimer;
                }
            }

            if (_getState == null) yield break;
            Debug.Log("[ExtendedExile] Campo ƼǰĀžƳ encontrado.");

            // obtém instância de UI_Lobby_State
            float stateTimer = 0f, lastStateLog = 0f;
            UI_Lobby_State stateComp = null;
            while (stateComp == null && stateTimer < 300f)
            {
                stateComp = _getState((UI_Lobby_Ready)readyComp);
                stateTimer += Time.deltaTime;
                if (stateTimer - lastStateLog >= 5f)
                {
                    Debug.Log($"[ExtendedExile] Aguardando UI_Lobby_State... {stateTimer:F2}s");
                    lastStateLog = stateTimer;
                }

                yield return null;
            }

            if (stateComp == null)
            {
                Debug.LogError("[ExtendedExile] Falha ao obter UI_Lobby_State após timeout. Abortando injeção.");
                yield break;
            }

            var stateType = stateComp.GetType();
            float methodTimer = 0f, lastMethodLog = 0f;
            MethodInfo startMethod = null;

            // aguarda método StartMission aparecer
            while (startMethod == null && methodTimer < 300f)
            {
                startMethod = stateType.GetMethod(
                    "StartMission",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                methodTimer += Time.deltaTime;
                if (methodTimer - lastMethodLog >= 5f)
                {
                    Debug.Log($"[ExtendedExile] Aguardando StartMission... {methodTimer:F2}s");
                    lastMethodLog = methodTimer;
                }

                yield return null;
            }

            if (startMethod == null)
            {
                Debug.LogError("[ExtendedExile] Método StartMission não encontrado dentro do timeout.");
                yield break;
            }

            Debug.Log("[ExtendedExile] PatchForceStartButton: Método StartMission encontrado, prosseguindo...");

            _blockInput = true;
            StartCoroutine(UpdateButtonInteractivity(stateComp));
            
            // localiza painel "Ready"
            var readyTransform = FindDeep(readyComp.transform, "Ready");
            if (readyTransform == null) yield break;
            Debug.Log("[ExtendedExile] PatchForceStartButton: Painel 'Ready' encontrado, prosseguindo...");
            if (readyTransform.Find("ForceStartButton") != null) yield break;

            // clona botão e ajusta visual
            var original = readyTransform.Find("StartButton")?.gameObject;
            if (original == null) yield break;
            Debug.Log("[ExtendedExile] PatchForceStartButton: Botão 'StartButton' encontrado, prosseguindo...");
            var clone = Instantiate(original, readyTransform, false);
            clone.name = "ForceStartButton";
            var img = clone.GetComponent<Image>();
            img.color = new Color(1f, 0.65f, 0f);
            img.raycastTarget = true;
            var origRt = original.GetComponent<RectTransform>();
            var rt = clone.GetComponent<RectTransform>();
            rt.anchorMin = origRt.anchorMin;
            rt.anchorMax = origRt.anchorMax;
            rt.pivot = origRt.pivot;
            rt.sizeDelta = origRt.sizeDelta;
            rt.anchoredPosition = origRt.anchoredPosition + new Vector2(origRt.sizeDelta.x + 10f, 0);
            clone.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);
            Transform txtTransform = null;
            float timerTxt = 0f, lastLogTxt = 0f;
            while (txtTransform == null && timerTxt < 300f) {
                txtTransform = clone.transform.Find("Text (TMP)");
                if (timerTxt - lastLogTxt >= 5f) {
                    Debug.Log($"[ExtendedExile] Aguardando Text (TMP)... {timerTxt:F2}s");
                    lastLogTxt = timerTxt;
                }
                timerTxt += Time.deltaTime;
                yield return null;
            }

            if (txtTransform != null) {
                var tmp = txtTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null) {
                    tmp.SetText("Force Start");
                    tmp.raycastTarget = false;
                }
            }

            // adiciona listener que chama StartMission em UI_Lobby_State
            var btn = clone.GetComponent<Button>();
            btn.interactable = true;
            btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log(
                    "[ExtendedExile] PatchForceStartButton: Botão Force Start pressionado, chamando StartMission...");
                ForceStartFlag.SkipAllReady = true;
                startMethod.Invoke(stateComp, null);
            });

            _forceStartBtn = btn;
            _forceStartBtn.interactable = true;

            Debug.Log("[ExtendedExile] PatchForceStartButton: Botão Force Start injetado com sucesso!");
        }

        private static Type FindTypeBySimpleName(string simpleName)
        {
            return AccessTools.TypeByName(simpleName)
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a => a.GetTypes())
                       .FirstOrDefault(t => t.Name == simpleName);
        }

        private Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var f = FindDeep(c, name);
                if (f != null) return f;
            }

            return null;
        }

        private IEnumerator UpdateButtonInteractivity(UI_Lobby_State stateComp)
        {
            while (true)
            {
                _blockInput = !stateComp.CanStartMission();
                yield return null;
            }
        }
    }
}