using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ExtendedExile.Patches
{
    public class PatchPlayerStatusScroll : MonoBehaviour
    {
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
            // Passa o nome da cena para a coroutine
            StartCoroutine(SetupScroll(scene.name));
        }

        IEnumerator SetupScroll(string targetScene)
        {
            // espera um frame para a UI ser instanciada
            yield return new WaitForEndOfFrame();

            Debug.Log($"[ExtendedExile] Configurando ScrollView em cena: {SceneManager.GetActiveScene().name}");

            // encontra o container original
            var oldContainer = GameObject.Find(
                "Important game obj/TentPrefab/PC (1)/Screen1_low/Mind_screen/Canvas/Panel/StatusPlayerContainer"
            );
            if (oldContainer == null)
            {
                Debug.LogWarning("[ExtendedExile] StatusPlayerContainer não encontrada.");
                yield break;
            }

            // polling até ter ao menos um filho ou cena mudar
            while (oldContainer.transform.childCount == 0 &&
                   SceneManager.GetActiveScene().name == targetScene)
            {
                yield return null;
            }

            if (SceneManager.GetActiveScene().name != targetScene)
            {
                Debug.LogWarning("[ExtendedExile] Cena mudou antes de povoar container, abortando.");
                yield break;
            }

            // pega referencias
            var parent = oldContainer.transform.parent;
            var oldRect = oldContainer.GetComponent<RectTransform>();
            var oldImage = oldContainer.GetComponent<Image>();

            // CRIA O SCROLLVIEW ---------------------------------------------------

            var scrollGo = new GameObject("StatusScrollView",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(Image)
            );
            scrollGo.transform.SetParent(parent, false);

            // replica RectTransform e visual
            var newRect = scrollGo.GetComponent<RectTransform>();
            newRect.anchorMin = oldRect.anchorMin;
            newRect.anchorMax = oldRect.anchorMax;
            newRect.pivot = oldRect.pivot;
            newRect.anchoredPosition = oldRect.anchoredPosition;
            newRect.sizeDelta = oldRect.sizeDelta;

            var img = scrollGo.GetComponent<Image>();
            if (oldImage != null)
            {
                img.sprite = oldImage.sprite;
                img.color = oldImage.color;
                img.type = oldImage.type;
            }
            else
            {
                img.color = new Color(1, 1, 1, 0); // transparente se não houver imagem
            }

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            // VIEWPORT ------------------------------------------------------------

            var viewportGo = new GameObject("Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            viewportGo.transform.SetParent(scrollGo.transform, false);

            var vpRect = viewportGo.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.anchoredPosition = Vector2.zero;
            vpRect.sizeDelta = Vector2.zero;

            var vpImage = viewportGo.GetComponent<Image>();

            if (oldImage != null)
            {
                vpImage.sprite = oldImage.sprite;
                vpImage.color = oldImage.color;
                vpImage.type = oldImage.type;
            }
            else
            {
                vpImage.enabled = false; // desabilita imagem se não houver
            }

            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            scrollRect.viewport = vpRect;

            // USA O PRÓPRIO oldContainer COMO CONTENT ----------------------------

            // reparent do container original para dentro do viewport
            oldContainer.transform.SetParent(vpRect, false);

            // ajusta seu RectTransform para topo-stretch
            var contentRect = oldContainer.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            // adiciona ou obtém o layout para empilhar verticalmente
            var layout = oldContainer.GetComponent<VerticalLayoutGroup>()
                         ?? oldContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // adiciona ou obtém o auto-size fitter
            var fitter = oldContainer.GetComponent<ContentSizeFitter>()
                         ?? oldContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // finalmente aponta o ScrollRect para esse content
            scrollRect.content = contentRect;

            // força rebuild imediato (útil pra quando já houver itens)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            Debug.Log("[ExtendedExile] Scroll view configurada com sucesso!");
        }
    }
}