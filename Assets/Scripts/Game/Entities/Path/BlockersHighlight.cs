using System.Collections.Generic;
using Game.Entities.Obstacles;
using Game.Entities.Player;

namespace Game.Entities.Path
{
    public class BlockersHighlight
    {
        private readonly PathChecker _pathChecker;
        private readonly PlayerBallControl _playerBallControl;

        private List<ObstacleControl> _highlighted = new();
        private List<ObstacleControl> _found = new();

        public BlockersHighlight(PathChecker pathChecker, PlayerBallControl playerBallControl)
        {
            _pathChecker = pathChecker;
            _playerBallControl = playerBallControl;
        }

        public void Refresh()
        {
            _pathChecker.CollectBlockers(_playerBallControl.DistanceToTarget, _found);

            foreach (ObstacleControl obstacle in _highlighted)
            {
                if (!_found.Contains(obstacle))
                {
                    obstacle.SetHighlighted(false);
                }
            }

            foreach (ObstacleControl obstacle in _found)
            {
                obstacle.SetHighlighted(true);
            }

            (_highlighted, _found) = (_found, _highlighted);
        }

        public void Clear()
        {
            foreach (ObstacleControl obstacle in _highlighted)
            {
                obstacle.SetHighlighted(false);
            }

            _highlighted.Clear();
        }
    }
}
