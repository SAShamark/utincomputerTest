using Game.Entities.Player;
using UnityEngine;

namespace Game.Entities.Shot
{
    [RequireComponent(typeof(LineRenderer))]
    public class InfectionPreview : MonoBehaviour
    {
        [SerializeField] private LayerMask _obstaclesMask;
        [SerializeField] private int _segments = 64;
        [SerializeField] private float _heightOffset = 0.05f;
        [SerializeField] private float _pulseSpeed = 8f;
        [SerializeField] private float _pulseWidthAmplitude = 0.3f;

        private BalanceConfig _balanceConfig;
        private PlayerBallControl _playerBallControl;
        private LineRenderer _lineRenderer;
        private float _baseWidth;

        public void Init(BalanceConfig balanceConfig, PlayerBallControl playerBallControl)
        {
            _balanceConfig = balanceConfig;
            _playerBallControl = playerBallControl;
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.loop = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = _segments;
            _baseWidth = _lineRenderer.widthMultiplier;
            _lineRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            ShotProjectile shot = _playerBallControl.ChargingShot;
            if (shot == null || !TryGetImpactPoint(shot, out Vector3 impactPoint))
            {
                _lineRenderer.enabled = false;
                return;
            }

            DrawCircle(impactPoint, shot.Radius * _balanceConfig.InfectionMultiplier);
        }

        private bool TryGetImpactPoint(ShotProjectile shot, out Vector3 impactPoint)
        {
            if (Physics.SphereCast(shot.transform.position, shot.Radius, _playerBallControl.MoveDirection,
                    out RaycastHit hit, _balanceConfig.ShotMaxDistance, _obstaclesMask,
                    QueryTriggerInteraction.Ignore))
            {
                impactPoint = hit.point;
                return true;
            }

            impactPoint = Vector3.zero;
            return false;
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            _lineRenderer.enabled = true;
            _lineRenderer.widthMultiplier =
                _baseWidth * (1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseWidthAmplitude);
            center.y = _heightOffset;

            for (int i = 0; i < _segments; i++)
            {
                float angle = i * Mathf.PI * 2f / _segments;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                _lineRenderer.SetPosition(i, center + offset);
            }
        }
    }
}
