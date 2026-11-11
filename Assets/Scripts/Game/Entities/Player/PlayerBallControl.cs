using System;
using Game.Controls;
using Game.Entities.Player.Parts;
using Game.Entities.Shot;
using UnityEngine;

namespace Game.Entities.Player
{
    public class PlayerBallControl : MonoBehaviour
    {
        [SerializeField] private Transform _view;
        [SerializeField] private ShootingLogic _shootingLogic;
        [SerializeField] private PlayerBallFeedback _feedback;

        private readonly BallSizeLogic _sizeLogic = new();
        private readonly HopMovementLogic _hopMovementLogic = new();

        private BalanceConfig _balanceConfig;
        private TapInput _tapInput;
        private Vector3 _moveDirection;
        private Vector3 _targetPosition;
        private bool _isGameplayActive;

        public float Mass => _sizeLogic.Mass;
        public float Radius => _sizeLogic.Radius;
        public Vector3 MoveDirection => _moveDirection;
        public bool CanShoot => _sizeLogic.Mass - _balanceConfig.MinMass >=
                                _balanceConfig.MinShotMass + _balanceConfig.ShotLaunchCost;
        public bool IsBusy => _shootingLogic.IsCharging || _shootingLogic.HasShotsInFlight || _hopMovementLogic.IsMoving;
        public float DistanceToTarget => HorizontalDistanceToTarget();
        public ShotProjectile ChargingShot => _shootingLogic.ChargingShot;

        public event Action OnMassDepleted;
        public event Action<float> OnMassChanged;
        public event Action<ShotProjectile, Vector3> OnShotImpact;
        public event Action OnShotFinished;
        public event Action OnMovementFinished;

        public void Init(BalanceConfig balanceConfig, TapInput tapInput, Vector3 targetPosition)
        {
            _balanceConfig = balanceConfig;
            _tapInput = tapInput;
            FaceTarget(targetPosition);
            Subscribe();
            InitParts();
            _isGameplayActive = true;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _feedback.Kill();
        }

        private void Update()
        {
            if (!_isGameplayActive)
            {
                return;
            }

            _hopMovementLogic.Tick(Time.deltaTime);
            _shootingLogic.Tick(Time.deltaTime);
        }

        public void MoveForward(float distance)
        {
            _hopMovementLogic.Move(distance);
        }

        public void StopGameplay()
        {
            _isGameplayActive = false;
            _shootingLogic.Stop();
            _hopMovementLogic.Stop();
            _feedback.PlayChargeCancelled();
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            _moveDirection = toTarget.normalized;
            transform.rotation = Quaternion.LookRotation(_moveDirection);
        }

        private void InitParts()
        {
            _sizeLogic.Init(_balanceConfig, _view);
            _shootingLogic.Init(_balanceConfig, _sizeLogic, transform, _moveDirection);
            _hopMovementLogic.Init(_balanceConfig, transform, _moveDirection);
            _feedback.Init(transform);
        }

        private void Subscribe()
        {
            _sizeLogic.OnMassChanged += HandleMassChanged;
            _shootingLogic.OnMassDepleted += HandleMassDepleted;
            _shootingLogic.OnShotImpact += HandleShotImpact;
            _shootingLogic.OnShotFinished += HandleShotFinished;
            _hopMovementLogic.OnHopLanded += HandleHopLanded;
            _hopMovementLogic.OnMovementFinished += HandleMovementFinished;
            _tapInput.OnPressed += HandlePressed;
            _tapInput.OnReleased += HandleReleased;
        }

        private void Unsubscribe()
        {
            _sizeLogic.OnMassChanged -= HandleMassChanged;
            _shootingLogic.OnMassDepleted -= HandleMassDepleted;
            _shootingLogic.OnShotImpact -= HandleShotImpact;
            _shootingLogic.OnShotFinished -= HandleShotFinished;
            _hopMovementLogic.OnHopLanded -= HandleHopLanded;
            _hopMovementLogic.OnMovementFinished -= HandleMovementFinished;
            _tapInput.OnPressed -= HandlePressed;
            _tapInput.OnReleased -= HandleReleased;
        }

        private void HandlePressed()
        {
            if (!_isGameplayActive || _shootingLogic.IsCharging || _hopMovementLogic.IsMoving || !CanShoot)
            {
                return;
            }

            _shootingLogic.BeginCharge();
            _feedback.PlayChargeStarted();
        }

        private void HandleReleased()
        {
            if (!_isGameplayActive || !_shootingLogic.IsCharging)
            {
                return;
            }

            _shootingLogic.Release();
            _feedback.PlayShotReleased(_moveDirection);
        }

        private void HandleMassChanged(float mass)
        {
            OnMassChanged?.Invoke(mass);
        }

        private void HandleMassDepleted()
        {
            OnMassDepleted?.Invoke();
        }

        private void HandleShotImpact(ShotProjectile shot, Vector3 impactPoint)
        {
            OnShotImpact?.Invoke(shot, impactPoint);
        }

        private void HandleShotFinished()
        {
            OnShotFinished?.Invoke();
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
    }
}
