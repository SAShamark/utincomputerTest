using System;
using DG.Tweening;
using UnityEngine;

namespace Game.CameraParts
{
    [Serializable]
    public class CameraImpactLogic
    {
        [SerializeField] private float _zoomPunchPerRadius = 4f;
        [SerializeField] private float _zoomPunchInDuration = 0.06f;
        [SerializeField] private float _zoomPunchOutDuration = 0.25f;

        private const int SHAKE_VIBRATO = 14;
        private const float SHAKE_RANDOMNESS = 90f;

        private Transform _transform;
        private Camera _camera;
        private float _baseFieldOfView;
        private Tween _shakeTween;
        private Tween _zoomTween;

        public void Init(Transform transform, Camera camera)
        {
            _transform = transform;
            _camera = camera;
            _baseFieldOfView = _camera.fieldOfView;
        }

        public void Shake(Vector3 basePosition, float strength, float duration)
        {
            _shakeTween?.Kill();
            _transform.position = basePosition;
            _shakeTween = _transform.DOShakePosition(duration, strength, SHAKE_VIBRATO, SHAKE_RANDOMNESS, false, true)
                .OnComplete(() => _transform.position = basePosition)
                .SetLink(_transform.gameObject);
        }

        public void PunchZoom(float shotRadius)
        {
            _zoomTween?.Kill();
            _camera.fieldOfView = _baseFieldOfView;
            _zoomTween = DOTween.Sequence()
                .Append(_camera.DOFieldOfView(_baseFieldOfView - shotRadius * _zoomPunchPerRadius, _zoomPunchInDuration)
                    .SetEase(Ease.OutQuad))
                .Append(_camera.DOFieldOfView(_baseFieldOfView, _zoomPunchOutDuration).SetEase(Ease.OutBack))
                .SetLink(_transform.gameObject);
        }

        public void StopShake()
        {
            _shakeTween?.Kill();
        }
    }
}
