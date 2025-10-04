// Assets/Editor/TurnSummaryUIBuilder.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public static class TurnSummaryUIBuilder
{
    [MenuItem("GameObject/Pokiwar UI/Create Turn Summary UI", false, 10)]
    public static void CreateTurnSummaryUI()
    {
        // ===== Ensure Canvas + EventSystem =====
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var goCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = goCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }
            Undo.RegisterCreatedObjectUndo(goCanvas, "Create Canvas");
        }

        // ===== Overlay Panel =====
        var panelGO = new GameObject("TurnSummaryPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGO.transform.SetParent(canvas.transform, false);
        var panelRT = (RectTransform)panelGO.transform;
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero; panelRT.offsetMax = Vector2.zero;

        var panelImg = panelGO.GetComponent<Image>(); panelImg.color = new Color(0, 0, 0, 0.6f);
        var cg = panelGO.GetComponent<CanvasGroup>();
        cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;

        // ===== Card =====
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panelGO.transform, false);
        var cardRT = (RectTransform)card.transform;
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(700, 520);
        var cardImg = card.GetComponent<Image>(); cardImg.color = new Color(1, 1, 1, 0.1f);

        var v = card.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperCenter; v.padding = new RectOffset(24, 24, 24, 24); v.spacing = 12;
        card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        TMP_Text MakeTitle(string text)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(card.transform, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 36; tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
        MakeTitle("TỔNG KẾT LƯỢT");

        // Rows
        var rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(card.transform, false);
        var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowsLayout.childAlignment = TextAnchor.UpperCenter; rowsLayout.spacing = 8; rowsLayout.padding = new RectOffset(8, 8, 4, 8);
        rows.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        (Image icon, TextMeshProUGUI count, Slider bar) MakeRow(string name, Color iconColor)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(rows.transform, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft; h.spacing = 10; h.padding = new RectOffset(4, 4, 4, 4);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(row.transform, false);
            ((RectTransform)iconGO.transform).sizeDelta = new Vector2(28, 28);
            var iconImg = iconGO.GetComponent<Image>(); iconImg.color = iconColor;

            var countGO = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGO.transform.SetParent(row.transform, false);
            var countTMP = countGO.GetComponent<TextMeshProUGUI>();
            countTMP.text = "0"; countTMP.fontSize = 26; countTMP.alignment = TextAlignmentOptions.MidlineLeft;
            ((RectTransform)countGO.transform).sizeDelta = new Vector2(60, 32);

            var barGO = new GameObject("Bar", typeof(RectTransform), typeof(Slider));
            barGO.transform.SetParent(row.transform, false);
            ((RectTransform)barGO.transform).sizeDelta = new Vector2(0, 22);

            var slider = barGO.GetComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0; slider.interactable = false;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(barGO.transform, false);
            var bgRT = (RectTransform)bg.transform; bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGO.transform, false);
            var faRT = (RectTransform)fillArea.transform; faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.offsetMin = new Vector2(4, 4); faRT.offsetMax = new Vector2(-4, -4);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = (RectTransform)fill.transform; fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>(); fillImg.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0.8f);

            slider.fillRect = fillRT; slider.targetGraphic = fillImg;

            iconGO.AddComponent<LayoutElement>().preferredWidth = 28;
            countGO.AddComponent<LayoutElement>().preferredWidth = 60;
            barGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            return (iconImg, countTMP, slider);
        }

        var red = MakeRow("Red", new Color(0.95f, 0.2f, 0.2f));
        var blue = MakeRow("Blue", new Color(0.2f, 0.45f, 0.95f));
        var green = MakeRow("Green", new Color(0.2f, 0.85f, 0.35f));
        var yellow = MakeRow("Yellow", new Color(0.98f, 0.85f, 0.2f));
        var grey = MakeRow("Grey", new Color(0.7f, 0.7f, 0.7f));
        var purple = MakeRow("Purple", new Color(0.7f, 0.3f, 0.85f));

        // Footer OK
        var footer = new GameObject("Footer", typeof(RectTransform));
        footer.transform.SetParent(card.transform, false);
        var fl = footer.AddComponent<HorizontalLayoutGroup>();
        fl.childAlignment = TextAnchor.MiddleCenter; fl.padding = new RectOffset(8, 8, 8, 8);

        var btnGO = new GameObject("OK", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(footer.transform, false);
        ((RectTransform)btnGO.transform).sizeDelta = new Vector2(160, 48);
        btnGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);

        var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>(); btnTMP.text = "OK"; btnTMP.fontSize = 28; btnTMP.alignment = TextAlignmentOptions.Center;
        var btnTextRT = (RectTransform)btnTextGO.transform; btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one; btnTextRT.offsetMin = Vector2.zero; btnTextRT.offsetMax = Vector2.zero;

        // Timer text (top-center)
        var timerGO = new GameObject("TurnTimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerGO.transform.SetParent(canvas.transform, false);
        var timerRT = (RectTransform)timerGO.transform; timerRT.anchorMin = timerRT.anchorMax = new Vector2(0.5f, 1f);
        timerRT.anchoredPosition = new Vector2(0, -40); timerRT.sizeDelta = new Vector2(200, 60);
        var timerTMP = timerGO.GetComponent<TextMeshProUGUI>(); timerTMP.text = "15"; timerTMP.fontSize = 40; timerTMP.alignment = TextAlignmentOptions.Center;

        // ===== Attach components by REFLECTION (no compile-time dependency) =====
        Type turnSummaryPanelType = Type.GetType("TurnSummaryPanel");
        if (turnSummaryPanelType == null)
        {
            Debug.LogWarning("Không tìm thấy class TurnSummaryPanel. Hãy tạo script TurnSummaryPanel.cs (không đặt trong Editor). Panel vẫn được tạo, bạn có thể gắn sau.");
        }
        else
        {
            var tsp = panelGO.AddComponent(turnSummaryPanelType); // GameObject.AddComponent(Type)
            // set public fields if exist
            SetIfFieldExists(tsp, "panel", cg);

            SetIfFieldExists(tsp, "redIcon", red.icon);
            SetIfFieldExists(tsp, "redCount", red.count);
            SetIfFieldExists(tsp, "redBar", red.bar);
            SetIfFieldExists(tsp, "blueIcon", blue.icon);
            SetIfFieldExists(tsp, "blueCount", blue.count);
            SetIfFieldExists(tsp, "blueBar", blue.bar);
            SetIfFieldExists(tsp, "greenIcon", green.icon);
            SetIfFieldExists(tsp, "greenCount", green.count);
            SetIfFieldExists(tsp, "greenBar", green.bar);
            SetIfFieldExists(tsp, "yellowIcon", yellow.icon);
            SetIfFieldExists(tsp, "yellowCount", yellow.count);
            SetIfFieldExists(tsp, "yellowBar", yellow.bar);
            SetIfFieldExists(tsp, "greyIcon", grey.icon);
            SetIfFieldExists(tsp, "greyCount", grey.count);
            SetIfFieldExists(tsp, "greyBar", grey.bar);
            SetIfFieldExists(tsp, "purpleIcon", purple.icon);
            SetIfFieldExists(tsp, "purpleCount", purple.count);
            SetIfFieldExists(tsp, "purpleBar", purple.bar);

            // Wire button: call Confirm() if exists
            var btn = btnGO.GetComponent<Button>();
            var mConfirm = turnSummaryPanelType.GetMethod("Confirm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (mConfirm != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (tsp != null) mConfirm.Invoke(tsp, null);
                });
            }
        }

        // Optional HUD for Timer via reflection
        Type hudType = Type.GetType("BattleHUD");
        if (hudType == null)
        {
            Debug.LogWarning("Không tìm thấy class BattleHUD. Hãy tạo script BattleHUD.cs (không đặt trong Editor). Timer text vẫn tạo sẵn, bạn có thể tự gắn sau.");
        }
        else
        {
            var hud = timerGO.AddComponent(hudType);
            SetIfFieldExists(hud, "timerText", timerTMP);
        }

        // Try auto-wire BattleManager
        var battle = FindFirstObjectByTypeSafe(typeof(BattleManager)) as MonoBehaviour;
        if (battle == null)
        {
            Debug.LogWarning("Không tìm thấy BattleManager trong scene. Sau khi thêm BattleManager, bạn nhớ kéo vào các field 'battle' trên TurnSummaryPanel/BattleHUD.");
        }
        else
        {
            // Assign battle to tsp/hud if fields exist
            var tspComp = panelGO.GetComponent(turnSummaryPanelType ?? typeof(Transform)); // may be null type
            if (tspComp && turnSummaryPanelType != null) SetIfFieldExists(tspComp, "battle", battle);

            var hudComp = timerGO.GetComponent(hudType ?? typeof(Transform));
            if (hudComp && hudType != null) SetIfFieldExists(hudComp, "battle", battle);
        }

        Selection.activeGameObject = panelGO;
        Debug.Log("Turn Summary UI created.");
    }

    static void SetIfFieldExists(Component comp, string fieldName, object value)
    {
        if (comp == null) return;
        var f = comp.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null) f.SetValue(comp, value);
    }

    static UnityEngine.Object FindFirstObjectByTypeSafe(Type t)
    {
#if UNITY_2023_1_OR_NEWER
        var method = typeof(UnityEngine.Object).GetMethods().FirstOrDefault(m => m.Name == "FindFirstObjectByType" && m.IsGenericMethodDefinition);
        if (method != null)
        {
            var g = method.MakeGenericMethod(t);
            return g.Invoke(null, null) as UnityEngine.Object;
        }
        return null;
#else
        return UnityEngine.Object.FindObjectOfType(t);
#endif
    }
}
#endif
