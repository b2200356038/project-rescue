using System;
using System.Collections.Generic;
using Game.Physics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerStateMachine : NetworkBehaviour, INetworkUpdateSystem
    {
        [SerializeField] internal Rigidbody rb;
        [SerializeField] internal PhysicsPlayerController physicsPlayerController;
        [SerializeField] internal PlayerInput playerInput;

        private NetworkVariable<PlayerState> _currentState = new(
            PlayerState.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Dictionary<PlayerState, PlayerStateBase> _states;
        private PlayerStateBase _activeState;

        public PlayerState CurrentState => _currentState.Value;
        public event Action<PlayerState, PlayerState> OnStateChanged;

        private void Awake()
        {
            _states = new Dictionary<PlayerState, PlayerStateBase>
            {
                [PlayerState.OnFoot] = new PlayerOnFootState()
            };
            foreach (var state in _states.Values)
            {
                state.Initialize(this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _currentState.OnValueChanged += OnStateChangedCallback;
            if (IsOwner)
            {
                ChangeState(PlayerState.OnFoot);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
            }
        }

        private void ChangeState(PlayerState newState)
        {
            if (!IsOwner)
                return;
            if (_currentState.Value == newState)
                return;
            _currentState.Value = newState;
        }

        private void OnStateChangedCallback(PlayerState previousState, PlayerState newState)
        {
            if (_activeState != null)
            {
                _activeState.OnExit();
            }

            if (_states.TryGetValue(newState, out var state))
            {
                _activeState = state;
                if (IsOwner) _activeState.OnEnter();
            }

            OnStateChanged?.Invoke(previousState, newState);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    _activeState.OnNetworkUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    _activeState.OnNetworkFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }

        public override void OnNetworkDespawn()
        {
            _currentState.OnValueChanged -= OnStateChangedCallback;
            this.UnregisterAllNetworkUpdates();
            base.OnNetworkDespawn();
        }
    }
}