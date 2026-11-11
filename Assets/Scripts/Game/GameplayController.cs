using System;
using Game.Entities.Door;
using Game.Entities.Obstacles;
using Game.Entities.Path;
using Game.Entities.Player;
using UI.Managers;
using UI.Popups;
using UI.Popups.Variables;
using UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class GameplayController : IDisposable
    {
        private readonly PlayerBallControl _playerBallControl;
        private readonly InfectionController _infectionController;
        private readonly PathChecker _pathChecker;
        private readonly UIManager _uiManager;
        private readonly BlockersHighlight _blockersHighlight;
        private readonly CameraControl _cameraControl;
        private readonly DoorControl _door;

        private bool _isFinished;

        public GameplayController(PlayerBallControl playerBallControl, InfectionController infectionController,
            PathChecker pathChecker, CameraControl cameraControl, DoorControl door)
        {
            _playerBallControl = playerBallControl;
            _infectionController = infectionController;
            _pathChecker = pathChecker;
            _cameraControl = cameraControl;
            _door = door;
            _uiManager = UIManager.Instance;
            _blockersHighlight = new BlockersHighlight(pathChecker, playerBallControl);
        }

        public void Init()
        {
            _playerBallControl.OnMassDepleted += HandleMassDepleted;
            _playerBallControl.OnShotFinished += HandleShotFinished;
            _playerBallControl.OnMovementFinished += HandleMovementFinished;
            _infectionController.OnInfectionFinished += HandleInfectionFinished;
            _playerBallControl.OnMassChanged += HandleMassChanged;

            _blockersHighlight.Refresh();
        }

        public void Dispose()
        {
            _playerBallControl.OnMassDepleted -= HandleMassDepleted;
            _playerBallControl.OnShotFinished -= HandleShotFinished;
            _playerBallControl.OnMovementFinished -= HandleMovementFinished;
            _infectionController.OnInfectionFinished -= HandleInfectionFinished;
            _playerBallControl.OnMassChanged -= HandleMassChanged;
        }

        private void HandleMassChanged(float mass)
        {
            if (_isFinished)
            {
                return;
            }

            _blockersHighlight.Refresh();
        }

        private void HandleMassDepleted()
        {
            Lose();
        }

        private void HandleShotFinished()
        {
            if (_infectionController.IsActive || _playerBallControl.IsBusy)
            {
                return;
            }

            EvaluatePath();
        }

        private void HandleInfectionFinished()
        {
            if (_playerBallControl.IsBusy)
            {
                return;
            }

            EvaluatePath();
        }

        private void HandleMovementFinished()
        {
            _playerBallControl.StopGameplay();
            _door.Close(Win);
        }

        private void EvaluatePath()
        {
            if (_isFinished)
            {
                return;
            }

            float distanceToTarget = _playerBallControl.DistanceToTarget;
            if (_pathChecker.IsPathClear(distanceToTarget))
            {
                _blockersHighlight.Clear();
                _cameraControl.StartFollow(_playerBallControl.transform, _door.transform.position);
                _playerBallControl.MoveForward(distanceToTarget);
                return;
            }

            _blockersHighlight.Refresh();

            if (!_playerBallControl.CanShoot)
            {
                Lose();
            }
        }

        private void Win()
        {
            _isFinished = true;
            _blockersHighlight.Clear();
            _playerBallControl.StopGameplay();
            HideGameplayScreen();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.LevelComplete);

            LevelCompletedPopup levelCompletedPopup =
                (LevelCompletedPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.LevelComplete);
            levelCompletedPopup.OnButtonClicked += HandleRestartClicked;
        }

        private void Lose()
        {
            _isFinished = true;
            _blockersHighlight.Clear();
            _playerBallControl.StopGameplay();
            HideGameplayScreen();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.Result);

            ResultPopup resultPopup = (ResultPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.Result);
            resultPopup.OnButtonClicked += HandleRestartClicked;
        }

        private void HideGameplayScreen()
        {
            _uiManager.ScreensManager.GetScreen(ScreenTypes.Gameplay).Hide();
        }

        private void HandleRestartClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
