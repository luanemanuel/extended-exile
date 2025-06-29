using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ExtendedExile.Patches
{
    public class PatchPlayerListScroll : MonoBehaviour
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

            Debug.Log($"[ExtendedExile] Configurando PlayerList ScrollView em cena: {SceneManager.GetActiveScene().name}");

            // encontra o container original
            var oldContainer = GameObject.Find("NewMenuInGame(Clone)")
                ?.transform
                .Find("Modal Windows/ListPlayer/ListPlayers")
                ?.gameObject;

            if (oldContainer == null)
            {
                Debug.LogWarning("[ExtendedExile] ListPlayers não encontrada.");
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
                Debug.LogWarning("[ExtendedExile] Cena mudou antes de povoar ListPlayers, abortando.");
                yield break;
            }

            // pega referências
            var parent   = oldContainer.transform.parent;
            var oldRect  = oldContainer.GetComponent<RectTransform>();
            var oldImage = oldContainer.GetComponent<Image>();

            // CRIA O SCROLLVIEW ---------------------------------------------------

            var scrollGo = new GameObject("ListPlayersScrollView",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(Image)
            );
            scrollGo.transform.SetParent(parent, false);

            // replica RectTransform e visual
            var newRect = scrollGo.GetComponent<RectTransform>();
            newRect.anchorMin        = oldRect.anchorMin;
            newRect.anchorMax        = oldRect.anchorMax;
            newRect.pivot            = oldRect.pivot;
            newRect.anchoredPosition = oldRect.anchoredPosition;
            newRect.sizeDelta        = oldRect.sizeDelta;

            // copia fundo (se existir) ou deixa transparente
            var bgImg = scrollGo.GetComponent<Image>();
            if (oldImage != null)
            {
                bgImg.sprite = oldImage.sprite;
                bgImg.color  = oldImage.color;
                bgImg.type   = oldImage.type;
            }
            else
            {
                bgImg.color = new Color(1, 1, 1, 0);
            }

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.vertical   = true;
            scrollRect.horizontal = false;

            // VIEWPORT ------------------------------------------------------------

            var viewportGo = new GameObject("Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            viewportGo.transform.SetParent(scrollGo.transform, false);

            var vpRect = viewportGo.GetComponent<RectTransform>();
            vpRect.anchorMin        = Vector2.zero;
            vpRect.anchorMax        = Vector2.one;
            vpRect.anchoredPosition = Vector2.zero;
            vpRect.sizeDelta        = Vector2.zero;

            var vpImg = viewportGo.GetComponent<Image>();
            // reutiliza mesmo visual do oldImage (opcional)
            if (oldImage != null)
            {
                vpImg.sprite = oldImage.sprite;
                vpImg.color  = oldImage.color;
                vpImg.type   = oldImage.type;
            }
            else
            {
                vpImg.enabled = false;
            }

            var vpMask = viewportGo.GetComponent<Mask>();
            vpMask.showMaskGraphic = false;

            scrollRect.viewport = vpRect;

            // USA O PRÓPRIO oldContainer COMO CONTENT ----------------------------

            // reparent do container original para dentro do viewport
            oldContainer.transform.SetParent(vpRect, false);

            // ajusta seu RectTransform para topo‐stretch
            var contentRect = oldContainer.GetComponent<RectTransform>();
            contentRect.anchorMin        = new Vector2(0, 1);
            contentRect.anchorMax        = new Vector2(1, 1);
            contentRect.pivot            = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta        = Vector2.zero;

            // adiciona ou obtém o layout para empilhar verticalmente
            var layout = oldContainer.GetComponent<VerticalLayoutGroup>()
                         ?? oldContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing               = 4;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // adiciona ou obtém o auto‐size fitter
            var fitter = oldContainer.GetComponent<ContentSizeFitter>()
                        ?? oldContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // finalmente aponta o ScrollRect para esse content
            scrollRect.content = contentRect;

            // força rebuild imediato (útil pra quando já houver itens)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            Debug.Log("[ExtendedExile] PlayerList ScrollView configurada com sucesso!");
        }
    }
}
