using DG.Tweening;
using Game;
using Game.Entities.Obstacles;
using Game.Entities.Player;
using Services;
using TMPro;
using UI.Screens.Base;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens.Variants.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private TMP_Text _sizeText;
        [SerializeField] private Image _sizeFill;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _levelProgressFill;
        [SerializeField] private float _levelProgressTweenDuration = 0.25f;

        private const string LEVEL_TEXT_FORMAT = "LEVEL {0}";

        private BalanceConfig _balanceConfig;
        private PlayerBallControl _playerBallControl;
        private InfectionController _infectionController;
        private int _totalObstacles;
        private int _destroyedObstacles;
        private Tween _levelProgressTween;

        public void Bind(BalanceConfig balanceConfig, PlayerBallControl playerBallControl)
        {
            UnbindPlayer();
            _balanceConfig = balanceConfig;
            _playerBallControl = playerBallControl;
            _playerBallControl.OnMassChanged += HandleMassChanged;
            HandleMassChanged(_playerBallControl.Mass);
        }

        public void BindLevelProgress(int levelNumber, InfectionController infectionController, int totalObstacles)
        {
            UnbindLevelProgress();
            _infectionController = infectionController;
            _totalObstacles = Mathf.Max(1, totalObstacles);
            _destroyedObstacles = 0;
            _levelText.text = string.Format(LEVEL_TEXT_FORMAT, levelNumber);
            _levelProgressFill.fillAmount = 0f;
            _infectionController.OnObstacleExploded += HandleObstacleExploded;
        }

        private void OnDestroy()
        {
            UnbindPlayer();
            UnbindLevelProgress();
            _levelProgressTween?.Kill();
        }

        private void UnbindPlayer()
        {
            if (_playerBallControl == null)
            {
                return;
            }

            _playerBallControl.OnMassChanged -= HandleMassChanged;
            _playerBallControl = null;
        }

        private void UnbindLevelProgress()
        {
            if (_infectionController == null)
            {
                return;
            }

            _infectionController.OnObstacleExploded -= HandleObstacleExploded;
            _infectionController = null;
        }

        private void HandleMassChanged(float mass)
        {
            float normalizedSize = Mathf.InverseLerp(_balanceConfig.MinMass, _balanceConfig.StartMass, mass);
            _sizeFill.fillAmount = normalizedSize;
            _sizeText.text = Mathf.RoundToInt(normalizedSize * ValueConstants.PERCENTAGE) + "%";
        }

        private void HandleObstacleExploded()
        {
            _destroyedObstacles++;
            float progress = Mathf.Clamp01((float)_destroyedObstacles / _totalObstacles);
            _levelProgressTween?.Kill();
            _levelProgressTween = _levelProgressFill.DOFillAmount(progress, _levelProgressTweenDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
    }
}
