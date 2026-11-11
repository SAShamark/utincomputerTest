using System;
using UnityEngine;

namespace Game.Entities.Player.Parts
{
    public class BallSizeLogic
    {
        private BalanceConfig _balanceConfig;
        private Transform _view;

        public float Mass { get; private set; }
        public float Radius => _balanceConfig.MassToRadius(Mass);

        public event Action<float> OnMassChanged;

        public void Init(BalanceConfig balanceConfig, Transform view)
        {
            _balanceConfig = balanceConfig;
            _view = view;
            SetMass(_balanceConfig.StartMass);
        }

        public float Take(float amount)
        {
            float takenMass = Mathf.Min(amount, Mass);
            SetMass(Mass - takenMass);
            return takenMass;
        }

        private void SetMass(float mass)
        {
            Mass = mass;
            float radius = Radius;
            _view.localScale = Vector3.one * (radius * 2f);
            _view.localPosition = Vector3.up * radius;
            OnMassChanged?.Invoke(Mass);
        }
    }
}
