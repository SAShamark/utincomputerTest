using System;
using DG.Tweening;
using UnityEngine;

namespace Game.Entities.Player.Parts
{
    [Serializable]
    public class PlayerBallFeedback
    {
        [SerializeField] private Vector3 _breathScale = new(1.06f, 0.92f, 1.06f);
        [SerializeField] private float _breathHalfPeriod = 0.12f;
        [SerializeField] private float _recoilDistance = 0.35f;
        [SerializeField] private float _recoilDuration = 0.25f;
        [SerializeField] private Vector3 _recoilSquash = new(0.15f, -0.15f, 0.15f);
        [SerializeField] private Vector3 _landingSquash = new(0.22f, -0.25f, 0.22f);
        [SerializeField] private float _landingDuration = 0.14f;
        [SerializeField] private Transform[] _eyes;
        [SerializeField] private float _blinkInterval = 2.6f;
        [SerializeField] private float _blinkCloseDuration = 0.06f;
        [SerializeField] private float _blinkOpenDuration = 0.09f;
        [SerializeField] private float _blinkClosedScale = 0.1f;

        private const int PUNCH_VIBRATO = 6;
        private const float PUNCH_ELASTICITY = 0.6f;

        private Transform _root;
        private Tween _scaleTween;
        private Tween _positionTween;
        private Sequence _blinkSequence;

        public void Init(Transform root)
        {
            _root = root;
            StartBlinking();
        }

        public void PlayChargeStarted()
        {
            KillScale();
            _scaleTween = _root.DOScale(_breathScale, _breathHalfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(_root.gameObject);
        }

        public void PlayShotReleased(Vector3 shotDirection)
        {
            KillScale();
            _scaleTween = _root.DOPunchScale(_recoilSquash, _recoilDuration, PUNCH_VIBRATO, PUNCH_ELASTICITY)
                .SetLink(_root.gameObject);

            _positionTween?.Kill(true);
            _positionTween = _root.DOPunchPosition(-shotDirection * _recoilDistance, _recoilDuration,
                    PUNCH_VIBRATO, PUNCH_ELASTICITY)
                .SetLink(_root.gameObject);
        }

        public void PlayChargeCancelled()
        {
            KillScale();
        }

        public void PlayLanded()
        {
            KillScale();
            _scaleTween = _root.DOPunchScale(_landingSquash, _landingDuration, PUNCH_VIBRATO, PUNCH_ELASTICITY)
                .SetLink(_root.gameObject);
        }

        public void Kill()
        {
            KillScale();
            _positionTween?.Kill();
            _blinkSequence?.Kill();
        }

        private void StartBlinking()
        {
            _blinkSequence = DOTween.Sequence().AppendInterval(_blinkInterval);

            foreach (Transform eye in _eyes)
            {
                Vector3 openScale = eye.localScale;
                Vector3 closedScale = new Vector3(openScale.x, openScale.y * _blinkClosedScale, openScale.z);
                _blinkSequence.Insert(_blinkInterval, eye.DOScale(closedScale, _blinkCloseDuration));
                _blinkSequence.Insert(_blinkInterval + _blinkCloseDuration, eye.DOScale(openScale, _blinkOpenDuration));
            }

            _blinkSequence.SetLoops(-1).SetLink(_root.gameObject);
        }

        private void KillScale()
        {
            _scaleTween?.Kill();
            _root.localScale = Vector3.one;
        }
    }
}
