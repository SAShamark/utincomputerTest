using System;
using Services;
using UnityEngine;

namespace Game.Entities.Player.Parts
{
    public class HopMovementLogic
    {
        private const float MIN_HOP_DURATION = 0.08f;

        private BalanceConfig _balanceConfig;
        private Transform _root;
        private Vector3 _direction;
        private Vector3 _hopStart;
        private Vector3 _hopEnd;
        private float _hopDuration;
        private float _hopHeight;
        private float _hopProgress;
        private float _remainingDistance;

        public bool IsMoving { get; private set; }

        public event Action OnHopLanded;
        public event Action OnMovementFinished;

        public void Init(BalanceConfig balanceConfig, Transform root, Vector3 direction)
        {
            _balanceConfig = balanceConfig;
            _root = root;
            _direction = direction;
        }

        public void Move(float distance)
        {
            if (distance <= ValueConstants.EPSILON)
            {
                return;
            }

            _remainingDistance = distance;
            IsMoving = true;
            StartNextHop();
        }

        public void Tick(float deltaTime)
        {
            if (!IsMoving)
            {
                return;
            }

            _hopProgress += deltaTime / _hopDuration;
            float progress = Mathf.Clamp01(_hopProgress);

            Vector3 position = Vector3.Lerp(_hopStart, _hopEnd, progress);
            position.y = Mathf.Sin(progress * Mathf.PI) * _hopHeight;
            _root.position = position;

            if (progress < 1f)
            {
                return;
            }

            OnHopLanded?.Invoke();

            if (_remainingDistance > ValueConstants.EPSILON)
            {
                StartNextHop();
                return;
            }

            IsMoving = false;
            OnMovementFinished?.Invoke();
        }

        public void Stop()
        {
            IsMoving = false;
        }

        private void StartNextHop()
        {
            float hopLength = Mathf.Min(_balanceConfig.HopLength, _remainingDistance);
            float hopRatio = hopLength / _balanceConfig.HopLength;
            _remainingDistance -= hopLength;

            _hopStart = _root.position;
            _hopStart.y = 0f;
            _hopEnd = _hopStart + _direction * hopLength;
            _hopDuration = Mathf.Max(MIN_HOP_DURATION, _balanceConfig.HopDuration * hopRatio);
            _hopHeight = _balanceConfig.HopHeight * hopRatio;
            _hopProgress = 0f;
        }
    }
}
