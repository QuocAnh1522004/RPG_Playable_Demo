using System;
using UnityEngine;

namespace RpgPlayable
{
    [Serializable]
    public class MonsterSounds
    {
        public string monsterName; // matches StageConfig.monsterName
        public AudioClip dash;
        public AudioClip attack;
        public AudioClip attackHit;
        public AudioClip hurt;
        public AudioClip die;
    }

    [CreateAssetMenu(menuName = "RPG Playable/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        public AudioClip music;

        [Header("Board")]
        public AudioClip diceTap;
        public AudioClip diceRoll;
        public AudioClip diceResult;
        public AudioClip panelOpen;
        public AudioClip panelClose;
        public AudioClip pickup;
        public AudioClip upgrade;
        public AudioClip[] footsteps;

        [Header("Hero")]
        public AudioClip heroDash;
        public AudioClip heroJumpBack;
        public AudioClip[] heroSwing;
        public AudioClip[] heroHurt;

        public MonsterSounds[] monsters;

        public MonsterSounds ForMonster(string monsterName)
        {
            if (monsters == null || monsterName == null) return null;
            string key = monsterName.ToUpper();
            foreach (MonsterSounds m in monsters)
                if (m.monsterName != null && m.monsterName.ToUpper() == key) return m;
            return null;
        }
    }
}
