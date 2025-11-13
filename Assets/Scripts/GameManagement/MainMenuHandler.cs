using System.Threading.Tasks;
using Input;
using UnityEngine;

namespace GameManagement
{
    class MainMenuHandler : MonoBehaviour
    {
        void Start()
        {
            InputSystemManager.Instance.EnableUIInputs();
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

        void OnConnectToSessionCompleted(Task task, string sessionName)
        {
            if (task.IsCompletedSuccessfully)
            {
                GameplayEventHandler.LoadInGameScene();
            }
        }
    }
}
