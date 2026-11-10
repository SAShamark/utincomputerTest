namespace Game.Entities.Player.Parts
{
    public class ChargeLogic
    {
        private BalanceConfig _balanceConfig;
        private float _holdTime;

        public bool IsCharging { get; private set; }

        public void Init(BalanceConfig balanceConfig)
        {
            _balanceConfig = balanceConfig;
        }

        public void Begin()
        {
            _holdTime = 0f;
            IsCharging = true;
        }

        public float Tick(float deltaTime)
        {
            if (!IsCharging)
            {
                return 0f;
            }

            _holdTime += deltaTime;
            float chargeRate = _balanceConfig.BaseChargeRate + _balanceConfig.ChargeAcceleration * _holdTime;
            return chargeRate * deltaTime;
        }

        public void Stop()
        {
            IsCharging = false;
        }
    }
}
