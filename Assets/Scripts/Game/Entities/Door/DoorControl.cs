using System;
using DG.Tweening;
using Game.Entities.Player;
using UnityEngine;

namespace Game.Entities.Door
{
    public class DoorControl : MonoBehaviour
    {
        [SerializeField] private Transform _leftLeaf;
        [SerializeField] private Transform _rightLeaf;
        [SerializeField] private Transform _passPoint;
        [SerializeField] private float _openAngle = 105f;
        [SerializeField] private float _openDuration = 0.45f;
        [SerializeField] private float _closeDuration = 0.3f;

        private BalanceConfig _balanceConfig;
        private PlayerBallControl _playerBallControl;
        private Sequence _sequence;

        public bool IsOpen { get; private set; }
        public Vector3 PassPosition => _passPoint.position;
        
        public void Init(BalanceConfig balanceConfig, PlayerBallControl playerBallControl)
        {
            _balanceConfig = balanceConfig;
            _playerBallControl = playerBallControl;
        }

        private void Update()
        {
            if (IsOpen)
            {
                return;
            }

            CheckPlayerDistance();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        public void Close(Action onClosed)
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_leftLeaf.DOLocalRotate(Vector3.zero, _closeDuration).SetEase(Ease.InQuad))
                .Join(_rightLeaf.DOLocalRotate(Vector3.zero, _closeDuration).SetEase(Ease.InQuad))
                .Append(transform.DOPunchScale(Vector3.one * 0.04f, 0.2f, 6, 0.5f))
                .OnComplete(onClosed.Invoke)
                .SetLink(gameObject);
        }

        private void CheckPlayerDistance()
        {
            Vector3 toPlayer = _playerBallControl.transform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > _balanceConfig.DoorOpenDistance * _balanceConfig.DoorOpenDistance)
            {
                return;
            }

            Open();
        }

        private void Open()
        {
            IsOpen = true;
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_leftLeaf.DOLocalRotate(new Vector3(0f, _openAngle, 0f), _openDuration).SetEase(Ease.OutBack))
                .Join(_rightLeaf.DOLocalRotate(new Vector3(0f, -_openAngle, 0f), _openDuration).SetEase(Ease.OutBack))
                .SetLink(gameObject);
        }
    }
}
