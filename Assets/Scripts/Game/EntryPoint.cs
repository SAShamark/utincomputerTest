using Game.Controls;
using Game.Entities.Door;
using Game.Entities.Obstacles;
using Game.Entities.Path;
using Game.Entities.Player;
using Game.Entities.Shot;
using UI.Managers;
using UI.Screens;
using UI.Screens.Variants.Gameplay;
using UnityEngine;

namespace Game
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private BalanceConfig _balanceConfig;
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private TapInput _tapInput;
        [SerializeField] private PlayerBallControl _playerBallControl;
        [SerializeField] private InfectionController _infectionController;
        [SerializeField] private PathChecker _pathChecker;
        [SerializeField] private PathView _pathView;
        [SerializeField] private InfectionPreview _infectionPreview;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private DoorControl _door;

        private UIManager _uiManager;
        private GameplayScreen _gameplayScreen;
        private GameplayController _gameplayController;

        private void Start()
        {
            InitializePlayer();
            InitializePath();
            InitializeObstacles();
            InitializeGameplayScreen();
            InitializeGameplay();
        }

        private void InitializePlayer()
        {
            _playerBallControl.Init(_balanceConfig, _tapInput, _door.PassPosition);
            _cameraControl.Init(_playerBallControl.transform.position, _door.transform.position);
        }

        private void InitializePath()
        {
            _pathChecker.Init(_balanceConfig, _playerBallControl);
            _pathView.Init(_playerBallControl, _door.PassPosition);
            _infectionPreview.Init(_balanceConfig, _playerBallControl);
        }

        private void InitializeObstacles()
        {
            _infectionController.Init(_balanceConfig, _playerBallControl, _cameraControl);
            _door.Init(_balanceConfig, _playerBallControl);
        }

        private void InitializeGameplayScreen()
        {
            _uiManager = UIManager.Instance;
            _uiManager.Init();
            _uiManager.ScreensManager.ShowScreen(ScreenTypes.Gameplay);
            _gameplayScreen = _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay) as GameplayScreen;
            _gameplayScreen.Bind(_balanceConfig, _playerBallControl);
            _gameplayScreen.BindLevelProgress(_levelConfig.LevelNumber, _infectionController,
                FindObjectsByType<ObstacleControl>(FindObjectsSortMode.None).Length);
        }

        private void InitializeGameplay()
        {
            _gameplayController = new GameplayController(_playerBallControl, _infectionController, _pathChecker,
                _cameraControl, _door);
            _gameplayController.Init();
        }

        private void OnDestroy()
        {
            _gameplayController.Dispose();
        }
    }
}
