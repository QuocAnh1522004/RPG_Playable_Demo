using System.Collections.Generic;
using UnityEngine;

namespace RpgPlayable
{
    public class Board : MonoBehaviour
    {
        // everything on the board (tiles, hero, monsters) stands on this height
        [SerializeField] float groundY = -1.55f;
        [SerializeField] List<Transform> tiles = new List<Transform>();

        public float GroundY { get { return groundY; } }
        public List<Transform> Tiles { get { return tiles; } }
        public int TileCount { get { return tiles.Count; } }

        public Vector3 GetTilePosition(int index)
        {
            return tiles[Mathf.Clamp(index, 0, tiles.Count - 1)].position;
        }

        public int NearestTile(Vector3 position)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null) continue;
                Vector3 d = tiles[i].position - position;
                d.y = 0f;
                if (d.sqrMagnitude < bestDist)
                {
                    bestDist = d.sqrMagnitude;
                    best = i;
                }
            }
            return best;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null) continue;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(tiles[i].position, 0.25f);
                if (i > 0 && tiles[i - 1] != null)
                    Gizmos.DrawLine(tiles[i - 1].position, tiles[i].position);

                UnityEditor.Handles.Label(tiles[i].position + Vector3.up * 0.5f, i.ToString());
            }
        }
#endif
    }
}
