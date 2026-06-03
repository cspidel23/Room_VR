using System;
using UnityEngine;

namespace RoomVR.MiniGame
{
    // Base class for all mini-games.
    //
    // Usage:
    //   1. Extend this class.
    //   2. Implement IsActive, StartGame(), StopGame().
    //   3. Override OnStickInput / OnFireInput only if you need them.
    //   4. Drag the component onto ControllerGrabHandler.MiniGame.
    public abstract class MiniGameBase : MonoBehaviour
    {
        public event Action<int> OnScoreChanged;
        public event Action OnGameCleared;

        public abstract bool IsActive { get; }

        // Called when the player grabs the controller with both hands.
        public abstract void StartGame();

        // Called when the controller is released.
        public abstract void StopGame();

        // Resets the game to its initial state (score, layout, difficulty). Override to use.
        // Called by GamePhaseDirector when a new phase begins, while the screen is black.
        public virtual void ResetGame() { }

        // Called every frame with left-stick value. Override to use.
        public virtual void OnStickInput(Vector2 input) { }

        // Called on trigger press. Override to use.
        public virtual void OnFireInput() { }

        protected void RaiseScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        protected void RaiseGameCleared() => OnGameCleared?.Invoke();
    }
}
