using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ExtendedExile.Patches
{
    public class PatchPlayerListScroll : MonoBehaviour
    {
        private string currentScene;

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
            currentScene = scene.name;

            // Passa o nome da cena para a coroutine
            StartCoroutine(MonitorAndPatchList(scene.name));
        }

        private IEnumerator MonitorAndPatchList(string targetScene)
        {
            // Pequeno delay inicial para a UI começar a aparecer
            yield return new WaitForEndOfFrame();

            while (SceneManager.GetActiveScene().name == targetScene)
            {
                // 1) Tenta achar o container
                var oldContainer = GameObject.Find(
                    "NewMenuInGame(Clone)/Modal Windows/ListPlayer/ListPlayers"
                );

                if (oldContainer != null)
                {
                    // 2) Só aplica se ainda não houver o ScrollView
                    bool alreadyPatched = oldContainer.transform
                        .parent
                        .Find("ListPlayersScrollView") != null;
                    if (!alreadyPatched && oldContainer.transform.childCount > 0)
                    {
                        // dispara somente uma vez por reabertura
                        SetupScrollFor(oldContainer);
                        Debug.Log("[ExtendedExile] PlayerList ScrollView aplicada!");
                    }
                }

                yield return null; // espera o próximo frame
            }
        }

        private void SetupScrollFor(GameObject oldContainer)
        {
            // pega referências
            var parent = oldContainer.transform.parent;
            var oldRect = oldContainer.GetComponent<RectTransform>();
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
            newRect.anchorMin = oldRect.anchorMin;
            newRect.anchorMax = oldRect.anchorMax;
            newRect.pivot = oldRect.pivot;
            newRect.anchoredPosition = oldRect.anchoredPosition;
            newRect.sizeDelta = oldRect.sizeDelta;

            var bgImg = scrollGo.GetComponent<Image>();
            if (oldImage != null)
            {
                bgImg.sprite = oldImage.sprite;
                bgImg.color = oldImage.color;
                bgImg.type = oldImage.type;
            }
            else
            {
                bgImg.color = new Color(1, 1, 1, 0);
            }

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.elasticity = 0.1f;
            scrollRect.viewport = CreateViewport(scrollGo.transform, oldImage);

            // reparenta o oldContainer dentro do viewport
            oldContainer.transform.SetParent(scrollRect.viewport, false);

            // ajusta RectTransform do conteúdo
            var contentRect = oldContainer.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            // layout & fitter
            var layout = oldContainer.GetComponent<VerticalLayoutGroup>()
                         ?? oldContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = oldContainer.GetComponent<ContentSizeFitter>()
                         ?? oldContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private RectTransform CreateViewport(Transform parent, Image oldImage)
        {
            var go = new GameObject("Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var img = go.GetComponent<Image>();
            if (oldImage != null)
            {
                img.sprite = oldImage.sprite;
                img.color = oldImage.color;
                img.type = oldImage.type;
            }
            else
            {
                img.enabled = false;
            }

            var mask = go.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            return rt;
        }
    }
}