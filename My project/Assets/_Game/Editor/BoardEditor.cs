using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RpgPlayable.EditorTools
{
    [CustomEditor(typeof(Board))]
    public class BoardEditor : Editor
    {
        bool placing;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var board = (Board)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Align Scene View To Main Camera"))
                AlignSceneView();

            GUI.color = placing ? Color.green : Color.white;
            if (GUILayout.Button(placing ? "Stop Placing (Esc)" : "Place Tiles", GUILayout.Height(28)))
            {
                placing = !placing;
                SceneView.RepaintAll();
            }
            GUI.color = Color.white;

            if (GUILayout.Button("Remove Last Tile"))
                RemoveLast(board);

            if (placing)
                EditorGUILayout.HelpBox("Click the tiles on the map in walking order. Esc to stop.", MessageType.Info);
        }

        void OnDisable()
        {
            placing = false;
        }

        void OnSceneGUI()
        {
            if (!placing) return;

            var board = (Board)target;
            Event e = Event.current;

            // stop clicks from selecting other objects while placing
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                placing = false;
                e.Use();
                Repaint();
                return;
            }

            Vector3 hit;
            bool onGround = RaycastGround(board, e.mousePosition, out hit);

            if (e.type == EventType.Repaint && onGround)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit, Vector3.up, 0.3f);
                Handles.Label(hit + Vector3.up * 0.5f, board.TileCount.ToString());
            }
            else if (e.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && onGround)
            {
                AddTile(board, hit);
                e.Use();
            }
        }

        static bool RaycastGround(Board board, Vector2 mousePos, out Vector3 hit)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            var plane = new Plane(Vector3.up, new Vector3(0f, board.GroundY, 0f));
            float dist;
            if (plane.Raycast(ray, out dist))
            {
                hit = ray.GetPoint(dist);
                return true;
            }
            hit = Vector3.zero;
            return false;
        }

        static void AddTile(Board board, Vector3 pos)
        {
            var go = new GameObject("Tile_" + board.TileCount);
            Undo.RegisterCreatedObjectUndo(go, "Add Tile");
            go.transform.SetParent(board.transform, true);
            go.transform.position = pos;

            Undo.RecordObject(board, "Add Tile");
            board.Tiles.Add(go.transform);
            EditorUtility.SetDirty(board);
        }

        static void RemoveLast(Board board)
        {
            if (board.TileCount == 0) return;

            Undo.RecordObject(board, "Remove Tile");
            Transform last = board.Tiles[board.TileCount - 1];
            board.Tiles.RemoveAt(board.TileCount - 1);
            if (last != null) Undo.DestroyObjectImmediate(last.gameObject);
            EditorUtility.SetDirty(board);
        }

        static void AlignSceneView()
        {
            Camera cam = Camera.main;
            SceneView view = SceneView.lastActiveSceneView;
            if (cam == null || view == null) return;

            view.orthographic = cam.orthographic;
            view.AlignViewToObject(cam.transform);
        }

        [MenuItem("RPG Playable/Snap Selection To Nearest Tile")]
        static void SnapSelectionToTile()
        {
            var board = FindFirstObjectByType<Board>();
            if (board == null || board.TileCount == 0)
            {
                Debug.LogWarning("No Board with tiles in the scene");
                return;
            }

            foreach (Transform t in Selection.transforms)
            {
                Transform nearest = null;
                float best = float.MaxValue;
                foreach (Transform tile in board.Tiles)
                {
                    if (tile == null) continue;
                    Vector3 d = tile.position - t.position;
                    d.y = 0f;
                    if (d.sqrMagnitude < best)
                    {
                        best = d.sqrMagnitude;
                        nearest = tile;
                    }
                }

                if (nearest != null)
                {
                    Undo.RecordObject(t, "Snap To Tile");
                    t.position = nearest.position;
                }
            }
        }

        // Adds TileObject to every monster / chest standing next to the Board (scene objects only,
        // the prefabs stay clean so the battle copies don't get one).
        [MenuItem("RPG Playable/Setup Tile Objects")]
        static void SetupTileObjects()
        {
            var board = FindFirstObjectByType<Board>();
            if (board == null || board.transform.parent == null)
            {
                Debug.LogWarning("No Board in the scene");
                return;
            }

            int monsters = 0, pickups = 0;
            foreach (Transform child in board.transform.parent)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                string name = source != null ? source.name : child.name;

                TileObject.Kind kind;
                if (name.StartsWith("Slime") || name.StartsWith("Troll") || name.StartsWith("Treant")) kind = TileObject.Kind.Monster;
                else if (name.StartsWith("chest")) kind = TileObject.Kind.Pickup;
                else continue;

                var obj = child.GetComponent<TileObject>();
                if (obj == null) obj = Undo.AddComponent<TileObject>(child.gameObject);

                var so = new SerializedObject(obj);
                so.FindProperty("kind").enumValueIndex = (int)kind;
                so.ApplyModifiedProperties();

                if (kind == TileObject.Kind.Monster) monsters++;
                else pickups++;
            }

            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
            Debug.Log("Tile objects: " + monsters + " monsters, " + pickups + " pickups");
        }
    }
}
