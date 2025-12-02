using System;
using Game.Gameplay;
using Game.Input;
using Game.Vehicle;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace Game.Player
{
    public class PlayerInteractionSystem : NetworkBehaviour, INetworkUpdateSystem
    {
        [Header("Settings")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        [Header("References")]
        [SerializeField] private Transform raycastOrigin;
        [SerializeField] private BoxCollider interactCollider;
        
        private IInteractable _currentInteractable;
        private PlayerStateMachine _stateMachine;
        private PlayerVehicleHandler _playerVehicleHandler;
        private readonly Collider[] _overlapResults = new Collider[8];

        private void Awake()
        {
            if (raycastOrigin == null) raycastOrigin = transform;
            if (interactCollider == null)
            {
                interactCollider = gameObject.AddComponent<BoxCollider>();
                interactCollider.isTrigger = true;
                interactCollider.size = new Vector3(interactionRange * 2, 2f, interactionRange * 2); 
            }
        }


        private void Start()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
            _playerVehicleHandler = GetComponent<PlayerVehicleHandler>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!HasAuthority) { return; }
            this.RegisterNetworkUpdate(NetworkUpdateStage.FixedUpdate);
            GameInput.Actions.Player.Interact.performed += OnInteractPerformed;
            GameInput.Actions.Player.Interact.canceled += OnInteractCanceled;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (HasAuthority)
            {
                GameInput.Actions.Player.Interact.performed -= OnInteractPerformed;
                GameInput.Actions.Player.Interact.canceled -= OnInteractCanceled;
            }
            this.UnregisterAllNetworkUpdates();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (context.interaction is TapInteraction)
                OnTapPerformed();
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
        }

        private void OnTapPerformed()
        {
            if (_currentInteractable == null || !_currentInteractable.CanInteract())
                return;
            if (_currentInteractable is VehicleController vehicle)
            {
                Debug.Log(_playerVehicleHandler==null);
                _playerVehicleHandler.TryEnterVehicle(vehicle);
            }
        }
        
        private void CheckForInteractablesInRange()
        {
            Vector3 checkPosition = interactCollider.transform.position;
            Vector3 halfExtents = interactCollider.bounds.extents;
            
            int hitCount = UnityEngine.Physics.OverlapBoxNonAlloc(checkPosition, halfExtents, _overlapResults, Quaternion.identity, interactionLayer);

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = _overlapResults[i];
                if (hitCollider.transform.IsChildOf(transform) || hitCollider == interactCollider) continue;
                    
                if (hitCollider.TryGetComponent<IInteractable>(out var interactable))
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (distance < closestDistance && distance <= interactionRange)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != null && closestInteractable != _currentInteractable)
            {
                _currentInteractable = closestInteractable;
            }
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStage.FixedUpdate) CheckForInteractablesInRange();
        }

        private void OnDrawGizmosSelected()
        {
            if (interactCollider != null)
            {
                Gizmos.color = _currentInteractable != null ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(interactCollider.bounds.center, interactCollider.bounds.size);
            }
        }
    }
}