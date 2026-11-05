using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable.EditorTools
{
    // Builds the battle arena (backdrop, camera, spots, battle hero), the battle UI, wires BattleController
    // and fills in the battle fields of the 3 stages. Re-running replaces the arena and the UI.
    public static class BattleBuilder
    {
        const string Ui = "Assets/PLA_TEST/Art/UI/";
        const string Fx = "Assets/PLA_TEST/Art/FX/Prefabs/";
        const string FontPath = Ui + "Font/cambria SDF.asset";

        static readonly Vector3 ArenaPosition = new Vector3(500f, 0f, 0f); // far from the board so neither camera sees the other
        const float CameraPitch = 20.66f; // matches CapturedCombatBackdrop

        [MenuItem("RPG Playable/Build Battle")]
        static void Build()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            Camera boardCamera = Camera.main;
            if (canvas == null || flow == null || boardCamera == null)
            {
                Debug.LogError("Need a Canvas, a GameFlowController and a Main Camera in the scene");
                return;
            }

            GameObject old = GameObject.Find("BattleArena");
            if (old != null) Undo.DestroyObjectImmediate(old);
            Transform oldUi = canvas.transform.Find("BattleUI");
            if (oldUi != null) Undo.DestroyObjectImmediate(oldUi.gameObject);

            var stats = flow.GetComponent<HeroStats>();
            if (stats == null) stats = Undo.AddComponent<HeroStats>(flow.gameObject);

            BattleController battle = BuildArena(boardCamera, out Camera battleCamera);
            GameObject ui = BuildUi(canvas, battle, battleCamera);

            Set(battle, "boardCamera", boardCamera);
            Set(battle, "battleCamera", battleCamera);
            Set(battle, "heroStats", stats);
            Set(battle, "skillLibrary", AssetDatabase.LoadAssetAtPath<SkillLibrary>("Assets/_Game/Data/SkillLibrary.asset"));
            Set(battle, "ui", ui);

            Set(flow, "battle", battle);
            Set(flow, "heroStats", stats);
            FillStages(flow);

            Undo.RegisterCreatedObjectUndo(battle.gameObject, "Build Battle");
            Undo.RegisterCreatedObjectUndo(ui, "Build Battle");
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = battle.gameObject;
            Debug.Log("Battle built. Select BattleCamera to frame the arena, move HeroSpot/MonsterSpot onto the floor.");
        }

        static BattleController BuildArena(Camera boardCamera, out Camera battleCamera)
        {
            var arena = new GameObject("BattleArena");
            Transform gameRoot = boardCamera.transform.parent;
            if (gameRoot != null) arena.transform.SetParent(gameRoot, false);
            arena.transform.position = ArenaPosition;

            var backdropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PLA_TEST/Prefabs/combat1_1_backdrop.prefab");
            var backdrop = (GameObject)PrefabUtility.InstantiatePrefab(backdropPrefab, arena.transform);
            backdrop.transform.localPosition = Vector3.zero;
            Transform picture = backdrop.transform.Find("CapturedCombatBackdrop");

            var camGo = new GameObject("BattleCamera");
            camGo.transform.SetParent(arena.transform, false);
            camGo.transform.rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
            Vector3 aim = picture != null ? picture.position : arena.transform.position;
            camGo.transform.position = aim - camGo.transform.forward * 30f - camGo.transform.up * 2f;

            battleCamera = camGo.AddComponent<Camera>();
            battleCamera.orthographic = true;
            battleCamera.orthographicSize = 5f;
            battleCamera.rect = new Rect(0f, 0.4f, 1f, 0.6f); // bottom 40% is the skills tray
            battleCamera.depth = 1f;
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = Color.black;
            battleCamera.nearClipPlane = 0.3f;
            battleCamera.farClipPlane = 100f;
            battleCamera.enabled = false;

            // spots sit on the camera's center line, ~15 units in, a bit below the middle
            Vector3 center = camGo.transform.position + camGo.transform.forward * 15f - Vector3.up * 1.5f;
            Transform heroSpot = NewChild("HeroSpot", arena.transform, center - Vector3.right * 2.5f, Quaternion.Euler(0f, 90f, 0f));
            Transform monsterSpot = NewChild("MonsterSpot", arena.transform, center + Vector3.right * 2.5f, Quaternion.Euler(0f, -90f, 0f));

            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/hero.prefab");
            var heroGo = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, heroSpot);
            heroGo.name = "BattleHero";
            heroGo.transform.localPosition = Vector3.zero;
            heroGo.transform.localRotation = Quaternion.identity;
            var heroAnim = heroGo.GetComponent<ActorAnimator>();
            if (heroAnim == null) heroAnim = heroGo.AddComponent<ActorAnimator>();

            var battle = arena.AddComponent<BattleController>();
            Set(battle, "heroSpot", heroSpot);
            Set(battle, "monsterSpot", monsterSpot);
            Set(battle, "hero", heroAnim);
            return battle;
        }

        static GameObject BuildUi(Canvas canvas, BattleController battle, Camera battleCamera)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var outline = AssetDatabase.LoadAssetAtPath<Material>(Ui + "Font/outline black - cambria Atlas Material.mat");
            var orange = AssetDatabase.LoadAssetAtPath<Material>(Ui + "Font/outline orange - cambria Atlas Material.mat");

            RectTransform root = NewRect("BattleUI", canvas.transform);
            Stretch(root);
            root.SetSiblingIndex(0); // under the skill panel, hand and HUD

            // bottom tray
            Image trayBg = NewImage("TrayBackground", root, null, new Color(0.1f, 0.09f, 0.09f, 1f));
            trayBg.raycastTarget = true;
            trayBg.rectTransform.anchorMin = Vector2.zero;
            trayBg.rectTransform.anchorMax = new Vector2(1f, 0.4f);
            trayBg.rectTransform.offsetMin = Vector2.zero;
            trayBg.rectTransform.offsetMax = Vector2.zero;

            Image tray = NewImage("Tray", trayBg.rectTransform, LoadSprite("Combat/skill_tray.png"), Color.white);
            tray.type = Image.Type.Sliced;
            Stretch(tray.rectTransform);
            tray.rectTransform.offsetMin = new Vector2(20f, 20f);
            tray.rectTransform.offsetMax = new Vector2(-20f, -20f);

            Image head = NewImage("Header", tray.rectTransform, LoadSprite("Combat/tray_head.png"), Color.white);
            SetAnchor(head.rectTransform, new Vector2(0.5f, 1f));
            head.rectTransform.sizeDelta = new Vector2(857f, 56f);
            head.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            TMP_Text headText = NewText("Text", head.rectTransform, "Skills", 32f, font, null, new Color(0.75f, 0.72f, 0.68f));
            headText.fontStyle = FontStyles.Italic;
            Stretch(headText.rectTransform);

            Image slot = NewImage("SlotTemplate", tray.rectTransform, LoadSprite("Combat/skill_bg.png"), Color.white);
            SetAnchor(slot.rectTransform, new Vector2(0f, 1f));
            slot.rectTransform.sizeDelta = new Vector2(114f, 116f);
            slot.rectTransform.anchoredPosition = new Vector2(90f, -140f);
            Image slotIcon = NewImage("Icon", slot.rectTransform, null, Color.white);
            slotIcon.rectTransform.sizeDelta = new Vector2(92f, 92f);
            slotIcon.preserveAspect = true;

            var skillTray = tray.gameObject.AddComponent<SkillTray>();
            Set(skillTray, "slotTemplate", slot.rectTransform);

            // hp bars follow the actors
            HpBar heroBar = NewHpBar("HeroBar", root, font, outline);
            HpBar monsterBar = NewHpBar("MonsterBar", root, font, outline);

            // damage numbers
            RectTransform popupLayer = NewRect("Popups", root);
            Stretch(popupLayer);
            TMP_Text popupTemplate = NewText("PopupTemplate", popupLayer, "000", 64f, font, null, Color.white);
            popupTemplate.rectTransform.sizeDelta = new Vector2(300f, 90f);
            var popups = popupLayer.gameObject.AddComponent<DamagePopups>();
            Set(popups, "template", popupTemplate);
            Set(popups, "worldCamera", battleCamera);
            SetArray(popups, "styleMaterials", new Object[]
            {
                LoadMat("normal dmg"), LoadMat("crit dmg"), LoadMat("burn dmg"), LoadMat("poison dmg"), LoadMat("lightning dmg")
            });

            // monster name banner across the top of the arena
            RectTransform bannerLayer = NewRect("Banner", root);
            Stretch(bannerLayer);
            Image bannerImg = NewImage("Strip", bannerLayer, LoadSprite("Combat/board_begin_battle.png"), Color.white);
            SetAnchor(bannerImg.rectTransform, new Vector2(0.5f, 1f));
            bannerImg.rectTransform.sizeDelta = new Vector2(1080f, 239f);
            bannerImg.rectTransform.anchoredPosition = new Vector2(0f, -330f);
            TMP_Text bannerText = NewText("Text", bannerImg.rectTransform, "SLIME", 80f, font, orange, Color.white);
            bannerText.fontStyle = FontStyles.Italic | FontStyles.Bold;
            Stretch(bannerText.rectTransform);
            var banner = bannerLayer.gameObject.AddComponent<BattleBanner>();
            Set(banner, "banner", bannerImg.rectTransform);
            Set(banner, "label", bannerText);

            Set(battle, "heroBar", heroBar);
            Set(battle, "monsterBar", monsterBar);
            Set(battle, "popups", popups);
            Set(battle, "banner", banner);
            Set(battle, "skillTray", skillTray);
            return root.gameObject;
        }

        static HpBar NewHpBar(string name, Transform parent, TMP_FontAsset font, Material outline)
        {
            RectTransform bar = NewRect(name, parent);
            bar.sizeDelta = new Vector2(252f, 33f);

            Image bg = NewImage("Background", bar, LoadSprite("Combat/bar_HP_empty.png"), Color.white);
            bg.type = Image.Type.Sliced;
            Stretch(bg.rectTransform);

            Image trail = NewFilled("Trail", bar, LoadSprite("Combat/bar_HP_decrease.png"));
            Image fill = NewFilled("Fill", bar, LoadSprite("Combat/bar_HP_full.png"));

            TMP_Text label = NewText("Label", bar, "100 / 100", 26f, font, outline, Color.white);
            label.rectTransform.sizeDelta = new Vector2(300f, 36f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 30f);

            var hp = bar.gameObject.AddComponent<HpBar>();
            Set(hp, "fill", fill);
            Set(hp, "trail", trail);
            Set(hp, "label", label);
            return hp;
        }

        static Image NewFilled(string name, Transform parent, Sprite sprite)
        {
            Image img = NewImage(name, parent, sprite, Color.white);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.rectTransform.sizeDelta = new Vector2(244f, 25f);
            return img;
        }

        static void FillStages(GameFlowController flow)
        {
            string[] names = { "SLIME", "TROLL", "TREANT" };
            string[] prefabs = { "Slime", "Troll", "Treant" };
            string[] hitFx = { "slime_fx_atk_hit", "troll_fx_atk_hit", "goblin_fx_ulti_hit" };
            int[] attack = { 40, 40, 40 };

            var so = new SerializedObject(flow);
            SerializedProperty stages = so.FindProperty("stages");
            for (int i = 0; i < stages.arraySize && i < 3; i++)
            {
                SerializedProperty s = stages.GetArrayElementAtIndex(i);
                s.FindPropertyRelative("monsterName").stringValue = names[i];
                s.FindPropertyRelative("battlePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/" + prefabs[i] + ".prefab");
                s.FindPropertyRelative("monsterHitFx").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(Fx + hitFx[i] + ".prefab");
                s.FindPropertyRelative("monsterAttack").intValue = attack[i];
                s.FindPropertyRelative("ranged").boolValue = i == 2;
                s.FindPropertyRelative("battleProjectile").objectReferenceValue = i == 2
                    ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PLA_TEST/Art/Actors/Treant/treant dice.fbx")
                    : null;
                if (s.FindPropertyRelative("battleScale").floatValue <= 0f)
                    s.FindPropertyRelative("battleScale").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform NewChild(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            return go.transform;
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

        static Material LoadMat(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(Ui + "Materials/" + name + " - cambria SDF Atlas Material.mat");
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
