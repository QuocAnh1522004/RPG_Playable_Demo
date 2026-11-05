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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Face setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Rotate the dice model until face N is on top, then press Save on row N.", MessageType.None);

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
    }
}
