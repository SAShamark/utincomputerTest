using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField] private BalanceConfig _balanceConfig;
        [SerializeField] private List<ZoneShots> _zones = new();

        private const float REQUIRED_RESERVE = 1.2f;

        public int LevelNumber => _levelNumber;

        public float ReferenceShotsMass
        {
            get
            {
                float totalMass = 0f;
                foreach (ZoneShots zone in _zones)
                {
                    totalMass += zone.TotalMass;
                }

                return totalMass;
            }
        }

        public int ReferenceShotsCount
        {
            get
            {
                int shotsCount = 0;
                foreach (ZoneShots zone in _zones)
                {
                    shotsCount += zone.ShotMasses.Count;
                }

                return shotsCount;
            }
        }

        public float Reserve
        {
            get
            {
                if (_balanceConfig == null)
                {
                    return 0f;
                }

                float referenceMass = ReferenceShotsMass + ReferenceShotsCount * _balanceConfig.ShotLaunchCost;
                if (referenceMass <= 0f)
                {
                    return 0f;
                }

                return (_balanceConfig.StartMass - _balanceConfig.MinMass) / referenceMass;
            }
        }

        private void OnValidate()
        {
            if (_balanceConfig == null || _zones.Count == 0)
            {
                return;
            }

            float reserve = Reserve;
            if (reserve < REQUIRED_RESERVE)
            {
                Debug.LogWarning($"[LevelConfig] Reserve {reserve:F2} is lower than required {REQUIRED_RESERVE:F2}");
            }
        }
    }

    [System.Serializable]
    public class ZoneShots
    {
        [SerializeField] private string _name;
        [SerializeField] private List<float> _shotMasses = new();

        public string Name => _name;
        public IReadOnlyList<float> ShotMasses => _shotMasses;

        public float TotalMass
        {
            get
            {
                float totalMass = 0f;
                foreach (float shotMass in _shotMasses)
                {
                    totalMass += shotMass;
                }

                return totalMass;
            }
        }
    }
}
