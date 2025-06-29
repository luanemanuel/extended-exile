using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace ExtendedExile.Patches
{
    public class PatchPlayerStatusScroll : MonoBehaviour
    {
        private Button _upBtn, _downBtn;
        private ScrollRect _scrollRect;
        private RectTransform _contentRect, _vpRect;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(SetupScroll(scene.name));
        }

        void LateUpdate()
        {
            UpdateButtonVisibility();
        }

        IEnumerator SetupScroll(string targetScene)
        {
            yield return new WaitForEndOfFrame();

            var oldContainer = GameObject.Find(
                "Important game obj/TentPrefab/PC (1)/Screen1_low/Mind_screen/Canvas/Panel/StatusPlayerContainer"
            );

            if (oldContainer == null)
            {
                Debug.LogWarning("[ExtendedExile] StatusPlayerContainer não encontrada.");
                yield break;
            }

            while (oldContainer.transform.childCount == 0 &&
                   SceneManager.GetActiveScene().name == targetScene)
            {
                yield return null;
            }

            if (SceneManager.GetActiveScene().name != targetScene)
                yield break;

            var parent = oldContainer.transform.parent;
            var oldRect = oldContainer.GetComponent<RectTransform>();
            var oldImage = oldContainer.GetComponent<Image>();

            // === SCROLL VIEW ====================================================
            var scrollGo = new GameObject("StatusScrollView",
                typeof(RectTransform),
                typeof(ScrollRect)
            );
            scrollGo.transform.SetParent(parent, false);

            var newRect = scrollGo.GetComponent<RectTransform>();
            newRect.anchorMin = oldRect.anchorMin;
            newRect.anchorMax = oldRect.anchorMax;
            newRect.pivot = oldRect.pivot;
            newRect.anchoredPosition = oldRect.anchoredPosition;
            newRect.sizeDelta = oldRect.sizeDelta;

            _scrollRect = scrollGo.GetComponent<ScrollRect>();
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;

            // === VIEWPORT =======================================================
            var viewportGo = new GameObject("Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            viewportGo.transform.SetParent(scrollGo.transform, false);

            _vpRect = viewportGo.GetComponent<RectTransform>();
            _vpRect.anchorMin = Vector2.zero;
            _vpRect.anchorMax = Vector2.one;
            _vpRect.anchoredPosition = Vector2.zero;
            _vpRect.sizeDelta = Vector2.zero;

            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.enabled = false;

            viewportGo.GetComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = _vpRect;

            // === CONTENT =========================================================
            oldContainer.transform.SetParent(_vpRect, false);
            _contentRect = oldContainer.GetComponent<RectTransform>();
            _contentRect.anchorMin = new Vector2(0, 1);
            _contentRect.anchorMax = new Vector2(1, 1);
            _contentRect.pivot = new Vector2(0.5f, 1);
            _contentRect.anchoredPosition = Vector2.zero;
            _contentRect.sizeDelta = Vector2.zero;

            var layout = oldContainer.GetComponent<VerticalLayoutGroup>()
                         ?? oldContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = oldContainer.GetComponent<ContentSizeFitter>()
                         ?? oldContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _contentRect;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);

            // === BOTÕES ==========================================================

            const float btnHeight = 40f;

            // Subtrai espaço do viewport
            _vpRect.offsetMin = new Vector2(0, btnHeight);
            _vpRect.offsetMax = new Vector2(0, -btnHeight);

            // Encontra o Label para posicionar o botão ▲ logo abaixo
            var labelObj = GameObject.Find(
                "Important game obj/TentPrefab/PC (1)/Screen1_low/Mind_screen/Canvas/Panel/Label"
            );
            Transform labelParent = labelObj ? labelObj.transform.parent : parent;

            // Botão ▲
            _upBtn = CreateButtonTMP("BtnScrollUp", labelParent,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(0, btnHeight), oldImage);
            _upBtn.GetComponentInChildren<TextMeshProUGUI>().text = "▲";
            _upBtn.onClick.AddListener(() =>
            {
                _scrollRect.verticalNormalizedPosition =
                    Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + 0.1f);
            });

            // Botão ▼ (fica embaixo da scroll)
            _downBtn = CreateButtonTMP("BtnScrollDown", parent,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 0), new Vector2(0, btnHeight), oldImage);
            _downBtn.GetComponentInChildren<TextMeshProUGUI>().text = "▼";
            _downBtn.onClick.AddListener(() =>
            {
                _scrollRect.verticalNormalizedPosition =
                    Mathf.Clamp01(_scrollRect.verticalNormalizedPosition - 0.1f);
            });

            Debug.Log("[ExtendedExile] ScrollView com botões configurada com sucesso!");
        }

        private Button CreateButtonTMP(string buttonName, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta,
            Image referenceImage)
        {
            var go = new GameObject(buttonName, typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = go.GetComponent<Image>();
            if (referenceImage != null)
            {
                img.sprite = referenceImage.sprite;
                img.color = referenceImage.color;
                img.type = referenceImage.type;
            }
            else
            {
                img.enabled = false; // deixa sem imagem
            }

            var textGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 48;
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.offsetMin = Vector2.zero;
            tmp.rectTransform.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        void UpdateButtonVisibility()
        {
            if (_scrollRect == null || _contentRect == null || _vpRect == null || _upBtn == null || _downBtn == null)
                return;

            float contentHeight = _contentRect.rect.height;
            float viewportHeight = _vpRect.rect.height;
            float scrollPos = _scrollRect.verticalNormalizedPosition;

            bool canScroll = (contentHeight - viewportHeight) > 2f;

            // Se não pode rolar, esconde ambos
            if (!canScroll)
            {
                _upBtn.gameObject.SetActive(false);
                _downBtn.gameObject.SetActive(false);
                return;
            }

            // Mostra ▲ se não está no topo
            _upBtn.gameObject.SetActive(scrollPos < 0.999f);

            // Mostra ▼ se não está no fundo
            _downBtn.gameObject.SetActive(scrollPos > 0.001f);
            
            _upBtn.transform.SetAsLastSibling();
            _downBtn.transform.SetAsFirstSibling();
        }
    }
}