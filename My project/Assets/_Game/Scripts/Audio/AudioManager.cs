using UnityEngine;

namespace RpgPlayable
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] SoundLibrary library;
        [SerializeField] float musicVolume = 0.4f;
        [SerializeField] float sfxVolume = 1f;

        static AudioManager instance;
        static SoundLibrary empty;

        AudioSource music;
        AudioSource sfx;

        // never null, so callers can write AudioManager.Play(AudioManager.Sounds.diceTap) without checks
        public static SoundLibrary Sounds
        {
            get
            {
                if (instance != null && instance.library != null) return instance.library;
                if (empty == null) empty = ScriptableObject.CreateInstance<SoundLibrary>();
                return empty;
            }
        }

        void Awake()
        {
            instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = musicVolume;
            if (library != null) music.clip = library.music;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
        }

        void Start()
        {
            if (music.clip != null) music.Play();
        }

        void Update()
        {
            // browsers block audio until the first touch, kick the music again once that happens
            if (Input.GetMouseButtonDown(0) && music.clip != null && !music.isPlaying)
                music.Play();
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (instance == null || clip == null) return;
            instance.sfx.PlayOneShot(clip, volume * instance.sfxVolume);
        }

        public static void PlayRandom(AudioClip[] clips, float volume = 1f)
        {
            if (clips == null || clips.Length == 0) return;
            Play(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}
