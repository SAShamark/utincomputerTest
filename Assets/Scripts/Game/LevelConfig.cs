using System;
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

        private void OnValidate()
        {
            if (_balanceConfig == null || _zones.Count == 0)
            {
                return;
            }

            float reserve = CalculateReserve();
            if (reserve < REQUIRED_RESERVE)
            {
                Debug.LogWarning($"[LevelConfig] Reserve {reserve:F2} is lower than required {REQUIRED_RESERVE:F2}");
            }
        }

        private float CalculateReserve()
        {
            float referenceMass = 0f;
            foreach (ZoneShots zone in _zones)
            {
                referenceMass += zone.CalculateMass(_balanceConfig.ShotLaunchCost);
            }

            if (referenceMass <= 0f)
            {
                return 0f;
            }

            return (_balanceConfig.StartMass - _balanceConfig.MinMass) / referenceMass;
        }
    }

    [Serializable]
    public class ZoneShots
    {
        [SerializeField] private string _name;
        [SerializeField] private List<float> _shotMasses = new();

        public float CalculateMass(float shotLaunchCost)
        {
            float totalMass = 0f;
            foreach (float shotMass in _shotMasses)
            {
                totalMass += shotMass + shotLaunchCost;
            }

            return totalMass;
        }
    }
}
