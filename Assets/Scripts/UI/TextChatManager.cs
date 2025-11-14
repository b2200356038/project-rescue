using System;
using System.Collections.Generic;
using Game.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game.GameManagement.GameplayEventHandler;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public class TextChatManager : MonoBehaviour
    {
 
        [SerializeField]
        GameObject messagePrefab;

        [SerializeField]
        Transform messageListContent;

        [SerializeField]
        TMP_InputField messageInputField;
        
        [SerializeField]
        GameObject textChatView;

        [SerializeField]
        ScrollRect scrollRect;

        List<ChatMessage> _messages = new();
        List<GameObject> _messageObjects = new();
        bool _isChatActive;

        void OnEnable()
        {
            messageInputField.onSelect.AddListener(OnTextfieldFocusIn);
            messageInputField.onDeselect.AddListener(OnTextfieldFocusOut);
            messageInputField.onSubmit.AddListener(OnSubmit);
            //SetViewInteractable(_isChatActive);
            //textChatView.SetActive(false);
            BindSessionEvents(true);
            _messages.Clear();
            //AddMessage(new ChatMessage("System", "Enjoy!"));
        }

        void OnSubmit(string _)
        {
            if (messageInputField.isFocused)
            {
                SendMessage();
                StartCoroutine(FocusInputFieldDelayed());
            }
        }

        System.Collections.IEnumerator FocusInputFieldDelayed()
        {
            yield return null;
            messageInputField.Select();
            messageInputField.ActivateInputField();
        }

        void OnTextfieldFocusIn(string _)
        {
            InputSystemManager.Instance.EnableUIInputs();
        }

        void OnTextfieldFocusOut(string _)
        {
            InputSystemManager.Instance.EnableGameplayInputs();
        }

        void OnDisable()
        {
            messageInputField.onSelect.RemoveListener(OnTextfieldFocusIn);
            messageInputField.onDeselect.RemoveListener(OnTextfieldFocusOut);
            messageInputField.onSubmit.RemoveListener(OnSubmit);
            BindSessionEvents(false);
        }

        void OnOpenChat(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (_isChatActive) return;

            _isChatActive = true;
            textChatView.SetActive(true);
            SetViewInteractable(true);

            messageInputField.Select();
            messageInputField.ActivateInputField();
        }

        void OnCloseChat(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (!_isChatActive) return;

            _isChatActive = false;
            textChatView.SetActive(false);
            SetViewInteractable(false);

            EventSystem.current.SetSelectedGameObject(null);
        }
        
        void SetViewInteractable(bool interactable)
        {
            messageInputField.interactable = interactable;
            if (scrollRect != null)
            {
                scrollRect.enabled = interactable;
            }
        }

        void SendMessage()
        {
            if (!string.IsNullOrEmpty(messageInputField.text))
            {
                SendTextMessage(messageInputField.text);
                messageInputField.text = "";
            }
        }

        void BindSessionEvents(bool doBind)
        {
            if (doBind)
            {
                OnChatIsReady += OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
                OnTextMessageReceived += OnChannelMessageReceived;
            }
            else
            {
                OnChatIsReady -= OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
            }
        }

        void OnOnChatIsReady(bool isReady, string channelName)
        {
            textChatView.SetActive(isReady);
        }

        void OnChannelMessageReceived(string sender, string message, bool fromSelf)
        {
            string nameColorHex = fromSelf ? "#F0FFFF" : "#D2FFFF";
            string coloredSender = $"<color={nameColorHex}>{sender}</color>";
            ChatMessage chatMessage = new ChatMessage(coloredSender, message);
            AddMessage(chatMessage);
        }

        void AddMessage(ChatMessage message)
        {
            _messages.Add(message);
            GameObject messageObj = Instantiate(messagePrefab, messageListContent);
            _messageObjects.Add(messageObj);
            TMP_Text messageText = messageObj.transform.Find("MessageText")?.GetComponent<TMP_Text>();
            if (messageText != null)
            {
                messageText.text = $"{message.name}: {message.message}";
            }
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        void ClearMessages()
        {
            foreach (var messageObj in _messageObjects)
            {
                if (messageObj != null)
                    Destroy(messageObj);
            }

            _messageObjects.Clear();
            _messages.Clear();
        }
    }

    [Serializable]
    public class ChatMessage
    {
        public string name;
        public string message;

        public ChatMessage(string name, string message)
        {
            this.name = name;
            this.message = message;
        }
    }
}