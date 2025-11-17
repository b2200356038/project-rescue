using Game.GameManagement;
using Game.Input;
using UnityEngine;

namespace Game.GameManagement
{
    class InGameHandler : MonoBehaviour
    {
        void Start()
        {
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += GameplayEventHandler.LoadMainMenuScene;
            GameplayEventHandler.OnExitedSession += GameplayEventHandler.LoadMainMenuScene;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnReturnToMainMenuButtonPressed -= GameplayEventHandler.LoadMainMenuScene;
            GameplayEventHandler.OnExitedSession -= GameplayEventHandler.LoadMainMenuScene;
        }
    }
}
