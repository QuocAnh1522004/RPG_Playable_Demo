using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    // Anything standing on a board tile. When the hero walks through its tile (without stopping there)
    // a pickup pops and vanishes, a monster shrinks away.
    public class TileObject : MonoBehaviour
    {
        public enum Kind { Pickup, Monster }

        [SerializeField] Kind kind = Kind.Pickup;
        [SerializeField] int tile = -1; // -1 = use the nearest tile

        static readonly List<TileObject> all = new List<TileObject>();

        bool cleared;

        void OnEnable()
        {
            all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
        }

        public static void ClearAt(Board board, int tileIndex)
        {
            // copy, Clear() can end up disabling objects
            var onTile = new List<TileObject>();
            foreach (TileObject o in all)
            {
                if (o.TileIndex(board) == tileIndex) onTile.Add(o);
            }
            foreach (TileObject o in onTile) o.Clear();
        }

        int TileIndex(Board board)
        {
            if (tile < 0) tile = board.NearestTile(transform.position);
            return tile;
        }

        void Clear()
        {
            if (cleared) return;
            cleared = true;
            transform.DOKill();

            if (kind == Kind.Pickup)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOMoveY(transform.position.y + 0.8f, 0.2f).SetEase(Ease.OutQuad));
                seq.Join(transform.DOScale(transform.localScale * 1.25f, 0.2f));
                seq.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
                seq.OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                transform.DOScale(0f, 0.35f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
            }
        }
    }
}
