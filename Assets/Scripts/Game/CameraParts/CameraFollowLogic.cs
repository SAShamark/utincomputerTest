using System;
using UnityEngine;

namespace Game.CameraParts
{
    [Serializable]
    public class CameraFollowLogic
    {
        [SerializeField] private float _followDistance = 16f;
        [SerializeField] private float _followSharpness = 3f;

        private Transform _transform;
        private Transform _target;
        private Vector3 _limitPoint;

        public bool IsFollowing => _target != null;

        public void Init(Transform transform)
        {
            _transform = transform;
        }

        public void Start(Transform target, Vector3 limitPoint)
        {
            _target = target;
            _limitPoint = limitPoint;
        }

        public void Tick(float deltaTime)
        {
            Vector3 followPoint = _target.position;
            Vector3 toLimit = _limitPoint - followPoint;
            toLimit.y = 0f;
            Vector3 flatForward = Vector3.ProjectOnPlane(_transform.forward, Vector3.up);

            if (Vector3.Dot(toLimit, flatForward) < 0f)
            {
                followPoint = _limitPoint;
            }

            Vector3 targetPosition = followPoint - _transform.forward * _followDistance;
            float blend = 1f - Mathf.Exp(-_followSharpness * deltaTime);
            _transform.position = Vector3.Lerp(_transform.position, targetPosition, blend);
        }
    }
}
