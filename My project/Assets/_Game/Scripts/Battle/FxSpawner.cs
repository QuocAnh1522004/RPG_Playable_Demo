using UnityEngine;

namespace RpgPlayable
{
    public static class FxSpawner
    {
        // most FX in the pack have Play On Awake off, so start every particle system by hand
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime, float scale)
        {
            if (prefab == null) return null;

            GameObject go = Object.Instantiate(prefab, position, rotation);
            // the *_reusable prefabs are saved disabled (they came from a pool in the original game)
            if (!go.activeSelf) go.SetActive(true);
            if (scale != 1f) go.transform.localScale *= scale;

            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play(false);

            if (lifetime > 0f) Object.Destroy(go, lifetime);
            return go;
        }
    }
}
