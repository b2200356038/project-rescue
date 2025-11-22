using System;
using System.Collections.Generic;
using Game.Vehicle;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerStateMachine : NetworkBehaviour, INetworkUpdateSystem
    {
        [Header("References")]
        [SerializeField] internal Rigidbody rb;
        [SerializeField] internal PlayerController playerController; 
        [SerializeField] internal PlayerInput playerInput;
        [SerializeField] internal Collider playerCollider; 

        [Header("State")]
        [SerializeField] private NetworkVariable<PlayerState> currentState = new(
            PlayerState.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private Dictionary<PlayerState, PlayerStateBase> _states;
        private PlayerStateBase _activeState;
        private VehicleController _currentVehicle;

        public PlayerState CurrentState => currentState.Value;
        public event Action<PlayerState, PlayerState> OnStateChanged; 

        private void Awake()
        {
            _states = new Dictionary<PlayerState, PlayerStateBase>
            {
                [PlayerState.OnFoot] = new PlayerOnFootState(),
                [PlayerState.Vehicle] = new PlayerVehicleState()
            };

            foreach (var state in _states.Values)
            {
                state.Initialize(this);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            currentState.OnValueChanged += OnStateChangedCallback;
            if (HasAuthority)
            {
                ChangeState(PlayerState.OnFoot);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
                this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);
            }
        }

        public override void OnNetworkDespawn()
        {
            currentState.OnValueChanged -= OnStateChangedCallback;
            this.UnregisterAllNetworkUpdates();
            base.OnNetworkDespawn();
        }
        
        public void ChangeState(PlayerState newState)
        {
            if (!IsOwner) return;
            if (currentState.Value == newState) return;
            currentState.Value = newState;
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
                if (HasAuthority) _activeState.OnEnter();
            }
            OnStateChanged?.Invoke(previousState, newState);
        }
        public void EnterVehicle(VehicleController vehicleController, int seatIndex)
        {
            if (!HasAuthority) return;
            EnterVehicleRpc(new NetworkBehaviourReference(vehicleController), seatIndex);
        }

        [Rpc(SendTo.Everyone)]
        private void EnterVehicleRpc(NetworkBehaviourReference vehicleRef, int seatIndex)
        {
            if (vehicleRef.TryGet(out VehicleController vehicle, NetworkManager))
            {
                _currentVehicle = vehicle;
                if (_states[PlayerState.Vehicle] is PlayerVehicleState vehicleState)
                {
                    vehicleState.SetVehicle(vehicle);
                }
                if (playerCollider) playerCollider.enabled = false;
                var seatManager = vehicle.GetComponent<VehicleSeatManager>();
                Transform targetSeat = seatManager.GetSeatTransformByIndex(seatIndex); 
                if (targetSeat != null)
                {
                    transform.position = targetSeat.position;
                    transform.rotation = targetSeat.rotation;
                }
                if (HasAuthority)
                {
                    ChangeState(PlayerState.Vehicle);
                }
            }
        }
        

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    _activeState?.OnNetworkUpdate();
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    _activeState?.OnNetworkFixedUpdate();
                    break;
            }
        }
        
        public VehicleController GetCurrentVehicle() => _currentVehicle;
    }
}