using UnityEngine;

namespace RpgPlayable
{
    public class StoreRedirect : MonoBehaviour
    {
        public static StoreRedirect Instance { get; private set; }

        bool gameEndedSent;

        void Awake()
        {
            Instance = this;
        }

        public void MarkGameEnded()
        {
            if (gameEndedSent) return;
            gameEndedSent = true;
            Luna.Unity.LifeCycle.GameEnded();
        }

        public void OpenStore()
        {
#if UNITY_EDITOR
            Debug.Log("OpenStore called (only works in the Luna build)");
#endif
            Luna.Unity.Playable.InstallFullGame();
        }
    }
}
