using UnityEngine;

namespace RpgPlayable
{
    // Puts UI over a world position using anchors (0..1 of the screen) instead of pixels.
    // WorldToScreenPoint pixels don't match the canvas in the Luna build (browser pixel ratio),
    // normalized coordinates mean the same thing everywhere.
    public static class UiAnchor
    {
        // 0..1 over the whole screen, also correct for cameras that only draw part of it (battle camera)
        public static Vector2 FromWorld(Camera cam, Vector3 world)
        {
            Vector3 v = cam.WorldToViewportPoint(world);
            Rect r = cam.rect;
            return new Vector2(r.x + v.x * r.width, r.y + v.y * r.height);
        }

        // rt's parent must cover the whole canvas
        public static void Place(RectTransform rt, Vector2 normalized, Vector2 offset)
        {
            rt.anchorMin = normalized;
            rt.anchorMax = normalized;
            rt.anchoredPosition = offset;
        }
    }
}
