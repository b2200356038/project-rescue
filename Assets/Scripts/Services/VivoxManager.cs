using System;
using System.Threading.Tasks;
using GameManagement;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using Utils;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        
        private string _textChannelName;
        private string _voiceChannelName;
        private const int AudibleDistance=20;
        private const int ConversationalDistance = 1;
        private const float AudioFadeByDistance = 1f;
        
        internal static VivoxManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance==null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
                
            }
            Destroy(gameObject);
        }

        internal async Task Initialize()
        {
            await VivoxService.Instance.InitializeAsync();
            BindGlobalEvents(true);
        }
        async void LoginVivox(Task t, string sessionName)
        {
            _textChannelName = sessionName + "_text";
            _voiceChannelName = sessionName + "_voice";
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = AuthenticationService.Instance.PlayerName,
                PlayerId = AuthenticationService.Instance.PlayerId
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        async void OnLoggedInVivox()
        {
            await JoinChannels();
        }
        async Task JoinChannels()
        {
            var positionalChannelProperties = new Channel3DProperties(AudibleDistance, ConversationalDistance, AudioFadeByDistance, AudioFadeModel.InverseByDistance);
            BindChannelEvents(true);
            await VivoxService.Instance.JoinPositionalChannelAsync(_voiceChannelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(_textChannelName, ChatCapability.TextOnly);
        }
        void OnParticipantLeftChannel(VivoxParticipant vivoxParticipant)
        {
            var channelOptions = new ChannelOptions();
            if (vivoxParticipant.ChannelName != _voiceChannelName)
                return;

            GameplayEventHandler.ParticipantLeftVoiceChat(vivoxParticipant);
        }
        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            if (vivoxParticipant.ChannelName != _voiceChannelName)
                return;
            GameplayEventHandler.ParticipantJoinedVoiceChat(vivoxParticipant);
        }

        void OnChannelJoined(string channelName)
        {
            if (channelName == _textChannelName)
                GameplayEventHandler.SetTextChatReady(true, _textChannelName);
        }

        async void LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false, _textChannelName);
            await VivoxService.Instance.LogoutAsync();
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(_textChannelName, message);
        }

        void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName = PlayerUtils.ExtractPlayerNameFromAuthUserName(vivoxMessage.SenderDisplayName);
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, _voiceChannelName, false);
        }
        
        
        private void BindGlobalEvents(bool doBind)
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= LoginVivox;
            VivoxService.Instance.LoggedIn -= OnLoggedInVivox;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            GameplayEventHandler.OnExitedSession -= LogoutVivox;

            if (doBind)
            {
                GameplayEventHandler.OnConnectToSessionCompleted += LoginVivox;
                VivoxService.Instance.LoggedIn += OnLoggedInVivox;
                VivoxService.Instance.ChannelJoined += OnChannelJoined;
                GameplayEventHandler.OnExitedSession += LogoutVivox;
            }
        }
        
        void BindChannelEvents(bool doBind)
        {
            VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantLeftChannel;
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;

            if (doBind)
            {
                VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantLeftChannel;
                GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
                VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
            }
        }
    }
}
