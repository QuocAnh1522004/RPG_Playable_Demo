using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable.EditorTools
{
    // Builds the skill panel + tutorial hand under the scene Canvas and wires them to GameFlowController.
    // Running it again replaces the old ones, so tweak the numbers here rather than by hand if you plan to rebuild.
    public static class UiBuilder
    {
        const string Ui = "Assets/PLA_TEST/Art/UI/";
        const string FontPath = Ui + "Font/cambria SDF.asset";
        const string OutlineMat = Ui + "Font/outline black - cambria Atlas Material.mat";
        const string BrownMat = Ui + "Font/outline brown - cambria Atlas Material.mat";

        static readonly Color CardColor = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        static readonly Color DescColor = new Color(0.55f, 1f, 0.3f, 1f);
        static readonly Color TitleColor = new Color(1f, 0.93f, 0.82f, 1f);

        [MenuItem("RPG Playable/Build Skill Panel UI")]
        static void BuildSkillPanel()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas in the scene");
                return;
            }

            RemoveChild(canvas.transform, "SkillPanel");
            RemoveChild(canvas.transform, "TutorialHand");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMat);
            var brown = AssetDatabase.LoadAssetAtPath<Material>(BrownMat);

            RectTransform panel = NewRect("SkillPanel", canvas.transform);
            Stretch(panel);
            var skillPanel = panel.gameObject.AddComponent<SkillPanel>();

            RectTransform root = NewRect("Root", panel);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            Image dim = NewImage("Dim", root, null, new Color(0f, 0f, 0f, 0.5f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);

            RectTransform content = NewRect("Content", root);
            content.sizeDelta = new Vector2(1000f, 900f);
            content.anchoredPosition = new Vector2(0f, 60f);

            Image title = NewImage("Title", content, LoadSprite("Skill Select/title_level_up.png"), Color.white);
            title.rectTransform.sizeDelta = new Vector2(857f, 295f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 330f);

            TMP_Text titleText = NewText("Text", title.rectTransform, "New Skill", 76f, font, brown, TitleColor);
            titleText.fontStyle = FontStyles.Italic;
            titleText.rectTransform.sizeDelta = new Vector2(800f, 100f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -62f);

            TMP_Text subtitle = NewText("Subtitle", content, "Choose a Skill", 38f, font, outline, Color.white);
            subtitle.fontStyle = FontStyles.Italic;
            subtitle.rectTransform.sizeDelta = new Vector2(800f, 60f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, 160f);

            var cards = new SkillCard[3];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = NewCard(content, i, font, outline);

            RectTransform handRoot = NewRect("TutorialHand", canvas.transform);
            Stretch(handRoot);
            Image hand = NewImage("Hand", handRoot, LoadSprite("Shared/tutorial_hand.png"), Color.white);
            hand.rectTransform.sizeDelta = new Vector2(150f, 211f);
            hand.rectTransform.pivot = new Vector2(0.23f, 0.97f); // fingertip
            var tutorialHand = handRoot.gameObject.AddComponent<TutorialHand>();

            Set(skillPanel, "root", root.gameObject);
            Set(skillPanel, "group", group);
            Set(skillPanel, "content", content);
            Set(skillPanel, "hand", tutorialHand);
            SetArray(skillPanel, "cards", cards);
            Set(tutorialHand, "hand", hand.rectTransform);

            var flow = Object.FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                Set(flow, "skillPanel", skillPanel);
                Set(flow, "tutorialHand", tutorialHand);
            }
            else
            {
                Debug.LogWarning("No GameFlowController found, assign Skill Panel and Tutorial Hand by hand");
            }

            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Build Skill Panel");
            Undo.RegisterCreatedObjectUndo(handRoot.gameObject, "Build Skill Panel");
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("Skill panel built and wired");
        }

        [MenuItem("RPG Playable/Add Dice Stand")]
        static void AddDiceStand()
        {
            Dice dice = Object.FindFirstObjectByType<Dice>();
            if (dice == null)
            {
                Debug.LogError("No Dice in the scene");
                return;
            }

            RemoveChild(dice.transform, "Stand");

            var go = new GameObject("Stand");
            go.transform.SetParent(dice.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.55f, 0.8f); // a bit below and behind the dice
            go.transform.localScale = Vector3.one * 0.45f;
            go.AddComponent<SpriteRenderer>().sprite = LoadSprite("Campaign/button_main_release.png");

            Undo.RegisterCreatedObjectUndo(go, "Add Dice Stand");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
        }

        static SkillCard NewCard(RectTransform parent, int index, TMP_FontAsset font, Material outline)
        {
            Sprite board = LoadSprite("Skill Select/board_common.png");

            RectTransform card = NewRect("Card_" + index, parent);
            card.sizeDelta = new Vector2(980f, 140f);
            card.anchoredPosition = new Vector2(0f, 60f - index * 155f);

            Image body = NewImage("Body", card, board, CardColor);
            body.raycastTarget = true;
            Stretch(body.rectTransform);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            button.transition = Selectable.Transition.None;

            Image icon = NewImage("Icon", card, null, Color.white);
            SetAnchor(icon.rectTransform, new Vector2(0f, 0.5f));
            icon.rectTransform.sizeDelta = new Vector2(112f, 112f);
            icon.rectTransform.anchoredPosition = new Vector2(76f, -4f);
            icon.preserveAspect = true;

            Image tag = NewImage("Tag", card, LoadSprite("Skill Select/tag_common.png"), Color.white);
            SetAnchor(tag.rectTransform, new Vector2(0f, 1f));
            tag.rectTransform.sizeDelta = new Vector2(113f, 32f);
            tag.rectTransform.anchoredPosition = new Vector2(70f, 0f);

            TMP_Text tagText = NewText("Text", tag.rectTransform, "Common", 20f, font, null, Color.white);
            Stretch(tagText.rectTransform);

            TMP_Text nameText = NewText("Name", card, "Skill Name", 40f, font, outline, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            SetAnchor(nameText.rectTransform, new Vector2(0f, 1f));
            nameText.rectTransform.pivot = new Vector2(0f, 1f);
            nameText.rectTransform.sizeDelta = new Vector2(800f, 50f);
            nameText.rectTransform.anchoredPosition = new Vector2(150f, -12f);

            TMP_Text desc = NewText("Description", card, "Description", 30f, font, null, DescColor);
            desc.alignment = TextAlignmentOptions.TopLeft;
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 20f;
            desc.fontSizeMax = 30f;
            SetAnchor(desc.rectTransform, new Vector2(0f, 1f));
            desc.rectTransform.pivot = new Vector2(0f, 1f);
            desc.rectTransform.sizeDelta = new Vector2(810f, 74f);
            desc.rectTransform.anchoredPosition = new Vector2(150f, -60f);

            // glow frame goes on top of everything, sticks out a bit so the glow sits around the card
            Image highlight = NewImage("Highlight", card, LoadSprite("Shared/board_daily_claim.png"), Color.white);
            highlight.type = Image.Type.Sliced;
            Stretch(highlight.rectTransform);
            highlight.rectTransform.offsetMin = new Vector2(-22f, -22f);
            highlight.rectTransform.offsetMax = new Vector2(22f, 22f);

            var skillCard = card.gameObject.AddComponent<SkillCard>();
            Set(skillCard, "button", button);
            Set(skillCard, "icon", icon);
            Set(skillCard, "nameText", nameText);
            Set(skillCard, "descriptionText", desc);
            Set(skillCard, "tagText", tagText);
            Set(skillCard, "highlight", highlight.gameObject);
            return skillCard;
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

        static void RemoveChild(Transform parent, string name)
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

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
