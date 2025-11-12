using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace GameManagement
{
    public static class GameplayEventHandler
    {
        internal static event Action<NetworkObject> OnNetworkObjectDespawned;
        internal static event Action<NetworkObject, ulong, ulong> OnNetworkObjectOwnershipChanged;
        internal static event Action<string, string> OnStartButtonPressed;
        internal static event Action OnReturnToMainMenuButtonPressed;
        internal static event Action OnQuitGameButtonPressed;
        internal static event Action OnExitedSession;
        internal static event Action<Task, string> OnConnectToSessionCompleted;


        internal static void StartButtonPressed(string playerName, string sessionName)
        {
            OnStartButtonPressed?.Invoke(playerName,sessionName);
        }

        internal static void ReturnToMainMenuPressed()
        {
            OnReturnToMainMenuButtonPressed?.Invoke();
        }
        
        internal static void QuitGameButtonPressed()
        {
            OnQuitGameButtonPressed?.Invoke();
        }
        
        internal static void ConnectToSessionComplete(Task task, string sessionName)
        {
            OnConnectToSessionCompleted?.Invoke(task, sessionName);
        }
        internal static void NetworkObjectDespawned(NetworkObject networkObject)
        {
            OnNetworkObjectDespawned?.Invoke(networkObject);
        }

        internal static void NetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            OnNetworkObjectOwnershipChanged?.Invoke(networkObject, previous, current);
        }

        internal static void ExitedSession()
        {
            OnExitedSession?.Invoke();
        }
        
        internal static void LoadMainMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }
        
        internal static void LoadInGameScene()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
