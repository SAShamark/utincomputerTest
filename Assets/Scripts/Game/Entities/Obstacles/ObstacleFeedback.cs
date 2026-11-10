using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game.Entities.Obstacles
{
    [Serializable]
    public class ObstacleFeedback
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, ColorUsage(true, true)] private Color _flashColor = new(3f, 1.6f, 0.5f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color _infectedColor = new(1.8f, 0.35f, 0.2f, 1f);
        [SerializeField] private float _flashDuration = 0.05f;
        [SerializeField] private float _settleDuration = 0.12f;
        [SerializeField] private float _infectPunchScale = 0.25f;
        [SerializeField] private float _infectPunchDuration = 0.2f;
        [SerializeField] private float _explodeSwellScale = 1.3f;
        [SerializeField] private float _explodeSwellDuration = 0.06f;
        [SerializeField] private float _explodeShrinkDuration = 0.14f;
        [SerializeField, ColorUsage(true, true)] private Color _highlightColor = new(0.45f, 0.7f, 2f, 1f);
        [SerializeField, Range(0f, 1f)] private float _highlightAmount = 0.35f;
        [SerializeField] private float _highlightPulseDuration = 0.45f;
        [SerializeField] private float _highlightFadeOutDuration = 0.15f;
        [SerializeField] private Vector3 _idleBobScale = new(1.07f, 0.9f, 1.07f);
        [SerializeField] private float _idleBobHalfPeriod = 0.4f;

        private readonly List<MaterialSlot> _slots = new();

        private Transform _visualRoot;
        private Transform _bobRoot;
        private Vector3 _bobDefaultScale;
        private Tween _bobTween;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _defaultScale;
        private Color _overlayColor;
        private float _overlayAmount;
        private Sequence _sequence;
        private Tween _highlightTween;

        public void Init(Transform visualRoot, Transform bobRoot)
        {
            _visualRoot = visualRoot;
            _bobRoot = bobRoot;
            _bobDefaultScale = _bobRoot.localScale;
            _propertyBlock = new MaterialPropertyBlock();
            _defaultScale = _visualRoot.localScale;

            foreach (Renderer renderer in _visualRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].HasProperty(BaseColorId))
                    {
                        _slots.Add(new MaterialSlot(renderer, i, materials[i].GetColor(BaseColorId)));
                    }
                }
            }

            StartIdleBob();
        }

        private void StartIdleBob()
        {
            _bobTween = _bobRoot.DOScale(Vector3.Scale(_bobDefaultScale, _idleBobScale), _idleBobHalfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(UnityEngine.Random.Range(0f, _idleBobHalfPeriod * 2f))
                .SetLink(_bobRoot.gameObject);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            _highlightTween?.Kill();

            if (isHighlighted)
            {
                _overlayColor = _highlightColor;
                _highlightTween = DOTween.To(() => _overlayAmount, SetOverlayAmount, _highlightAmount,
                        _highlightPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(_visualRoot.gameObject);
                return;
            }

            _highlightTween = DOTween.To(() => _overlayAmount, SetOverlayAmount, 0f, _highlightFadeOutDuration)
                .SetLink(_visualRoot.gameObject);
        }

        public void PlayInfected()
        {
            Kill();
            _visualRoot.localScale = _defaultScale;
            _overlayColor = _flashColor;

            _sequence = DOTween.Sequence()
                .Append(DOTween.To(() => _overlayAmount, SetOverlayAmount, 1f, _flashDuration).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _overlayColor, SetOverlayColor, _infectedColor, _settleDuration)
                    .SetEase(Ease.InQuad))
                .Insert(0f, _visualRoot.DOPunchScale(_defaultScale * _infectPunchScale, _infectPunchDuration, 6, 0.6f))
                .SetLink(_visualRoot.gameObject);
        }

        public void PlayExplode(Action onComplete)
        {
            Kill();
            _visualRoot.localScale = _defaultScale;

            _sequence = DOTween.Sequence()
                .Append(_visualRoot.DOScale(_defaultScale * _explodeSwellScale, _explodeSwellDuration)
                    .SetEase(Ease.OutQuad))
                .Join(DOTween.To(() => _overlayColor, SetOverlayColor, _flashColor, _explodeSwellDuration))
                .Append(_visualRoot.DOScale(Vector3.zero, _explodeShrinkDuration).SetEase(Ease.InBack))
                .OnComplete(() => onComplete.Invoke())
                .SetLink(_visualRoot.gameObject);
        }

        public void Kill()
        {
            _bobTween?.Kill();
            _bobTween = null;
            _bobRoot.localScale = _bobDefaultScale;
            _sequence?.Kill();
            _sequence = null;
            _highlightTween?.Kill();
            _highlightTween = null;
        }

        private void SetOverlayAmount(float amount)
        {
            _overlayAmount = amount;
            ApplyColors();
        }

        private void SetOverlayColor(Color color)
        {
            _overlayColor = color;
            ApplyColors();
        }

        private void ApplyColors()
        {
            foreach (MaterialSlot slot in _slots)
            {
                slot.Renderer.GetPropertyBlock(_propertyBlock, slot.MaterialIndex);
                _propertyBlock.SetColor(BaseColorId, Color.Lerp(slot.DefaultColor, _overlayColor, _overlayAmount));
                slot.Renderer.SetPropertyBlock(_propertyBlock, slot.MaterialIndex);
            }
        }

        private readonly struct MaterialSlot
        {
            public readonly Renderer Renderer;
            public readonly int MaterialIndex;
            public readonly Color DefaultColor;

            public MaterialSlot(Renderer renderer, int materialIndex, Color defaultColor)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
                DefaultColor = defaultColor;
            }
        }
    }
}
