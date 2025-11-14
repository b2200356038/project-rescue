using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class MainUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField sessionNameInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        
        [Header("Development Settings")]
        [SerializeField] private bool autoFillInEditor = true;
        
        private const int MaxNameLength = 50;
        
        private static readonly string[] RandomNames = new[]
        {
            "Phoenix", "Shadow", "Storm", "Blaze", "Frost",
            "Nova", "Raven", "Wolf", "Dragon", "Tiger",
            "Hawk", "Viper", "Ghost", "Spark", "Thunder"
        };
        private void Awake()
        {
            startButton.interactable = true;
            playerNameInput.onValueChanged.AddListener(_ => OnFieldChanged());
            sessionNameInput.onValueChanged.AddListener(_ => OnFieldChanged());
            startButton.onClick.AddListener(OnStartButtonPressed);
            quitButton.onClick.AddListener(OnQuitButtonPressed);
            GameplayEventHandler.OnConnectToSessionCompleted += OnConnectToSessionCompleted;
        }
        
        private void OnFieldChanged()
        {
            playerNameInput.text = SanitizePlayerName(playerNameInput.text);
        }

        private void OnStartButtonPressed()
        {
            if (Application.isEditor && autoFillInEditor)
            {
                if (string.IsNullOrEmpty(playerNameInput.text))
                {
                    playerNameInput.text = GenerateRandomPlayerName();
                }
                
                if (string.IsNullOrEmpty(sessionNameInput.text))
                {
                    sessionNameInput.text = GenerateRandomSessionName();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(playerNameInput.text))
                {
                    playerNameInput.text = GenerateRandomPlayerName();
                }
                
                if (string.IsNullOrEmpty(sessionNameInput.text))
                {
                    sessionNameInput.text = GenerateRandomSessionName();
                }
            }
            
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
        
        private static string GenerateRandomPlayerName()
        {
            string name = RandomNames[UnityEngine.Random.Range(0, RandomNames.Length)];
            int number = UnityEngine.Random.Range(100, 999);
            return $"{name}{number}";
        }
        
        private static string GenerateRandomSessionName()
        {
            int number = UnityEngine.Random.Range(1, 10);
            return number.ToString();
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