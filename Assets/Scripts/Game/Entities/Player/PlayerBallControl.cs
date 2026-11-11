using System;
using Game.Controls;
using Game.Entities.Player.Parts;
using Game.Entities.Shot;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Player
{
    public class PlayerBallControl : MonoBehaviour
    {
        [SerializeField] private Transform _view;
        [SerializeField] private ShotProjectile _shotPrefab;
        [SerializeField] private Transform _shotsContainer;
        [SerializeField] private PlayerBallFeedback _feedback;

        private const int SHOTS_POOL_SIZE = 5;

        private readonly BallSizeLogic _sizeLogic = new();
        private readonly ChargeLogic _chargeLogic = new();
        private readonly HopMovementLogic _hopMovementLogic = new();

        private BalanceConfig _balanceConfig;
        private TapInput _tapInput;
        private ObjectPool<ShotProjectile> _shotsPool;
        private ShotProjectile _currentShot;
        private Vector3 _moveDirection;
        private Vector3 _targetPosition;
        private bool _isGameplayActive;
        private int _shotsInFlight;

        public float Mass => _sizeLogic.Mass;
        public float Radius => _sizeLogic.Radius;
        public Vector3 MoveDirection => _moveDirection;
        public bool CanShoot => _sizeLogic.Mass - _balanceConfig.MinMass >=
                                _balanceConfig.MinShotMass + _balanceConfig.ShotLaunchCost;
        public bool IsBusy => _chargeLogic.IsCharging || _shotsInFlight > 0 || _hopMovementLogic.IsMoving;
        public float DistanceToTarget => HorizontalDistanceToTarget();
        public ShotProjectile ChargingShot => _chargeLogic.IsCharging ? _currentShot : null;

        public event Action OnMassDepleted;
        public event Action<float> OnMassChanged;
        public event Action<ShotProjectile, Vector3> OnShotImpact;
        public event Action OnShotFinished;
        public event Action OnMovementFinished;

        public void Init(BalanceConfig balanceConfig, TapInput tapInput, Vector3 targetPosition)
        {
            _balanceConfig = balanceConfig;
            _tapInput = tapInput;
            _shotsPool = new ObjectPool<ShotProjectile>(_shotPrefab, SHOTS_POOL_SIZE, _shotsContainer);

            _targetPosition = targetPosition;
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _moveDirection = toTarget.normalized;
            transform.rotation = Quaternion.LookRotation(_moveDirection);

            _sizeLogic.OnMassChanged += HandleMassChanged;
            _sizeLogic.Init(_balanceConfig, _view);
            _chargeLogic.Init(_balanceConfig);
            _hopMovementLogic.Init(_balanceConfig, transform, _moveDirection);
            _feedback.Init(transform);
            _isGameplayActive = true;

            _hopMovementLogic.OnMovementFinished += HandleMovementFinished;
            _hopMovementLogic.OnHopLanded += HandleHopLanded;

            _tapInput.OnPressed += HandlePressed;
            _tapInput.OnReleased += HandleReleased;
        }

        private void OnDestroy()
        {
            if (_tapInput == null)
            {
                return;
            }

            _feedback.Kill();
            _hopMovementLogic.OnHopLanded -= HandleHopLanded;
            _tapInput.OnPressed -= HandlePressed;
            _tapInput.OnReleased -= HandleReleased;
            _hopMovementLogic.OnMovementFinished -= HandleMovementFinished;
            _sizeLogic.OnMassChanged -= HandleMassChanged;
        }

        private void Update()
        {
            if (!_isGameplayActive)
            {
                return;
            }

            _hopMovementLogic.Tick(Time.deltaTime);

            if (_chargeLogic.IsCharging)
            {
                Charge(Time.deltaTime);
            }
        }

        public void MoveForward(float distance)
        {
            _hopMovementLogic.Move(distance);
        }

        public void StopGameplay()
        {
            _isGameplayActive = false;
            _chargeLogic.Stop();
            _hopMovementLogic.Stop();
            _feedback.PlayChargeCancelled();
        }

        private void HandlePressed()
        {
            if (!_isGameplayActive || _chargeLogic.IsCharging || _hopMovementLogic.IsMoving || !CanShoot)
            {
                return;
            }

            _currentShot = _shotsPool.GetFreeElement();
            _currentShot.transform.SetParent(_shotsContainer, false);
            _currentShot.Prepare(_balanceConfig);
            _chargeLogic.Begin();
            UpdateShotPosition();
            _feedback.PlayChargeStarted();
        }

        private void HandleReleased()
        {
            if (!_isGameplayActive || !_chargeLogic.IsCharging)
            {
                return;
            }

            _chargeLogic.Stop();
            _shotsInFlight++;
            _currentShot.Launch(_moveDirection, HandleShotImpact, HandleShotMissed);
            _currentShot = null;
            _sizeLogic.Take(_balanceConfig.ShotLaunchCost);
            _feedback.PlayShotReleased(_moveDirection);
        }

        private void Charge(float deltaTime)
        {
            float chargeMass = _chargeLogic.Tick(deltaTime);
            float takenMass = _sizeLogic.Take(chargeMass);
            _currentShot.AddMass(takenMass);
            UpdateShotPosition();

            if (_sizeLogic.Mass < _balanceConfig.MinMass + _balanceConfig.ShotLaunchCost)
            {
                _chargeLogic.Stop();
                _shotsPool.ReturnToPool(_currentShot);
                _currentShot = null;
                OnMassDepleted?.Invoke();
            }
        }

        private void UpdateShotPosition()
        {
            float shotRadius = _currentShot.Radius;
            _currentShot.transform.position = transform.position
                                              + _moveDirection * (_sizeLogic.Radius + shotRadius)
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

        private void HandleMassChanged(float mass)
        {
            OnMassChanged?.Invoke(mass);
        }

        private void HandleHopLanded()
        {
            _feedback.PlayLanded();
        }

        private void HandleMovementFinished()
        {
            OnMovementFinished?.Invoke();
        }

        private float HorizontalDistanceToTarget()
        {
            Vector3 toTarget = _targetPosition - transform.position;
            toTarget.y = 0f;
            return toTarget.magnitude;
        }

        private void FinishShot(ShotProjectile shot)
        {
            _shotsPool.ReturnToPool(shot);
            _shotsInFlight--;
            OnShotFinished?.Invoke();
        }
    }
}
