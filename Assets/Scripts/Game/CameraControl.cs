using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] private float _sideMargin = 2.6f;
        [SerializeField] private float _startBackMargin = 1.2f;
        [SerializeField] private float _endHeight = 3.8f;
        [SerializeField, Range(0f, 0.4f)] private float _topPadding = 0.12f;
        [SerializeField, Range(0f, 0.4f)] private float _bottomPadding = 0.08f;
        [SerializeField, Range(0f, 0.4f)] private float _sidePadding = 0.03f;
        [SerializeField] private float _followDistance = 16f;
        [SerializeField] private float _followSharpness = 3f;
        [SerializeField] private float _zoomPunchPerRadius = 4f;
        [SerializeField] private float _zoomPunchInDuration = 0.06f;
        [SerializeField] private float _zoomPunchOutDuration = 0.25f;

        private const int CENTERING_ROUNDS = 4;
        private const int SHAKE_VIBRATO = 14;
        private const float SHAKE_RANDOMNESS = 90f;
        private const int DISTANCE_SEARCH_STEPS = 24;
        private const float MIN_DISTANCE = 1f;
        private const float MAX_DISTANCE = 300f;

        private readonly List<Vector3> _framingPoints = new();

        private Camera _camera;
        private float _fittedAspect;
        private Vector3 _basePosition;
        private Tween _shakeTween;
        private Tween _zoomTween;
        private Transform _followTarget;
        private Vector3 _followLimit;
        private float _baseFieldOfView;

        public void Init(Vector3 startPosition, Vector3 endPosition)
        {
            _camera = GetComponent<Camera>();
            _baseFieldOfView = _camera.fieldOfView;
            BuildFramingPoints(startPosition, endPosition);
            Fit();
        }

        public void Shake(float strength, float duration)
        {
            _shakeTween?.Kill();
            transform.position = _basePosition;
            _shakeTween = transform.DOShakePosition(duration, strength, SHAKE_VIBRATO, SHAKE_RANDOMNESS, false, true)
                .OnComplete(() => transform.position = _basePosition)
                .SetLink(gameObject);
        }

        public void StartFollow(Transform target, Vector3 limitPoint)
        {
            _shakeTween?.Kill();
            _followTarget = target;
            _followLimit = limitPoint;
        }

        public void PunchZoom(float shotRadius)
        {
            _zoomTween?.Kill();
            _camera.fieldOfView = _baseFieldOfView;
            _zoomTween = DOTween.Sequence()
                .Append(_camera.DOFieldOfView(_baseFieldOfView - shotRadius * _zoomPunchPerRadius, _zoomPunchInDuration)
                    .SetEase(Ease.OutQuad))
                .Append(_camera.DOFieldOfView(_baseFieldOfView, _zoomPunchOutDuration).SetEase(Ease.OutBack))
                .SetLink(gameObject);
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            if (_followTarget != null)
            {
                Follow(Time.deltaTime);
                return;
            }

            if (!Mathf.Approximately(_fittedAspect, _camera.aspect))
            {
                Fit();
            }
        }

        private void Follow(float deltaTime)
        {
            Vector3 followPoint = _followTarget.position;
            Vector3 toLimit = _followLimit - followPoint;
            toLimit.y = 0f;
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            if (Vector3.Dot(toLimit, flatForward) < 0f)
            {
                followPoint = _followLimit;
            }

            Vector3 targetPosition = followPoint - transform.forward * _followDistance;
            float blend = 1f - Mathf.Exp(-_followSharpness * deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, blend);
        }

        private void BuildFramingPoints(Vector3 startPosition, Vector3 endPosition)
        {
            startPosition.y = 0f;
            endPosition.y = 0f;

            Vector3 direction = (endPosition - startPosition).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, direction) * _sideMargin;
            Vector3 start = startPosition - direction * _startBackMargin;

            _framingPoints.Clear();
            _framingPoints.Add(start - side);
            _framingPoints.Add(start + side);
            _framingPoints.Add(endPosition - side);
            _framingPoints.Add(endPosition + side);
            _framingPoints.Add(endPosition - side + Vector3.up * _endHeight);
            _framingPoints.Add(endPosition + side + Vector3.up * _endHeight);
        }

        private void Fit()
        {
            _shakeTween?.Kill();
            _fittedAspect = _camera.aspect;

            Vector3 forward = transform.forward;
            Vector3 center = GetPointsCenter();
            float distance = MAX_DISTANCE;

            for (int round = 0; round < CENTERING_ROUNDS; round++)
            {
                distance = FindFitDistance(center, forward);
                transform.position = center - forward * distance;
                center += GetCenteringOffset(distance);
            }

            transform.position = center - forward * FindFitDistance(center, forward);
            _basePosition = transform.position;
        }

        private float FindFitDistance(Vector3 center, Vector3 forward)
        {
            float minDistance = MIN_DISTANCE;
            float maxDistance = MAX_DISTANCE;

            for (int i = 0; i < DISTANCE_SEARCH_STEPS; i++)
            {
                float distance = (minDistance + maxDistance) * 0.5f;
                transform.position = center - forward * distance;

                if (AreAllPointsVisible())
                {
                    maxDistance = distance;
                }
                else
                {
                    minDistance = distance;
                }
            }

            return maxDistance;
        }

        private bool AreAllPointsVisible()
        {
            foreach (Vector3 point in _framingPoints)
            {
                Vector3 viewportPoint = _camera.WorldToViewportPoint(point);

                if (viewportPoint.z <= 0f
                    || viewportPoint.x < _sidePadding || viewportPoint.x > 1f - _sidePadding
                    || viewportPoint.y < _bottomPadding || viewportPoint.y > 1f - _topPadding)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 GetCenteringOffset(float distance)
        {
            Vector2 min = Vector2.one * float.MaxValue;
            Vector2 max = Vector2.one * float.MinValue;

            foreach (Vector3 point in _framingPoints)
            {
                Vector3 viewportPoint = _camera.WorldToViewportPoint(point);
                min = Vector2.Min(min, viewportPoint);
                max = Vector2.Max(max, viewportPoint);
            }

            float targetCenterX = 0.5f;
            float targetCenterY = (_bottomPadding + 1f - _topPadding) * 0.5f;
            float frustumHeight = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * _camera.aspect;

            float offsetX = ((min.x + max.x) * 0.5f - targetCenterX) * frustumWidth;
            float offsetY = ((min.y + max.y) * 0.5f - targetCenterY) * frustumHeight;

            return transform.right * offsetX + transform.up * offsetY;
        }

        private Vector3 GetPointsCenter()
        {
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in _framingPoints)
            {
                center += point;
            }

            return center / _framingPoints.Count;
        }
    }
}
