using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField sessionNameInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;


        private const int MaxNameLength = 50;

        private void Awake()
        {
            startButton.interactable = false;
            playerNameInput.onValueChanged.AddListener(_ => OnFieldChanged());
            sessionNameInput.onValueChanged.AddListener(_ => OnFieldChanged());
            startButton.onClick.AddListener(OnStartButtonPressed);
            quitButton.onClick.AddListener(OnQuitButtonPressed);
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }
        private void OnFieldChanged()
        {
            playerNameInput.text = SanitizePlayerName(playerNameInput.text);
            bool canStart = !string.IsNullOrEmpty(playerNameInput.text) &&
                            !string.IsNullOrEmpty(sessionNameInput.text);
            startButton.interactable = canStart;
        }

        private void OnStartButtonPressed()
        {
            string playerName = playerNameInput.text;
            string sessionName = sessionNameInput.text;

            startButton.interactable = false; 
            GameplayEventHandler.StartButtonPressed(playerName, sessionName);
        }

        private void OnQuitButtonPressed()
        {
            GameplayEventHandler.QuitGamePressed();
        }

        private void OnConnectToSessionCompleted(Task task, string sessionName)
        {
            if (!task.IsCompletedSuccessfully)
                startButton.interactable = true;
        }

        private static string SanitizePlayerName(string input)
        {
            string clean = Regex.Replace(input, @"\s", "");
            return clean[..Math.Min(clean.Length, MaxNameLength)];
        }
        private void OnDestroy()
        {
            playerNameInput.onValueChanged.RemoveAllListeners();
            sessionNameInput.onValueChanged.RemoveAllListeners();
            startButton.onClick.RemoveAllListeners();
            quitButton.onClick.RemoveAllListeners();
            GameplayEventHandler.OnConnectToSessionCompleted -= OnConnectToSessionCompleted;
        }

    }
}