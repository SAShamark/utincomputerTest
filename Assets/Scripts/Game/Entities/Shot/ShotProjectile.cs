using System;
using UnityEngine;

namespace Game.Entities.Shot
{
    public class ShotProjectile : MonoBehaviour
    {
        [SerializeField] private LayerMask _obstaclesMask;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _chargePulseSpeed = 18f;
        [SerializeField] private float _chargePulseAmplitude = 0.08f;

        private readonly Collider[] _overlapBuffer = new Collider[1];

        private BalanceConfig _balanceConfig;
        private Action<ShotProjectile, Vector3> _onImpact;
        private Action<ShotProjectile> _onMissed;
        private Vector3 _direction;
        private float _travelledDistance;
        private bool _isFlying;
        private bool _isCharging;

        public float Mass { get; private set; }
        public float Radius => _balanceConfig.MassToRadius(Mass);

        public void Prepare(BalanceConfig balanceConfig)
        {
            _balanceConfig = balanceConfig;
            _isFlying = false;
            _isCharging = true;
            _trail.emitting = false;
            _trail.Clear();
            SetMass(0f);
        }

        public void AddMass(float mass)
        {
            SetMass(Mass + mass);
        }

        public void Launch(Vector3 direction, Action<ShotProjectile, Vector3> onImpact, Action<ShotProjectile> onMissed)
        {
            transform.SetParent(null, true);
            _direction = direction;
            _onImpact = onImpact;
            _onMissed = onMissed;
            _travelledDistance = 0f;
            _isCharging = false;
            _isFlying = true;
            ApplyScale(1f);
            _trail.widthMultiplier = Radius * 2f;
            _trail.Clear();
            _trail.emitting = true;
        }

        private void Update()
        {
            if (_isCharging)
            {
                ApplyScale(1f + Mathf.Sin(Time.time * _chargePulseSpeed) * _chargePulseAmplitude);
                return;
            }

            if (!_isFlying)
            {
                return;
            }

            Fly(Time.deltaTime);
        }

        private void Fly(float deltaTime)
        {
            float stepDistance = _balanceConfig.ShotSpeed * deltaTime;

            if (Physics.OverlapSphereNonAlloc(transform.position, Radius, _overlapBuffer, _obstaclesMask,
                    QueryTriggerInteraction.Ignore) > 0)
            {
                _isFlying = false;
                _trail.emitting = false;
                _onImpact.Invoke(this, _overlapBuffer[0].ClosestPoint(transform.position));
                return;
            }

            if (Physics.SphereCast(transform.position, Radius, _direction, out RaycastHit hit, stepDistance,
                    _obstaclesMask, QueryTriggerInteraction.Ignore))
            {
                transform.position += _direction * hit.distance;
                _isFlying = false;
                _trail.emitting = false;
                _onImpact.Invoke(this, hit.point);
                return;
            }

            transform.position += _direction * stepDistance;
            _travelledDistance += stepDistance;

            if (_travelledDistance >= _balanceConfig.ShotMaxDistance)
            {
                _isFlying = false;
                _trail.emitting = false;
                _onMissed.Invoke(this);
            }
        }

        private void SetMass(float mass)
        {
            Mass = mass;
            ApplyScale(1f);
        }

        private void ApplyScale(float multiplier)
        {
            transform.localScale = Vector3.one * (Radius * 2f * multiplier);
        }
    }
}
