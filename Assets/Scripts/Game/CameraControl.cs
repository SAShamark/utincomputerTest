using Game.CameraParts;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] private CameraFitLogic _fitLogic;
        [SerializeField] private CameraImpactLogic _impactLogic;
        [SerializeField] private CameraFollowLogic _followLogic;

        private Camera _camera;

        public void Init(Vector3 startPosition, Vector3 endPosition)
        {
            _camera = GetComponent<Camera>();
            _fitLogic.Init(transform, _camera, startPosition, endPosition);
            _impactLogic.Init(transform, _camera);
            _followLogic.Init(transform);
            _fitLogic.Fit();
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            if (_followLogic.IsFollowing)
            {
                _followLogic.Tick(Time.deltaTime);
                return;
            }

            if (_fitLogic.IsAspectChanged)
            {
                _impactLogic.StopShake();
                _fitLogic.Fit();
            }
        }

        public void Shake(float strength, float duration)
        {
            _impactLogic.Shake(_fitLogic.BasePosition, strength, duration);
        }

        public void PunchZoom(float shotRadius)
        {
            _impactLogic.PunchZoom(shotRadius);
        }

        public void StartFollow(Transform target, Vector3 limitPoint)
        {
            _impactLogic.StopShake();
            _followLogic.Start(target, limitPoint);
        }
    }
}
