using Game.Entities.Player;
using UnityEngine;

namespace Game.Entities.Path
{
    public class PathView : MonoBehaviour
    {
        [SerializeField] private Transform _surface;
        [SerializeField] private float _thickness = 0.08f;
        [SerializeField] private float _backExtension = 6f;
        [SerializeField] private float _widthPadding = 0.15f;

        private PlayerBallControl _playerBallControl;
        private Vector3 _startPosition;
        private Vector3 _targetPosition;

        public void Init(PlayerBallControl playerBallControl, Vector3 targetPosition)
        {
            _playerBallControl = playerBallControl;
            _targetPosition = targetPosition;
            _targetPosition.y = 0f;
            _startPosition = _playerBallControl.transform.position - _playerBallControl.MoveDirection * _backExtension;
            _startPosition.y = 0f;
            UpdateView();
        }

        private void LateUpdate()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            Vector3 path = _targetPosition - _startPosition;
            float length = path.magnitude;
            Vector3 direction = length > 0f ? path / length : _playerBallControl.MoveDirection;

            _surface.position = (_startPosition + _targetPosition) * 0.5f + Vector3.up * (_thickness * 0.5f);
            _surface.rotation = Quaternion.LookRotation(Vector3.down, direction);
            _surface.localScale = new Vector3(_playerBallControl.Radius * 2f + _widthPadding, length, _thickness);
        }
    }
}
