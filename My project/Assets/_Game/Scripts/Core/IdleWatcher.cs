using UnityEngine;

namespace RpgPlayable
{
    // README: no input for 10s anywhere except battle -> big PLAY NOW.
    public class IdleWatcher : MonoBehaviour
    {
        [SerializeField] GameFlowController flow;
        [SerializeField] AfkOverlay overlay;
        [SerializeField] float idleSeconds = 10f;

        float timer;

        void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                timer = 0f;

            bool paused = overlay.IsShowing
                || flow.CurrentStep == FlowStep.Battle
                || flow.CurrentStep == FlowStep.Ending;
            if (paused)
            {
                timer = 0f;
                return;
            }

            timer += Time.deltaTime;
            if (timer >= idleSeconds)
            {
                timer = 0f;
                overlay.Show();
            }
        }
    }
}
