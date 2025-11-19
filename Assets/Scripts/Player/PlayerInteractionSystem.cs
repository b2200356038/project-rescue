using System.Collections;
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
        
        private PlayerStateMachine _stateMachine;
        private IInteractable _currentInteractable;
        private Collider _potentialInteractCollider;
        private Collider[] _overlapResults = new Collider[8];
        
        private bool _holdingInteractionPerformed;
        private bool _waitingForOwnership;

        private void Awake()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
            
            if (raycastOrigin == null)
                raycastOrigin = transform;
                
            if (interactCollider == null)
            {
                interactCollider = gameObject.AddComponent<BoxCollider>();
                interactCollider.isTrigger = true;
                interactCollider.size = new Vector3(interactionRange * 2, 2f, interactionRange * 2); 
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!HasAuthority)
            {
                enabled = false;
                return;
            }

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
                this.UnregisterAllNetworkUpdates();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    _holdingInteractionPerformed = true;
                    OnHoldStarted();
                    break;
                case TapInteraction:
                    OnTapPerformed();
                    break;
            }
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                if (_holdingInteractionPerformed)
                {
                    OnHoldReleased(context.duration);
                }
                _holdingInteractionPerformed = false;
            }
        }

        private void OnTapPerformed()
        {
            if (_currentInteractable == null || !_currentInteractable.CanInteract())
                return;
            if (_currentInteractable is VehicleController vehicle)
            {
                _stateMachine.EnterVehicle(vehicle); 
            }
            else
            {
                _currentInteractable.Interact();
            }
        }
        
        private void OnHoldStarted()
        {
            if (_currentInteractable != null)
            {
                // Hold animasyonu vs.
            }
        }

        private void OnHoldReleased(double holdDuration)
        {
            // Hold bırakıldığında
        }

        private void CheckForInteractablesInRange()
        {
            Vector3 checkPosition = interactCollider.transform.position;
            Vector3 halfExtents = interactCollider.bounds.extents;
            
            int hitCount = UnityEngine.Physics.OverlapBoxNonAlloc(
                checkPosition,
                halfExtents,
                _overlapResults,
                Quaternion.identity,
                interactionLayer
            );

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;
            Collider closestCollider = null;

            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = _overlapResults[i];
                if (hitCollider.transform.IsChildOf(transform) || hitCollider == interactCollider)
                    continue;
                    
                if (hitCollider.TryGetComponent<IInteractable>(out var interactable))
                {
                    float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    
                    if (distance < closestDistance && distance <= interactionRange)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                        closestCollider = hitCollider;
                    }
                }
            }

            if (closestInteractable != _currentInteractable)
            {
                _currentInteractable = closestInteractable;
                _potentialInteractCollider = closestCollider;
                
                if (_currentInteractable != null)
                {
                    OnInteractableInRange(_currentInteractable);
                }
                else
                {
                    OnInteractableOutOfRange();
                }
            }
        }

        private void OnInteractableInRange(IInteractable interactable)
        {
            string prompt = interactable.GetInteractionPrompt();
            Debug.Log($"Interaction available: {prompt}");
        }

        private void OnInteractableOutOfRange()
        {
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStage.FixedUpdate)
            {
                CheckForInteractablesInRange();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (interactCollider != null)
            {
                Gizmos.color = _currentInteractable != null ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(interactCollider.bounds.center, interactCollider.bounds.size);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
        
    }
}