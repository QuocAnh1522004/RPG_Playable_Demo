using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable.EditorTools
{
    // Hero HUD (top left), logo + PLAY NOW (top right), and the AFK overlay. Re-running replaces them.
    public static class HudBuilder
    {
        const string Ui = "Assets/PLA_TEST/Art/UI/";
        static readonly Color Green = new Color(0.45f, 1f, 0.3f, 1f); // tint on btn_yellow, the pack has no green button
        static readonly Color Gold = new Color(0.85f, 0.7f, 0.45f, 1f);

        // HUDs built before PulseScale existed don't have it, add it in place so nobody has to rebuild
        [InitializeOnLoadMethod]
        static void AddMissingPulse()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null) return;
                Transform play = canvas.transform.Find("TopBar/TopRight/PlayNow");
                if (play == null) return;

                bool changed = false;
                if (play.GetComponent<PulseScale>() == null)
                {
                    play.gameObject.AddComponent<PulseScale>();
                    changed = true;
                }

                // btn_yellow's slice borders (318px) are wider than this button, Luna then draws nothing
                var img = play.GetComponent<Image>();
                if (img != null && img.type == Image.Type.Sliced)
                {
                    img.type = Image.Type.Simple;
                    ((RectTransform)play).sizeDelta = new Vector2(290f, 99f); // sprite is 413x141
                    changed = true;
                }

                if (!changed) return;
                EditorSceneManager.SaveScene(play.gameObject.scene);
                Debug.Log("Fixed the top-right PLAY NOW (pulse / simple image) and saved the scene");
            };
        }

        [MenuItem("RPG Playable/Build HUD And AFK")]
        static void Build()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            if (canvas == null || flow == null)
            {
                Debug.LogError("Need a Canvas and a GameFlowController in the scene");
                return;
            }

            var store = flow.GetComponent<StoreRedirect>();
            var stats = flow.GetComponent<HeroStats>();
            if (store == null || stats == null)
            {
                Debug.LogError("GameFlow needs StoreRedirect and HeroStats (run Build Battle first)");
                return;
            }

            Remove(canvas.transform, "TopBar");
            Remove(canvas.transform, "AfkOverlay");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Ui + "Font/cambria SDF.asset");
            var outline = AssetDatabase.LoadAssetAtPath<Material>(Ui + "Font/outline black - cambria Atlas Material.mat");
            var thick = AssetDatabase.LoadAssetAtPath<Material>(Ui + "Font/outline black thicker bolder - cambria SDF Atlas Material.mat");

            RectTransform top = NewRect("TopBar", canvas.transform);
            Stretch(top);
            Transform battleUi = canvas.transform.Find("BattleUI");
            top.SetSiblingIndex(battleUi != null ? battleUi.GetSiblingIndex() + 1 : 0); // above battle, under the skill panel

            HeroHud hud = BuildHud(top, stats, font, outline);
            Button topPlay = BuildTopRight(top, font, thick);
            UnityEventTools.AddPersistentListener(topPlay.onClick, store.OpenStore);

            AfkOverlay afk = BuildAfk(canvas.transform, store, font, thick);

            var idle = flow.GetComponent<IdleWatcher>();
            if (idle == null) idle = Undo.AddComponent<IdleWatcher>(flow.gameObject);
            Set(idle, "flow", flow);
            Set(idle, "overlay", afk);

            Undo.RegisterCreatedObjectUndo(top.gameObject, "Build HUD");
            Undo.RegisterCreatedObjectUndo(afk.gameObject, "Build HUD");
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = hud.gameObject;
            Debug.Log("HUD, PLAY NOW and AFK overlay built");
        }

        static HeroHud BuildHud(RectTransform parent, HeroStats stats, TMP_FontAsset font, Material outline)
        {
            RectTransform hud = NewRect("HeroHud", parent);
            TopLeft(hud, new Vector2(0f, 0f), new Vector2(700f, 220f));

            Image panel = NewImage("Panel", hud, LoadSprite("In Game/board_char.png"), new Color(0.45f, 0.5f, 0.55f, 0.65f));
            TopLeft(panel.rectTransform, new Vector2(95f, -58f), new Vector2(579f, 150f));

            Image avatar = NewImage("Avatar", hud, LoadSprite("In Game/avatar_character.png"), Color.white);
            TopLeft(avatar.rectTransform, new Vector2(18f, -55f), new Vector2(154f, 154f));

            TMP_Text healthLabel = NewText("HealthLabel", hud, "HEALTH", 28f, font, outline, Gold);
            healthLabel.alignment = TextAlignmentOptions.Left;
            TopLeft(healthLabel.rectTransform, new Vector2(190f, -62f), new Vector2(200f, 34f));

            Image barBg = NewImage("HpBarBg", hud, LoadSprite("In Game/bar_HP empty.png"), Color.white);
            TopLeft(barBg.rectTransform, new Vector2(178f, -92f), new Vector2(444f, 36f));

            Image fill = NewImage("HpFill", hud, LoadSprite("In Game/bar_HP full.png"), Color.white);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            TopLeft(fill.rectTransform, new Vector2(182f, -96f), new Vector2(436f, 28f));

            TMP_Text hpText = NewText("HpText", hud, "1000 / 1000", 30f, font, outline, Color.white);
            hpText.fontStyle = FontStyles.Italic;
            TopLeft(hpText.rectTransform, new Vector2(178f, -90f), new Vector2(444f, 40f));

            TMP_Text atk = NewStat(hud, "Attack", "In Game/icon_attack.png", new Vector2(175f, -142f), font, outline);
            TMP_Text def = NewStat(hud, "Defense", "In Game/icon_defence.png", new Vector2(405f, -142f), font, outline);

            var comp = hud.gameObject.AddComponent<HeroHud>();
            Set(comp, "stats", stats);
            Set(comp, "hpFill", fill);
            Set(comp, "hpText", hpText);
            Set(comp, "atkText", atk);
            Set(comp, "defText", def);
            return comp;
        }

        static TMP_Text NewStat(RectTransform parent, string label, string icon, Vector2 pos, TMP_FontAsset font, Material outline)
        {
            Image pill = NewImage(label + "Stat", parent, LoadSprite("In Game/board_stat.png"), Color.white);
            TopLeft(pill.rectTransform, pos, new Vector2(220f, 46f));

            Image ic = NewImage("Icon", pill.rectTransform, LoadSprite(icon), Color.white);
            ic.preserveAspect = true;
            SetAnchor(ic.rectTransform, new Vector2(0f, 0.5f));
            ic.rectTransform.sizeDelta = new Vector2(34f, 34f);
            ic.rectTransform.anchoredPosition = new Vector2(28f, 0f);

            TMP_Text name = NewText("Label", pill.rectTransform, label, 24f, font, outline, Gold);
            name.fontStyle = FontStyles.Italic;
            name.alignment = TextAlignmentOptions.Left;
            SetAnchor(name.rectTransform, new Vector2(0f, 0.5f));
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.sizeDelta = new Vector2(110f, 40f);
            name.rectTransform.anchoredPosition = new Vector2(50f, 0f);

            TMP_Text value = NewText("Value", pill.rectTransform, "0", 28f, font, outline, Color.white);
            value.fontStyle = FontStyles.Italic;
            value.alignment = TextAlignmentOptions.Right;
            SetAnchor(value.rectTransform, new Vector2(1f, 0.5f));
            value.rectTransform.pivot = new Vector2(1f, 0.5f);
            value.rectTransform.sizeDelta = new Vector2(80f, 40f);
            value.rectTransform.anchoredPosition = new Vector2(-16f, 0f);
            return value;
        }

        static Button BuildTopRight(RectTransform parent, TMP_FontAsset font, Material thick)
        {
            RectTransform corner = NewRect("TopRight", parent);
            SetAnchor(corner, Vector2.one);
            corner.pivot = Vector2.one;
            corner.sizeDelta = new Vector2(360f, 230f);
            corner.anchoredPosition = Vector2.zero;

            Image logo = NewImage("Logo", corner, LoadSprite("logo.png"), Color.white);
            logo.preserveAspect = true;
            logo.rectTransform.sizeDelta = new Vector2(300f, 140f);
            logo.rectTransform.anchoredPosition = new Vector2(-10f, 40f);

            Button play = NewPlayButton("PlayNow", corner, new Vector2(290f, 99f), new Vector2(-10f, -55f), 42f, font, thick);
            play.GetComponent<Image>().type = Image.Type.Simple; // sliced borders are wider than the button, Luna draws nothing
            play.gameObject.AddComponent<PulseScale>();
            return play;
        }

        static AfkOverlay BuildAfk(Transform canvas, StoreRedirect store, TMP_FontAsset font, Material thick)
        {
            RectTransform overlay = NewRect("AfkOverlay", canvas);
            Stretch(overlay);
            overlay.SetAsLastSibling();

            RectTransform root = NewRect("Root", overlay);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            Image dim = NewImage("Dim", root, null, new Color(0f, 0f, 0f, 0.6f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            var dismiss = dim.gameObject.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;

            Image logo = NewImage("Logo", root, LoadSprite("logo.png"), Color.white);
            logo.preserveAspect = true;
            logo.rectTransform.sizeDelta = new Vector2(700f, 330f);
            logo.rectTransform.anchoredPosition = new Vector2(0f, 180f);

            Button play = NewPlayButton("PlayNow", root, new Vector2(620f, 170f), new Vector2(0f, -110f), 90f, font, thick);

            var afk = overlay.gameObject.AddComponent<AfkOverlay>();
            Set(afk, "root", root.gameObject);
            Set(afk, "group", group);
            Set(afk, "logo", logo.rectTransform);
            Set(afk, "playButton", (RectTransform)play.transform);
            Set(afk, "dismissArea", dismiss);
            Set(afk, "playNow", play);
            Set(afk, "store", store);
            return afk;
        }

        static Button NewPlayButton(string name, Transform parent, Vector2 size, Vector2 pos, float fontSize, TMP_FontAsset font, Material thick)
        {
            Image img = NewImage(name, parent, LoadSprite("Shared/btn_yellow.png"), Green);
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            TMP_Text text = NewText("Text", img.rectTransform, "PLAY NOW", fontSize, font, thick, Color.white);
            text.fontStyle = FontStyles.Bold;
            Stretch(text.rectTransform);
            return button;
        }

        static void TopLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TMP_Text NewText(string name, Transform parent, string text, float size, TMP_FontAsset font, Material mat, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            if (mat != null) t.fontSharedMaterial = mat;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetAnchor(RectTransform rt, Vector2 anchor)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
        }

        static Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Ui + path);
            if (s == null) Debug.LogWarning("Sprite not found: " + path);
            return s;
        }

        static void Remove(Transform parent, string name)
        {
            Transform old = parent.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
