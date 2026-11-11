using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraParts
{
    [Serializable]
    public class CameraFitLogic
    {
        [SerializeField] private float _sideMargin = 2.6f;
        [SerializeField] private float _startBackMargin = 1.2f;
        [SerializeField] private float _endHeight = 3.8f;
        [SerializeField, Range(0f, 0.4f)] private float _topPadding = 0.12f;
        [SerializeField, Range(0f, 0.4f)] private float _bottomPadding = 0.08f;
        [SerializeField, Range(0f, 0.4f)] private float _sidePadding = 0.03f;

        private const int CENTERING_ROUNDS = 4;
        private const int DISTANCE_SEARCH_STEPS = 24;
        private const float MIN_DISTANCE = 1f;
        private const float MAX_DISTANCE = 300f;

        private readonly List<Vector3> _framingPoints = new();

        private Transform _transform;
        private Camera _camera;
        private float _fittedAspect;

        public Vector3 BasePosition { get; private set; }
        public bool IsAspectChanged => !Mathf.Approximately(_fittedAspect, _camera.aspect);

        public void Init(Transform transform, Camera camera, Vector3 startPosition, Vector3 endPosition)
        {
            _transform = transform;
            _camera = camera;
            BuildFramingPoints(startPosition, endPosition);
        }

        public void Fit()
        {
            _fittedAspect = _camera.aspect;

            Vector3 forward = _transform.forward;
            Vector3 center = GetPointsCenter();

            for (int round = 0; round < CENTERING_ROUNDS; round++)
            {
                float distance = FindFitDistance(center, forward);
                _transform.position = center - forward * distance;
                center += GetCenteringOffset(distance);
            }

            _transform.position = center - forward * FindFitDistance(center, forward);
            BasePosition = _transform.position;
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

        private float FindFitDistance(Vector3 center, Vector3 forward)
        {
            float minDistance = MIN_DISTANCE;
            float maxDistance = MAX_DISTANCE;

            for (int i = 0; i < DISTANCE_SEARCH_STEPS; i++)
            {
                float distance = (minDistance + maxDistance) * 0.5f;
                _transform.position = center - forward * distance;

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

            return _transform.right * offsetX + _transform.up * offsetY;
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
