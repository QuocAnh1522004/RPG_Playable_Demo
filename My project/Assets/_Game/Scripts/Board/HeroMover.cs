using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace RpgPlayable
{
    public class HeroMover : MonoBehaviour
    {
        [SerializeField] Board board;
        [SerializeField] ActorAnimator anim;
        [SerializeField] float hopDuration = 0.3f;
        [SerializeField] float hopHeight = 0.35f;
        [SerializeField] float turnDuration = 0.1f;

        public int CurrentTile { get; private set; }

        void Awake()
        {
            if (anim == null) anim = GetComponent<ActorAnimator>();
        }

        public void PlaceAt(int tile)
        {
            CurrentTile = tile;
            transform.position = board.GetTilePosition(tile);
        }

        public IEnumerator MoveTo(int targetTile)
        {
            if (anim != null) anim.Play(ActorAnimator.Run);

            while (CurrentTile < targetTile)
            {
                int next = CurrentTile + 1;
                Vector3 to = board.GetTilePosition(next);

                Vector3 dir = to - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.DORotateQuaternion(Quaternion.LookRotation(dir), turnDuration);

                Tween hop = transform.DOJump(to, hopHeight, 1, hopDuration).SetEase(Ease.Linear);
                yield return TweenWait.For(hop);
                CurrentTile = next;
            }

            if (anim != null) anim.Play(ActorAnimator.Idle);
        }
    }
}
