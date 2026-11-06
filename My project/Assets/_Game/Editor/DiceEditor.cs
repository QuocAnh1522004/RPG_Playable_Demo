using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable.EditorTools
{
    [CustomEditor(typeof(Dice))]
    public class DiceEditor : Editor
    {
        const string DoneKey = "RpgPlayable.DiceFacesAutoSetup.v2";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var dice = (Dice)target;
            if (dice.Visual == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Face setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Auto Setup Faces", GUILayout.Height(26)))
                AutoSetup(dice);

            serializedObject.Update();
            SerializedProperty faces = serializedObject.FindProperty("faceRotations");
            for (int i = 0; i < faces.arraySize; i++)
            {
                SerializedProperty face = faces.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(16));
                if (GUILayout.Button("Show"))
                {
                    Undo.RecordObject(dice.Visual, "Show Dice Face");
                    dice.Visual.localEulerAngles = face.vector3Value;
                }
                if (GUILayout.Button("Save"))
                    face.vector3Value = dice.Visual.localEulerAngles;
                EditorGUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }

        // runs once after this script compiles, so nobody has to press the button
        [InitializeOnLoadMethod]
        static void AutoSetupOnce()
        {
            if (EditorPrefs.GetBool(DoneKey, false)) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                Dice dice = Object.FindFirstObjectByType<Dice>();
                if (dice == null || dice.Visual == null) return; // scene not open yet, try again next reload

                if (AutoSetup(dice))
                {
                    EditorSceneManager.SaveScene(dice.gameObject.scene);
                    EditorPrefs.SetBool(DoneKey, true);
                    Debug.Log("Dice faces set up automatically and scene saved");
                }
            };
        }

        // scenes from before the tap button existed get one on the next recompile
        [InitializeOnLoadMethod]
        static void BuildTapAreaIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                Dice dice = Object.FindFirstObjectByType<Dice>();
                if (dice == null) return;
                var so = new SerializedObject(dice);
                if (so.FindProperty("tapButton").objectReferenceValue != null) return;

                if (BuildTapArea(dice))
                {
                    EditorSceneManager.SaveScene(dice.gameObject.scene);
                    Debug.Log("Built DiceTapArea and saved the scene");
                }
            };
        }

        [MenuItem("RPG Playable/Build Dice Tap Area")]
        static void BuildTapAreaMenu()
        {
            Dice dice = Object.FindFirstObjectByType<Dice>();
            if (dice != null && BuildTapArea(dice))
                EditorSceneManager.MarkSceneDirty(dice.gameObject.scene);
        }

        // Same recipe as the PLAY NOW button that works in the Luna build: a scene object with a sprite,
        // nearly (not fully) transparent so nothing can cull it out of the raycast.
        static bool BuildTapArea(Dice dice)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas in the scene");
                return false;
            }

            Transform old = canvas.transform.Find("DiceTapArea");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var go = new GameObject("DiceTapArea", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.sizeDelta = new Vector2(320f, 320f);

            // above the HUD, below the skill panel, hand and AFK overlay
            Transform topBar = canvas.transform.Find("TopBar");
            rt.SetSiblingIndex(topBar != null ? topBar.GetSiblingIndex() + 1 : 0);

            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PLA_TEST/Art/UI/Shared/UnitSquare.png");
            img.color = new Color(1f, 1f, 1f, 0.01f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;

            var so = new SerializedObject(dice);
            so.FindProperty("tapButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(go, "Build Dice Tap Area");
            return true;
        }

        [MenuItem("RPG Playable/Auto Setup Dice Faces")]
        static void AutoSetupMenu()
        {
            Dice dice = Object.FindFirstObjectByType<Dice>();
            if (dice == null || dice.Visual == null)
            {
                Debug.LogError("No Dice with a Visual in the scene");
                return;
            }
            if (AutoSetup(dice)) EditorSceneManager.MarkSceneDirty(dice.gameObject.scene);
        }

        // Finds which side of the mesh carries which number (from the UVs on dice_color.png),
        // then builds a rotation per number that puts that side facing world up with the cube axis-aligned.
        static bool AutoSetup(Dice dice)
        {
            MeshFilter mf = dice.Visual.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogError("Dice: no mesh found under Visual");
                return false;
            }

            Mesh mesh = mf.sharedMesh;
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] tris = mesh.triangles;

            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var uvSum = new Vector2[6];
            var areaSum = new float[6];

            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                float area = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]).magnitude;
                if (area < 1e-8f) continue;

                Vector3 n = (normals[a] + normals[b] + normals[c]).normalized;
                for (int k = 0; k < 6; k++)
                {
                    if (Vector3.Dot(n, axes[k]) > 0.95f)
                    {
                        uvSum[k] += (uvs[a] + uvs[b] + uvs[c]) / 3f * area;
                        areaSum[k] += area;
                        break;
                    }
                }
            }

            var axisOf = new Vector3[7];
            var found = new bool[7];
            for (int k = 0; k < 6; k++)
            {
                if (areaSum[k] <= 0f) continue;
                int number = NumberAt(uvSum[k] / areaSum[k]);
                if (number == 0) continue;
                axisOf[number] = axes[k];
                found[number] = true;
            }

            for (int n = 1; n <= 6; n++)
            {
                if (!found[n])
                {
                    Debug.LogError("Dice: couldn't find face " + n + ", set it by hand with Save");
                    return false;
                }
            }

            Transform visual = dice.Visual;
            Quaternion meshToVisual = Quaternion.Inverse(visual.rotation) * mf.transform.rotation;
            Quaternion anchorInv = Quaternion.Inverse(visual.parent != null ? visual.parent.rotation : Quaternion.identity);

            var so = new SerializedObject(dice);
            SerializedProperty faces = so.FindProperty("faceRotations");
            faces.arraySize = 6;
            for (int n = 1; n <= 6; n++)
            {
                Vector3 up = meshToVisual * axisOf[n];
                Vector3 forward = meshToVisual * axisOf[Perpendicular(axisOf, n)];
                Quaternion world = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                faces.GetArrayElementAtIndex(n - 1).vector3Value = (anchorInv * world).eulerAngles;
            }
            so.ApplyModifiedProperties();

            int resting = Mathf.Clamp(so.FindProperty("restingFace").intValue, 1, 6);
            Undo.RecordObject(visual, "Dice Resting Face");
            visual.localEulerAngles = faces.GetArrayElementAtIndex(resting - 1).vector3Value;
            EditorUtility.SetDirty(dice);
            return true;
        }

        static int Perpendicular(Vector3[] axisOf, int n)
        {
            for (int m = 1; m <= 6; m++)
                if (Mathf.Abs(Vector3.Dot(axisOf[m], axisOf[n])) < 0.1f) return m;
            return n;
        }

        // layout of dice_color.png in a 4x4 grid (row 0 = top of the image)
        static int NumberAt(Vector2 uv)
        {
            int col = Mathf.FloorToInt(Mathf.Repeat(uv.x, 1f) * 4f);
            int row = Mathf.FloorToInt((1f - Mathf.Repeat(uv.y, 1f)) * 4f);

            if (row == 0 && col == 1) return 3;
            if (row == 2 && col == 1) return 4;
            if (row == 1)
            {
                if (col == 0) return 2;
                if (col == 1) return 1;
                if (col == 2) return 5;
                if (col == 3) return 6;
            }
            return 0;
        }
    }
}
