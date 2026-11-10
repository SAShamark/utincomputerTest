using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "ScriptableObjects/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        [Header("Size")]
        [SerializeField, Min(0.01f)] private float _radiusScale = 1f;
        [SerializeField, Min(0.01f)] private float _startMass = 0.9f;
        [SerializeField, Min(0.001f)] private float _minMass = 0.03f;
        [SerializeField, Min(0.001f)] private float _minShotMass = 0.02f;

        [Header("Charge")]
        [SerializeField, Min(0f)] private float _baseChargeRate = 0.08f;
        [SerializeField, Min(0f)] private float _chargeAcceleration = 0.25f;

        [Header("Shot")]
        [SerializeField, Min(0.1f)] private float _shotSpeed = 14f;
        [SerializeField, Min(1f)] private float _shotMaxDistance = 40f;

        [Header("Infection")]
        [SerializeField, Min(1f)] private float _infectionBudgetScale = 380f;
        [SerializeField, Min(1f)] private float _infectionBudgetExponent = 1.5f;
        [SerializeField, Min(0f)] private float _shotLaunchCost = 0.01f;
        [SerializeField, Min(0.1f)] private float _infectionMultiplier = 2.5f;
        [SerializeField, Min(0.1f)] private float _linkDistance = 0.9f;
        [SerializeField, Min(0f)] private float _hopEnergyCost = 0.5f;
        [SerializeField, Min(0f)] private float _chainDelay = 0.08f;
        [SerializeField, Min(0f)] private float _explodeDelay = 0.2f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _hopLength = 1.2f;
        [SerializeField, Min(0.05f)] private float _hopDuration = 0.25f;
        [SerializeField, Min(0f)] private float _hopHeight = 0.5f;
        [SerializeField, Range(0.5f, 1f)] private float _pathWidthTolerance = 0.9f;

        [Header("Door")]
        [SerializeField, Min(0f)] private float _doorOpenDistance = 5f;

        private const float ONE_THIRD = 1f / 3f;

        public float DoorOpenDistance => _doorOpenDistance;

        public float HopLength => _hopLength;
        public float HopDuration => _hopDuration;
        public float HopHeight => _hopHeight;
        public float PathWidthTolerance => _pathWidthTolerance;

        public float InfectionMultiplier => _infectionMultiplier;
        public float ShotLaunchCost => _shotLaunchCost;
        public float LinkDistance => _linkDistance;
        public float HopEnergyCost => _hopEnergyCost;
        public float ChainDelay => _chainDelay;
        public float ExplodeDelay => _explodeDelay;

        public float RadiusScale => _radiusScale;
        public float StartMass => _startMass;
        public float MinMass => _minMass;
        public float MinShotMass => _minShotMass;
        public float BaseChargeRate => _baseChargeRate;
        public float ChargeAcceleration => _chargeAcceleration;
        public float ShotSpeed => _shotSpeed;
        public float ShotMaxDistance => _shotMaxDistance;

        public int GetInfectionBudget(float shotMass)
        {
            return Mathf.Max(1, Mathf.RoundToInt(_infectionBudgetScale * Mathf.Pow(shotMass, _infectionBudgetExponent)));
        }

        public float MassToRadius(float mass)
        {
            return Mathf.Pow(Mathf.Max(0f, mass), ONE_THIRD) * _radiusScale;
        }
    }
}
