using System;
using Game.Entities.Shot;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Player.Parts
{
    [Serializable]
    public class ShootingLogic
    {
        [SerializeField] private ShotProjectile _shotPrefab;
        [SerializeField] private Transform _shotsContainer;

        private const int SHOTS_POOL_SIZE = 5;

        private readonly ChargeLogic _chargeLogic = new();

        private BalanceConfig _balanceConfig;
        private BallSizeLogic _sizeLogic;
        private Transform _origin;
        private Vector3 _direction;
        private ObjectPool<ShotProjectile> _shotsPool;
        private ShotProjectile _currentShot;
        private int _shotsInFlight;

        public bool IsCharging => _chargeLogic.IsCharging;
        public bool HasShotsInFlight => _shotsInFlight > 0;
        public ShotProjectile ChargingShot => _chargeLogic.IsCharging ? _currentShot : null;

        public event Action OnMassDepleted;
        public event Action<ShotProjectile, Vector3> OnShotImpact;
        public event Action OnShotFinished;

        public void Init(BalanceConfig balanceConfig, BallSizeLogic sizeLogic, Transform origin, Vector3 direction)
        {
            _balanceConfig = balanceConfig;
            _sizeLogic = sizeLogic;
            _origin = origin;
            _direction = direction;
            _chargeLogic.Init(_balanceConfig);
            _shotsPool = new ObjectPool<ShotProjectile>(_shotPrefab, SHOTS_POOL_SIZE, _shotsContainer);
        }

        public void BeginCharge()
        {
            _currentShot = _shotsPool.GetFreeElement();
            _currentShot.transform.SetParent(_shotsContainer, false);
            _currentShot.Prepare(_balanceConfig);
            _chargeLogic.Begin();
            UpdateShotPosition();
        }

        public void Tick(float deltaTime)
        {
            if (!_chargeLogic.IsCharging)
            {
                return;
            }

            float takenMass = _sizeLogic.Take(_chargeLogic.Tick(deltaTime));
            _currentShot.AddMass(takenMass);
            UpdateShotPosition();

            if (_sizeLogic.Mass < _balanceConfig.MinMass + _balanceConfig.ShotLaunchCost)
            {
                CancelCharge();
                OnMassDepleted?.Invoke();
            }
        }

        public void Release()
        {
            _chargeLogic.Stop();
            _shotsInFlight++;
            _currentShot.Launch(_direction, HandleShotImpact, HandleShotMissed);
            _currentShot = null;
            _sizeLogic.Take(_balanceConfig.ShotLaunchCost);
        }

        public void Stop()
        {
            _chargeLogic.Stop();
        }

        private void CancelCharge()
        {
            _chargeLogic.Stop();
            _shotsPool.ReturnToPool(_currentShot);
            _currentShot = null;
        }

        private void UpdateShotPosition()
        {
            float shotRadius = _currentShot.Radius;
            _currentShot.transform.position = _origin.position
                                              + _direction * (_sizeLogic.Radius + shotRadius)
                                              + Vector3.up * shotRadius;
        }

        private void HandleShotImpact(ShotProjectile shot, Vector3 impactPoint)
        {
            OnShotImpact?.Invoke(shot, impactPoint);
            FinishShot(shot);
        }

        private void HandleShotMissed(ShotProjectile shot)
        {
            FinishShot(shot);
        }

        private void FinishShot(ShotProjectile shot)
        {
            _shotsPool.ReturnToPool(shot);
            _shotsInFlight--;
            OnShotFinished?.Invoke();
        }
    }
}
