using System;
using System.Threading.Tasks;
using Input;
using UnityEngine;

namespace GameManagement
{
    public class MainMenuHandler : MonoBehaviour
    {
        private void Start()
        {
            InputSystemManager.Instance.EnableUIInputs();
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }

        private void OnDestroy()
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
