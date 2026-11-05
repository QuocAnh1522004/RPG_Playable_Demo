using System.Collections.Generic;
using UnityEngine;

namespace RpgPlayable
{
    [CreateAssetMenu(menuName = "RPG Playable/Skill Library", fileName = "SkillLibrary")]
    public class SkillLibrary : ScriptableObject
    {
        public SkillData basicAttack;
        public List<SkillData> skills = new List<SkillData>();

        public List<SkillData> PickRandom(int count, ICollection<SkillData> exclude)
        {
            var pool = new List<SkillData>();
            foreach (var s in skills)
            {
                if (s != null && s.selectable && (exclude == null || !exclude.Contains(s)))
                    pool.Add(s);
            }

            var result = new List<SkillData>();
            while (result.Count < count && pool.Count > 0)
            {
                int i = Random.Range(0, pool.Count);
                result.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return result;
        }
    }
}
