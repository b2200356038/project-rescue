using System;
using System.Threading.Tasks;
using GameManagement;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Services
{
    class ServicesHelper : MonoBehaviour
    {
        static bool _initialLoad;
        ISession _currentSession;
        bool _isLeavingSession;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        async void Start()
        {
            UnityServices.Initialized += OnUnityServicesInitialized;
            await UnityServices.InitializeAsync();

            if (!_initialLoad)
            {
                _initialLoad = true;
                GameplayEventHandler.LoadMainMenuScene();
            }

            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            GameplayEventHandler.OnStartButtonPressed += OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed += LeaveSession;
            GameplayEventHandler.OnQuitGameButtonPressed += OnQuitGameButtonPressed;
            await VivoxManager.Instance.Initialize();
        }

        async void OnUnityServicesInitialized()
        {
            UnityServices.Initialized -= OnUnityServicesInitialized;
            await SignIn();
        }

        async void OnStartButtonPressed(string playerName, string sessionName)
        {
            var connectTask = ConnectToSession(playerName, sessionName);
            await connectTask;
            GameplayEventHandler.ConnectToSessionComplete(connectTask, sessionName);
        }

        async Task ConnectToSession(string playerName, string sessionName)
        {
            if (AuthenticationService.Instance == null)
            {
                return;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await SignIn();
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

            if (string.IsNullOrEmpty(sessionName))
            {
                Debug.LogError("Session name is empty. Cannot connect.");
                return;
            }

            await ConnectThroughLiveService(sessionName);
        }

        async Task SignIn()
        {
            try
            {
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SwitchProfile(GetRandomString(5));
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        async Task ConnectThroughLiveService(string sessionName)
        {
            // Join Session
            var options = new SessionOptions()
            {
                Name = sessionName,
                MaxPlayers = 64,
                IsPrivate = false,
            }.WithDistributedAuthorityNetwork();

            _currentSession = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
            _currentSession.RemovedFromSession += RemovedFromSession;
            _currentSession.StateChanged += CurrentSessionOnStateChanged;
        }

        void OnQuitGameButtonPressed()
        {
            LeaveSession();
            Application.Quit();
        }

        async void LeaveSession()
        {
            if (_currentSession != null && !_isLeavingSession)
            {
                try
                {
                    _isLeavingSession = true;
                    await _currentSession.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw;
                }
                finally
                {
                    _isLeavingSession = false;
                    ExitedSession();
                }
            }
        }

        void SignInFailed(RequestFailedException e)
        {
            AuthenticationService.Instance.SignInFailed -= SignInFailed;
            Debug.LogWarning($"Sign in via Authentication failed: e.ErrorCode {e.ErrorCode}");
        }

        void RemovedFromSession()
        {
            ExitedSession();
        }

        void CurrentSessionOnStateChanged(SessionState sessionState)
        {
            if (sessionState != SessionState.Connected)
            {
                ExitedSession();
            }
        }

        void ExitedSession()
        {
            if (_currentSession != null)
            {
                _currentSession.RemovedFromSession -= RemovedFromSession;
                _currentSession.StateChanged -= CurrentSessionOnStateChanged;
                _currentSession = null;
                GameplayEventHandler.ExitedSession();
            }
        }

        void OnClientStopped(bool obj)
        {
            LeaveSession();
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            }

            GameplayEventHandler.OnStartButtonPressed -= OnStartButtonPressed;
            GameplayEventHandler.OnReturnToMainMenuButtonPressed -= LeaveSession;
            GameplayEventHandler.OnQuitGameButtonPressed -= OnQuitGameButtonPressed;
        }

        static string GetRandomString(int length)
        {
            var r = new System.Random();
            var result = new char[length];

            for (var i = 0; i < length; i++)
            {
                result[i] = (char)r.Next('a', 'z' + 1);
            }

            return new string(result);
        }
    }
}
