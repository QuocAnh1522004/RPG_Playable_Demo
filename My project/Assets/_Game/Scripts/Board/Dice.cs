using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RpgPlayable
{
    // Put this on an anchor under the Main Camera so the dice stays at the bottom of the screen.
    // No collider/raycast: the physics module is stripped from the Luna build, so taps are checked in screen space.
    public class Dice : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] Vector3[] faceRotations = new Vector3[6]; // local euler with face 1..6 on top
        [SerializeField] int restingFace = 3; // what it shows before the first roll

        [SerializeField] float tapRadius = 0.1f; // fraction of screen height
        [SerializeField] float jumpHeight = 3f;
        [SerializeField] float rollDuration = 1.1f;
        [SerializeField] int spins = 2;

        Camera cam;
        Vector3 restPosition;
        bool waitingForTap;
        bool tapped;

        public Transform Visual { get { return visual; } }
        public bool IsWaitingForTap { get { return waitingForTap; } }
        public Vector3 ScreenPosition { get { return cam.WorldToScreenPoint(visual.position); } }

        void Awake()
        {
            cam = Camera.main;
            restPosition = visual.localPosition;
            visual.localEulerAngles = faceRotations[Mathf.Clamp(restingFace, 1, 6) - 1];
        }

        void Update()
        {
            if (!waitingForTap || !Input.GetMouseButtonDown(0)) return;
            if (TapIsOnUi()) return; // e.g. the AFK overlay is up

            float dist = Vector2.Distance(ScreenPosition, Input.mousePosition);
            if (dist <= tapRadius * Screen.height)
                tapped = true;
        }

        static bool TapIsOnUi()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            if (es.IsPointerOverGameObject()) return true;
            return Input.touchCount > 0 && es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        public IEnumerator WaitForTapAndRoll(int result, System.Action onTapped)
        {
            tapped = false;
            waitingForTap = true;
            while (!tapped) yield return null;
            waitingForTap = false;

            if (onTapped != null) onTapped();
            AudioManager.Play(AudioManager.Sounds.diceTap);
            yield return Roll(result);
        }

        IEnumerator Roll(int result)
        {
            Vector3 face = faceRotations[Mathf.Clamp(result, 1, 6) - 1];
            Vector3 spin = new Vector3(360f * spins, 360f * (spins + 1), 360f * spins);

            visual.localPosition = restPosition;
            visual.DOLocalRotate(face + spin, rollDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic);

            Sequence seq = DOTween.Sequence();
            seq.Append(visual.DOLocalMoveY(restPosition.y + jumpHeight, rollDuration * 0.45f).SetEase(Ease.OutQuad));
            seq.Append(visual.DOLocalMoveY(restPosition.y, rollDuration * 0.4f).SetEase(Ease.InQuad));
            seq.Append(visual.DOLocalMoveY(restPosition.y + jumpHeight * 0.12f, rollDuration * 0.08f).SetEase(Ease.OutQuad));
            seq.Append(visual.DOLocalMoveY(restPosition.y, rollDuration * 0.07f).SetEase(Ease.InQuad));
            seq.Append(visual.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.5f));
            seq.InsertCallback(rollDuration * 0.85f, () => AudioManager.Play(AudioManager.Sounds.diceResult));
            AudioManager.Play(AudioManager.Sounds.diceRoll);

            yield return TweenWait.For(seq);
            yield return new WaitForSeconds(0.2f);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (faceRotations == null || faceRotations.Length != 6)
                System.Array.Resize(ref faceRotations, 6);
        }
#endif
    }
}
