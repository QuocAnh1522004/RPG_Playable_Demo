using UnityEditor;
using UnityEngine;

namespace RpgPlayable.EditorTools
{
    [CustomEditor(typeof(Dice))]
    public class DiceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var dice = (Dice)target;
            if (dice.Visual == null) return;

            serializedObject.Update();
            SerializedProperty faces = serializedObject.FindProperty("faceRotations");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Face setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Auto Setup Faces", GUILayout.Height(26)))
                AutoSetup(dice, faces);

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

        // Finds which side of the mesh carries which number (from the UVs on dice_color.png),
        // then builds a rotation per number that puts that side facing world up with the cube axis-aligned.
        static void AutoSetup(Dice dice, SerializedProperty faces)
        {
            MeshFilter mf = dice.Visual.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogError("Dice: no mesh found under Visual");
                return;
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
                    return;
                }
            }

            Transform visual = dice.Visual;
            Quaternion meshToVisual = Quaternion.Inverse(visual.rotation) * mf.transform.rotation;
            Quaternion anchorInv = Quaternion.Inverse(visual.parent != null ? visual.parent.rotation : Quaternion.identity);

            for (int n = 1; n <= 6; n++)
            {
                Vector3 up = meshToVisual * axisOf[n];
                Vector3 forward = meshToVisual * axisOf[Perpendicular(axisOf, n)];
                Quaternion world = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                faces.GetArrayElementAtIndex(n - 1).vector3Value = (anchorInv * world).eulerAngles;
            }

            faces.serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(visual, "Show Dice Face");
            visual.localEulerAngles = faces.GetArrayElementAtIndex(0).vector3Value;
            Debug.Log("Dice faces set up, showing 1");
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
