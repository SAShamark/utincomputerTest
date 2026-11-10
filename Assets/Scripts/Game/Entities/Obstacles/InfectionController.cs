using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Entities.Player;
using Game.Entities.Shot;
using Services;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Obstacles
{
    public class InfectionController : MonoBehaviour
    {
        [SerializeField] private LayerMask _obstaclesMask;
        [SerializeField] private ParticleSystem _impactEffectPrefab;
        [SerializeField] private ParticleSystem _explosionEffectPrefab;
        [SerializeField] private Transform _effectsContainer;
        [SerializeField] private float _effectLifetime = 1.5f;
        [SerializeField] private float _explosionEffectHeight = 0.6f;
        [SerializeField] private float _shakeStrengthPerRadius = 0.3f;
        [SerializeField] private float _shakeDuration = 0.25f;

        private const int OVERLAP_BUFFER_SIZE = 128;
        private const int IMPACT_EFFECTS_POOL_SIZE = 2;
        private const int EXPLOSION_EFFECTS_POOL_SIZE = 24;

        private readonly Collider[] _overlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];
        private readonly List<SpreadTask> _spreadTasks = new();
        private readonly List<ObstacleControl> _waitingExplosion = new();
        private readonly List<ObstacleControl> _sortedObstacles = new();

        private BalanceConfig _balanceConfig;
        private PlayerBallControl _playerBallControl;
        private CameraControl _cameraControl;
        private ObjectPool<ParticleSystem> _impactEffectsPool;
        private ObjectPool<ParticleSystem> _explosionEffectsPool;
        private bool _isInfectionRunning;

        public bool IsActive => _spreadTasks.Count > 0 || _waitingExplosion.Count > 0;

        public event Action OnInfectionFinished;
        public event Action OnObstacleExploded;

        public void Init(BalanceConfig balanceConfig, PlayerBallControl playerBallControl, CameraControl cameraControl)
        {
            _balanceConfig = balanceConfig;
            _playerBallControl = playerBallControl;
            _cameraControl = cameraControl;
            _impactEffectsPool = new ObjectPool<ParticleSystem>(_impactEffectPrefab, IMPACT_EFFECTS_POOL_SIZE,
                _effectsContainer);
            _explosionEffectsPool = new ObjectPool<ParticleSystem>(_explosionEffectPrefab,
                EXPLOSION_EFFECTS_POOL_SIZE, _effectsContainer);
            _playerBallControl.OnShotImpact += HandleShotImpact;
        }

        private void OnDestroy()
        {
            if (_playerBallControl != null)
            {
                _playerBallControl.OnShotImpact -= HandleShotImpact;
            }
        }

        private void Update()
        {
            if (!_isInfectionRunning)
            {
                return;
            }

            ProcessSpreadTasks();
            CheckFinished();
        }

        private void HandleShotImpact(ShotProjectile shot, Vector3 impactPoint)
        {
            float infectionRadius = shot.Radius * _balanceConfig.InfectionMultiplier;
            int budgetCount = _balanceConfig.GetInfectionBudget(shot.Mass);
            InfectionBudget budget = new InfectionBudget(budgetCount);

            CollectSortedObstacles(impactPoint, infectionRadius, null);
            foreach (ObstacleControl obstacle in _sortedObstacles)
            {
                float distance = HorizontalDistance(impactPoint, obstacle.transform.position);
                Infect(obstacle, Mathf.Max(infectionRadius - distance, ValueConstants.EPSILON), budget);
            }

            PlayEffect(_impactEffectsPool, impactPoint);
            _cameraControl.Shake(shot.Radius * _shakeStrengthPerRadius, _shakeDuration);
            _cameraControl.PunchZoom(shot.Radius);
            Debug.Log($"Shot impact: mass {shot.Mass:F3}, infection radius {infectionRadius:F2}, budget {budgetCount}");
        }

        private void Infect(ObstacleControl obstacle, float energy, InfectionBudget budget)
        {
            bool isNewInfection = !obstacle.IsInfected;
            if (isNewInfection && budget.Remaining <= 0)
            {
                return;
            }

            if (!obstacle.TryInfect(energy, _balanceConfig.ExplodeDelay))
            {
                return;
            }

            if (isNewInfection)
            {
                budget.Spend();
            }

            if (!_waitingExplosion.Contains(obstacle))
            {
                _waitingExplosion.Add(obstacle);
                obstacle.OnExploded += HandleObstacleExploded;
            }

            _spreadTasks.Add(new SpreadTask(obstacle, energy, budget, Time.time + _balanceConfig.ChainDelay));
            _isInfectionRunning = true;
        }

        private void ProcessSpreadTasks()
        {
            for (int i = _spreadTasks.Count - 1; i >= 0; i--)
            {
                SpreadTask task = _spreadTasks[i];
                if (Time.time < task.SpreadTime)
                {
                    continue;
                }

                _spreadTasks.RemoveAt(i);
                Spread(task.Source, task.Energy, task.Budget);
            }
        }

        private void Spread(ObstacleControl source, float energy, InfectionBudget budget)
        {
            Vector3 sourcePosition = source.transform.position;
            CollectSortedObstacles(sourcePosition, _balanceConfig.LinkDistance, source);

            foreach (ObstacleControl neighbour in _sortedObstacles)
            {
                float distance = HorizontalDistance(sourcePosition, neighbour.transform.position);
                float neighbourEnergy = energy - distance * _balanceConfig.HopEnergyCost;

                if (neighbourEnergy > 0f)
                {
                    Infect(neighbour, neighbourEnergy, budget);
                }
            }
        }

        private void CollectSortedObstacles(Vector3 center, float radius, ObstacleControl ignored)
        {
            _sortedObstacles.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, _overlapBuffer, _obstaclesMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i].TryGetComponent(out ObstacleControl obstacle) && obstacle != ignored)
                {
                    _sortedObstacles.Add(obstacle);
                }
            }

            _sortedObstacles.Sort((first, second) =>
                HorizontalDistance(center, first.transform.position)
                    .CompareTo(HorizontalDistance(center, second.transform.position)));
        }

        private void HandleObstacleExploded(ObstacleControl obstacle)
        {
            obstacle.OnExploded -= HandleObstacleExploded;
            _waitingExplosion.Remove(obstacle);
            PlayEffect(_explosionEffectsPool, obstacle.transform.position + Vector3.up * _explosionEffectHeight);
            OnObstacleExploded?.Invoke();
        }

        private void PlayEffect(ObjectPool<ParticleSystem> pool, Vector3 position)
        {
            ParticleSystem effect = pool.GetFreeElement();
            effect.transform.position = position;
            effect.Clear(true);
            effect.Play(true);
            DOVirtual.DelayedCall(_effectLifetime, () => pool.ReturnToPool(effect)).SetLink(gameObject);
        }

        private void CheckFinished()
        {
            if (IsActive)
            {
                return;
            }

            _isInfectionRunning = false;
            OnInfectionFinished?.Invoke();
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        private readonly struct SpreadTask
        {
            public readonly ObstacleControl Source;
            public readonly float Energy;
            public readonly InfectionBudget Budget;
            public readonly float SpreadTime;

            public SpreadTask(ObstacleControl source, float energy, InfectionBudget budget, float spreadTime)
            {
                Source = source;
                Energy = energy;
                Budget = budget;
                SpreadTime = spreadTime;
            }
        }

        private class InfectionBudget
        {
            public int Remaining { get; private set; }

            public InfectionBudget(int remaining)
            {
                Remaining = remaining;
            }

            public void Spend()
            {
                Remaining--;
            }
        }
    }
}
