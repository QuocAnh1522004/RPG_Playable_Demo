using System;
using UnityEngine;

namespace RpgPlayable
{
    [Serializable]
    public class StageConfig
    {
        public string label = "Small Monster";
        public string monsterName = "SLIME";
        public int targetTileIndex; // dice result = this - hero's current tile

        public GameObject battlePrefab;
        public float battleScale = 1f;

        public int roundsToKill = 1;
        public int monsterMaxHp = 100;
        public int monsterAttack = 40;

        public bool ranged; // throws battleProjectile instead of walking up
        public GameObject battleProjectile;
        public GameObject monsterHitFx;
    }
}
