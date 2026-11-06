using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace RpgPlayable
{
    // Put this on an anchor under the Main Camera so the dice stays at the bottom of the screen.
    // Taps go through an invisible UI button parked over the dice: comparing Input.mousePosition by hand
    // broke in the browser (device pixel ratio), the UI event system handles that for us.
    public class Dice : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] Vector3[] faceRotations = new Vector3[6]; // local euler with face 1..6 on top
        [SerializeField] int restingFace = 3; // what it shows before the first roll

        [SerializeField] Button tapButton; // built in the editor (RPG Playable menu), sits on the canvas
        [SerializeField] float jumpHeight = 3f;
        [SerializeField] float rollDuration = 1.1f;
        [SerializeField] int spins = 2;

        Camera cam;
        RectTransform tapArea;
        Vector3 restPosition;
        bool waitingForTap;
        bool tapped;

        public Transform Visual { get { return visual; } }
        public bool IsWaitingForTap { get { return waitingForTap; } }
        public Vector2 AnchorPosition { get { return UiAnchor.FromWorld(cam, visual.position); } }

        void Awake()
        {
            cam = Camera.main;
            restPosition = visual.localPosition;
            visual.localEulerAngles = faceRotations[Mathf.Clamp(restingFace, 1, 6) - 1];

            if (tapButton != null)
            {
                tapArea = (RectTransform)tapButton.transform;
                tapButton.onClick.AddListener(OnTapAreaClicked);
                tapButton.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("Dice has no Tap Button, run RPG Playable > Build Dice Tap Area");
            }
        }

        void OnTapAreaClicked()
        {
            if (waitingForTap) tapped = true;
        }

        void LateUpdate()
        {
            if (tapArea != null && tapArea.gameObject.activeSelf)
                UiAnchor.Place(tapArea, AnchorPosition, Vector2.zero);
        }

        public IEnumerator WaitForTapAndRoll(int result, System.Action onTapped)
        {
            tapped = false;
            waitingForTap = true;
            if (tapArea != null)
            {
                tapArea.gameObject.SetActive(true);
                UiAnchor.Place(tapArea, AnchorPosition, Vector2.zero);
            }

            while (!tapped) yield return null;

            waitingForTap = false;
            if (tapArea != null) tapArea.gameObject.SetActive(false);

            if (onTapped != null) onTapped();
            AudioManager.Play(AudioManager.Sounds.diceTap);
            yield return Roll(result);
            yield return null; // Luna, see TweenWait
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
