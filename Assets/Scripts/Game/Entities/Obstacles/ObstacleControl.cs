using System;
using UnityEngine;

namespace Game.Entities.Obstacles
{
    public class ObstacleControl : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _bobRoot;
        [SerializeField] private Collider _collider;
        [SerializeField] private ObstacleFeedback _feedback;

        private float _explodeTime;
        private bool _isHighlighted;

        public bool IsInfected { get; private set; }
        public bool IsExploded { get; private set; }
        public float Energy { get; private set; }

        public event Action<ObstacleControl> OnExploded;

        private void Awake()
        {
            _feedback.Init(_visualRoot, _bobRoot);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            if (IsInfected || _isHighlighted == isHighlighted)
            {
                return;
            }

            _isHighlighted = isHighlighted;
            _feedback.SetHighlighted(isHighlighted);
        }

        public bool TryInfect(float energy, float explodeDelay)
        {
            if (IsExploded || (IsInfected && Energy >= energy))
            {
                return false;
            }

            if (!IsInfected)
            {
                IsInfected = true;
                _explodeTime = Time.time + explodeDelay;
                _feedback.PlayInfected();
            }

            Energy = energy;
            return true;
        }

        private void Update()
        {
            if (!IsInfected || IsExploded || Time.time < _explodeTime)
            {
                return;
            }

            Explode();
        }

        private void OnDestroy()
        {
            _feedback.Kill();
        }

        private void Explode()
        {
            IsExploded = true;
            _collider.enabled = false;
            OnExploded?.Invoke(this);
            _feedback.PlayExplode(HandleExplodeAnimationCompleted);
        }

        private void HandleExplodeAnimationCompleted()
        {
            gameObject.SetActive(false);
        }
    }
}
