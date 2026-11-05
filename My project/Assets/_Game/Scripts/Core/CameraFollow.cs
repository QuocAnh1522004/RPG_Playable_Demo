using UnityEngine;

namespace RpgPlayable
{
    // Keeps the framing you set up in the editor and slides it along with the target.
    // Only position changes, the rotation has to stay matched to the map backdrop.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.25f;

        Vector3 offset;
        float targetBaseY;
        Vector3 velocity;

        void Start()
        {
            offset = transform.position - target.position;
            targetBaseY = target.position.y;
        }

        void LateUpdate()
        {
            // ignore hop height so the camera doesn't bob
            Vector3 focus = new Vector3(target.position.x, targetBaseY, target.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, focus + offset, ref velocity, smoothTime);
        }
    }
}
