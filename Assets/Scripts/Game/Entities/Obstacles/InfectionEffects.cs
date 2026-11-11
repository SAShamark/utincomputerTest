using System;
using DG.Tweening;
using Services.ObjectPool;
using UnityEngine;

namespace Game.Entities.Obstacles
{
    [Serializable]
    public class InfectionEffects
    {
        [SerializeField] private ParticleSystem _impactEffectPrefab;
        [SerializeField] private ParticleSystem _explosionEffectPrefab;
        [SerializeField] private Transform _effectsContainer;
        [SerializeField] private float _effectLifetime = 1.5f;
        [SerializeField] private float _explosionEffectHeight = 0.6f;
        [SerializeField] private float _shakeStrengthPerRadius = 0.3f;
        [SerializeField] private float _shakeDuration = 0.25f;

        private const int IMPACT_EFFECTS_POOL_SIZE = 2;
        private const int EXPLOSION_EFFECTS_POOL_SIZE = 24;

        private CameraControl _cameraControl;
        private GameObject _linkTarget;
        private ObjectPool<ParticleSystem> _impactEffectsPool;
        private ObjectPool<ParticleSystem> _explosionEffectsPool;

        public void Init(CameraControl cameraControl, GameObject linkTarget)
        {
            _cameraControl = cameraControl;
            _linkTarget = linkTarget;
            _impactEffectsPool = new ObjectPool<ParticleSystem>(_impactEffectPrefab, IMPACT_EFFECTS_POOL_SIZE,
                _effectsContainer);
            _explosionEffectsPool = new ObjectPool<ParticleSystem>(_explosionEffectPrefab,
                EXPLOSION_EFFECTS_POOL_SIZE, _effectsContainer);
        }

        public void PlayImpact(Vector3 impactPoint, float shotRadius)
        {
            PlayEffect(_impactEffectsPool, impactPoint);
            _cameraControl.Shake(shotRadius * _shakeStrengthPerRadius, _shakeDuration);
            _cameraControl.PunchZoom(shotRadius);
        }

        public void PlayExplosion(Vector3 obstaclePosition)
        {
            PlayEffect(_explosionEffectsPool, obstaclePosition + Vector3.up * _explosionEffectHeight);
        }

        private void PlayEffect(ObjectPool<ParticleSystem> pool, Vector3 position)
        {
            ParticleSystem effect = pool.GetFreeElement();
            effect.transform.position = position;
            effect.Clear(true);
            effect.Play(true);
            DOVirtual.DelayedCall(_effectLifetime, () => pool.ReturnToPool(effect)).SetLink(_linkTarget);
        }
    }
}
