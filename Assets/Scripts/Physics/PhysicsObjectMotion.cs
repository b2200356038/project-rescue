using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Game.Utils;

namespace Game.Physics
{
    
    class PhysicsObjectMotion : BaseObjectMotionHandler
    {
        
        [SerializeField]
        float maxAngularVelocity = 30;
        [SerializeField]
        float maxVelocity = 30;
        protected NetworkVariable<bool> IsInitialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<float> Mass = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> AngularVelocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> Velocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> Torque = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected NetworkVariable<Vector3> Force = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected List<RemoteForce> RemoteAppliedForce = new List<RemoteForce>();
        protected override Vector3 OnGetObjectVelocity(bool getReference = false)
        {
            if (getReference)
            {
                return Velocity.Value;
            }
            return base.OnGetObjectVelocity();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Vector3 OnGetObjectAngularVelocity()
        {
            return AngularVelocity.Value;
        }

        protected void UpdateVelocity(Vector3 velocity, bool updateObjectVelocity = true)
        {
            if (HasAuthority)
            {
                if (updateObjectVelocity)
                {
                    SetObjectVelocity(velocity);
                }

                Velocity.Value = velocity;
            }
        }

        protected void UpdateAngularVelocity(Vector3 torque)
        {

            if (HasAuthority)
            {
                Rigidbody.AddTorque(torque);
                Torque.Value = torque;
            }
        }

        protected void UpdateImpulseForce(Vector3 impulseForce)
        {
            if (HasAuthority)
            {
                Rigidbody.AddForce(impulseForce, ForceMode.Impulse);
                Force.Value = impulseForce;
            }
        }

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            if (!IsInitialized.Value)
            {
                return;
            }
            if (networkTransformState.HasRotAngleChange && !Rigidbody.isKinematic)
            {
                if (Vector3.Distance(GetObjectAngularVelocity(), Rigidbody.angularVelocity) > RotAngleThreshold)
                {
                    UpdateAngularVelocity(Rigidbody.angularVelocity);
                }
            }

            if (networkTransformState.HasPositionChange && !Rigidbody.isKinematic)
            {
                var velocity = GetObjectVelocity();
                if (Vector3.Distance(GetObjectVelocity(true), velocity) > PositionThreshold)
                {
                    UpdateVelocity(velocity, false);
                }
            }
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            EnableColliders(true);
            RigidbodyContactEventManager.Instance.RegisterHandler(this);
            Rigidbody.maxAngularVelocity = maxAngularVelocity;
            Rigidbody.maxLinearVelocity = maxVelocity;
            if (HasAuthority)
            {
                NetworkRigidbody.SetIsKinematic(false);
#if SESSION_STORE_ENABLED
            if (!BeenInitialized.Value)
#endif
                {
                    IsInitialized.Value = true;
                }
#if SESSION_STORE_ENABLED
            else
            {
                Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), MaxAngularVelocity);
                SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(), MaxVelocity));
            }
#endif
            }
        }


        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (NetworkManager.LocalClientId==current)
            {
                NetworkRigidbody.SetIsKinematic(false);
                if (IsInitialized.Value)
                {
                    Rigidbody.angularVelocity = Vector3.ClampMagnitude(GetObjectAngularVelocity(), maxAngularVelocity);
                    SetObjectVelocity(Vector3.ClampMagnitude(GetObjectVelocity(true), maxVelocity));
                }
                else
                {
                    Rigidbody.AddTorque(Torque.Value, ForceMode.Impulse);
                    Rigidbody.AddForce(Force.Value, ForceMode.Impulse);
                }
            }
            else
            {
                NetworkRigidbody.SetIsKinematic(true);
            }
            base.OnOwnershipChanged(previous, current);
        }

        protected override void OnHandleCollision(CollisionMessageInfo collisionMessage, bool isLocal = false, bool applyImmediately = false)
        {
            if (collisionMessage.HasCollisionForce())
            {
                AddForceDirect(collisionMessage.CollisionForce);
            }
            base.OnHandleCollision(collisionMessage, isLocal, applyImmediately);
        }
        
        void AddForceDirect(Vector3 force)
        {
            var remoteForce = new RemoteForce()
            {
                TargetForce = force,
                AppliedForce = Vector3.zero,
            };
            RemoteAppliedForce.Add(remoteForce);
        }
        
        protected override void OnContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            var collidingBaseObjectMotion = collidingBody.GetComponent<BaseObjectMotionHandler>();
            var collidingBodyPhys = collidingBaseObjectMotion as PhysicsObjectMotion;
            if (eventId == LastEventId || collidingBaseObjectMotion == null || (!HasAuthority && !collidingBaseObjectMotion.HasAuthority))
            {
                return;
            }
            if (collidingBodyPhys == null || !collidingBodyPhys.IsSpawned)
            {
                return;
            }
            var collisionNormal = hasCollisionStay ? averagedCollisionStayNormal : averagedCollisionNormal;

            var thisVelocity = (!Rigidbody.isKinematic ? Rigidbody.linearVelocity.sqrMagnitude : GetObjectVelocity().sqrMagnitude) * 0.5f;
            var otherVelocity = (!collidingBody.isKinematic ? collidingBody.linearVelocity.sqrMagnitude : collidingBodyPhys.GetObjectVelocity().sqrMagnitude) * 0.5f;
            var thisKineticForce = (Rigidbody.mass / collidingBody.mass) * -collisionNormal * thisVelocity;
            var otherKineticForce = (collidingBody.mass / Rigidbody.mass) * collisionNormal * otherVelocity;

            if (!Rigidbody.isKinematic && collidingBody.isKinematic && thisVelocity > 0.01f)
            {
                CollisionMessage.CollisionForce = thisKineticForce;
                CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                
                EventCollision(averagedCollisionNormal, collidingBodyPhys);
            }

            if (Rigidbody.isKinematic && !collidingBody.isKinematic && otherVelocity > 0.01f)
            {
                collidingBodyPhys.CollisionMessage.CollisionForce = otherKineticForce;
                collidingBodyPhys.CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
                collidingBodyPhys.EventCollision(averagedCollisionNormal, this);
            }
            else if (!Rigidbody.isKinematic && !collidingBody.isKinematic && otherVelocity > 0.01f)
            {
                CollisionMessage.CollisionForce = thisKineticForce;
               CollisionMessage.SetFlag(true, (uint)CollisionCategoryFlags.CollisionForce);
            }

            base.OnContactEvent(eventId, averagedCollisionNormal, collidingBody, contactPoint, hasCollisionStay, averagedCollisionStayNormal);
        }
        
        void ApplyCollisionForce(Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
            Rigidbody.AddTorque(force * 0.25f, ForceMode.Impulse);
        }
        
        void ProcessRemoteForces()
        {
            if (RemoteAppliedForce.Count == 0)
            {
                return;
            }

            var accumulativeForce = Vector3.zero;
            for (int i = RemoteAppliedForce.Count - 1; i >= 0; i--)
            {
                var remoteForce = RemoteAppliedForce[i];
                accumulativeForce += remoteForce.TargetForce;
                if (MathUtils.Approximately(remoteForce.TargetForce, Vector3.zero))
                {
                    RemoteAppliedForce.RemoveAt(i);
                }
                else
                {
                    RemoteAppliedForce[i] = remoteForce;
                }
            }

            ApplyCollisionForce(accumulativeForce);
            RemoteAppliedForce.Clear();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsSpawned || !HasAuthority || Rigidbody != null && Rigidbody.isKinematic)
            {
                return;
            }
            ProcessRemoteForces();
        }
        
        public override void OnNetworkDespawn()
        {
            RigidbodyContactEventManager.Instance.RegisterHandler(this, false);
            base.OnNetworkDespawn();

            
        }

        protected Vector3 GetObjectAngularVelocity()
        {
            return OnGetObjectAngularVelocity();
        }
        
    }
    struct RemoteForce
    {
        public float EndOfLife;
        public Vector3 TargetForce;
        public Vector3 AppliedForce;
    }
}
