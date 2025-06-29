using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ExtendedExile.Patches
{
    [HarmonyPatch(typeof(VGUI_MenuInGame_PlayerList), "Awake")]
    static class PatchPlayerListAddScroll
    {
        static void Postfix(VGUI_MenuInGame_PlayerList __instance)
        {
            // 1) pega o container original (ListPlayer)
            //    você já tem um campo containerPlayers, mas se for privado:
            var fld = AccessTools.Field(typeof(VGUI_MenuInGame_PlayerList), "containerPlayers");
            var original = (Transform)fld.GetValue(__instance);
            if (original == null)
            {
                Debug.LogError("[ExtendedExile] containerPlayers não encontrado!");
                return;
            }

            var originalRT = original.GetComponent<RectTransform>();

            // 2) cria o GameObject ScrollView
            var scrollGo = new GameObject("ExtendedPlayerScroll",
                typeof(RectTransform),
                typeof(ScrollRect));
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(1, 1, 1, 0);
            scrollGo.transform.SetParent(originalRT.parent, false);

            var scrollRT = scrollGo.GetComponent<RectTransform>();
            var scrollRect = scrollGo.GetComponent<ScrollRect>();

            // copia âncoras/posição/tamanho
            scrollRT.anchorMin = originalRT.anchorMin;
            scrollRT.anchorMax = originalRT.anchorMax;
            scrollRT.anchoredPosition = originalRT.anchoredPosition;
            scrollRT.sizeDelta = originalRT.sizeDelta;

            // habilita só rolagem vertical
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.inertia = false;
            scrollRect.elasticity = 0f;

            // 3) cria Viewport (com máscara)
            var vpGo = new GameObject("Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D));
            vpGo.transform.SetParent(scrollGo.transform, false);
            
            var vpRT = vpGo.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            
            var vpImg = vpGo.GetComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0); // máscara invisível
            
            scrollRect.viewport = vpRT;

            // 4) cria Content (VerticalLayout + ContentSizeFitter)
            var contentGo = new GameObject("Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(vpGo.transform, false);

            var contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRT;

            // 5) cria a Scrollbar vertical
            var sbGo = new GameObject("ScrollbarVertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(scrollGo.transform, false);
            var sbRT = sbGo.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(1, 0);
            sbRT.anchorMax = new Vector2(1, 1);
            sbRT.pivot = new Vector2(1, 1);
            sbRT.sizeDelta = new Vector2(20, 0);

            var sbImage = sbGo.GetComponent<Image>();
            sbImage.color = new Color(1, 1, 1, 0.3f);

            var scrollbar = sbGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = sbImage;

            // handle
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(sbGo.transform, false);
            var hRT = handleGo.GetComponent<RectTransform>();
            hRT.anchorMin = Vector2.zero;
            hRT.anchorMax = Vector2.one;
            hRT.offsetMin = Vector2.zero;
            hRT.offsetMax = Vector2.zero;
            var hImage = handleGo.GetComponent<Image>();
            hImage.color = new Color(1, 1, 1, 0.8f);
            scrollbar.handleRect = hRT;

            // 6) configura ScrollRect para usar a Scrollbar
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = -5;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 7) redireciona container (passo 5 do patch original)
            original.gameObject.SetActive(false);
            fld.SetValue(__instance, contentRT);
        }
    }
}