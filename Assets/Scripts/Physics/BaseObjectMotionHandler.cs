using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Physics
{
    class BaseObjectMotionHandler : NetworkTransform, ICollisionHandler, IContactEventHandlerWithInfo
    {
        protected CollisionMessageInfo CollisionMessage;

        [SerializeField] CollisionType collisionType;
        [SerializeField] private ushort collisionDamage;

        [SerializeField] private List<Collider> childColliders;

        protected Rigidbody Rigidbody { get; private set; }
        protected ulong LastEventId { get; private set; }
        protected NetworkRigidbody NetworkRigidbody { get; private set; }


        protected void EnableColliders(bool enable)
        {
            foreach (var col in childColliders)
            {
                if (col != null)
                    col.enabled = enable;
            }
        }

        public Rigidbody GetRigidbody()
        {
            return Rigidbody;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual Vector3 GetObjectVelocity(bool getReference = false)
        {
            return OnGetObjectVelocity(getReference);

        }
        
        protected virtual Vector3 OnGetObjectVelocity(bool getReference = false)
        {
            if (Rigidbody != null)
            {
                return Rigidbody.linearVelocity;
            }
            return Vector3.zero;
        }
        
        
        protected void SetObjectVelocity(Vector3 velocity)
        {
            if (Rigidbody != null)
                Rigidbody.linearVelocity = velocity;
        }

        protected virtual Vector3 OnGetObjectAngularVelocity()
        {
            if (Rigidbody != null)
                return Rigidbody.angularVelocity;

            return Vector3.zero;
        }

        protected override void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            NetworkRigidbody = GetComponent<NetworkRigidbody>();
            base.Awake();
        }

        protected virtual void Start()
        {
            CollisionMessage.Damage = collisionDamage;
            CollisionMessage.SetFlag(true, (uint)collisionType);
        }


        static GameObject GetRootParent(GameObject parent)
        {
            return parent.transform.root.gameObject;
        }

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }
        
        void HandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            OnHandleCollision(collisionMessage, isLocal, applyImmediately);
        }
        
        
        protected virtual void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false,
            bool applyImmediately = false)
        {
            // Child class override
        }


        [Rpc(SendTo.Authority, RequireOwnership = false)]
        public void HandleCollisionRpc(CollisionMessageInfo collisionMessage, RpcParams rpcParams = default)
        {
            HandleCollision(collisionMessage);
        }


        public void SendCollisionMessage(CollisionMessageInfo collisionMessage)
        {
            HandleCollisionRpc(collisionMessage);
        }


        protected void EventCollision(Vector3 averagedCollisionNormal, BaseObjectMotionHandler collidingBodyBaseObject)
        {
            collidingBodyBaseObject.SendCollisionMessage(CollisionMessage);
        }


        public void ContactEvent(
            ulong eventId,
            Vector3 averagedCollisionNormal,
            Rigidbody collidingBody,
            Vector3 contactPoint,
            bool hasCollisionStay = false,
            Vector3 averagedCollisionStayNormal = default)
        {
            OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint, hasCollisionStay,
                averagedCollisionStayNormal);
            LastEventId = eventId;
        }

        public ContactEventHandlerInfo GetContactEventHandlerInfo()
        {
            var contactEventHandlerInfo = new ContactEventHandlerInfo();
            contactEventHandlerInfo.ProvideNonRigidBodyContactEvents = false;
            contactEventHandlerInfo.HasContactEventPriority = HasAuthority;
            return contactEventHandlerInfo;
        }
        

        protected virtual void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody,
            Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
        // Child class override
        }
    }
}