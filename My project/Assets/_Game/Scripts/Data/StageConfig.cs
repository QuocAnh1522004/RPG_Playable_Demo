using System;
using UnityEngine;

namespace RpgPlayable
{
    [Serializable]
    public class StageConfig
    {
        public string label = "Small Monster";
        public int targetTileIndex; // dice result = this - hero's current tile
        public GameObject monster;
        public int roundsToKill = 1;
        public int monsterMaxHp = 100;
    }
}
