using System.Collections.Generic;
using Game.Entities.Obstacles;
using Game.Entities.Player;
using UnityEngine;

namespace Game.Entities.Path
{
    public class PathChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _obstaclesMask;
        [SerializeField] private float _blockersCheckHalfHeight = 1f;

        private const int OVERLAP_BUFFER_SIZE = 128;

        private readonly Collider[] _overlapBuffer = new Collider[OVERLAP_BUFFER_SIZE];

        private BalanceConfig _balanceConfig;
        private PlayerBallControl _playerBallControl;

        public void Init(BalanceConfig balanceConfig, PlayerBallControl playerBallControl)
        {
            _balanceConfig = balanceConfig;
            _playerBallControl = playerBallControl;
        }

        private float CorridorHalfWidth => _playerBallControl.Radius * _balanceConfig.PathWidthTolerance;

        public bool IsPathClear(float distance)
        {
            float castRadius = CorridorHalfWidth;
            Vector3 origin = _playerBallControl.transform.position;
            origin.y = _playerBallControl.Radius;

            if (Physics.CheckSphere(origin, castRadius, _obstaclesMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return !Physics.SphereCast(origin, castRadius, _playerBallControl.MoveDirection, out _, distance,
                _obstaclesMask, QueryTriggerInteraction.Ignore);
        }

        public void CollectBlockers(float distance, List<ObstacleControl> blockers)
        {
            blockers.Clear();

            float halfWidth = CorridorHalfWidth;
            Vector3 direction = _playerBallControl.MoveDirection;
            Vector3 center = _playerBallControl.transform.position + direction * (distance * 0.5f);
            center.y = _blockersCheckHalfHeight;
            Vector3 halfExtents = new Vector3(halfWidth, _blockersCheckHalfHeight, distance * 0.5f);

            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapBuffer,
                Quaternion.LookRotation(direction), _obstaclesMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (_overlapBuffer[i].TryGetComponent(out ObstacleControl obstacle))
                {
                    blockers.Add(obstacle);
                }
            }
        }
    }
}
