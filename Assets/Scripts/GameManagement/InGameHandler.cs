using Input;
using UnityEngine;

namespace GameManagement
{
    class InGameHandler : MonoBehaviour
    {
        void Start()
        {
            InputSystemManager.Instance.EnableGameplayInputs();
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
