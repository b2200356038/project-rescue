using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "PlayerControllerSettings", menuName = "ScriptableObjects/PlayerControllerSettings", order = 1)]
    class PlayerControllerSettings : ScriptableObject
    {
        [SerializeField]
        internal float WalkSpeed;
        [SerializeField]
        internal float SprintSpeed;
        [SerializeField]
        internal float Acceleration;
        [SerializeField]
        internal float DragCoefficient;
        [SerializeField]
        internal float AirControlFactor;
        [SerializeField]
        internal float JumpImpusle;
        [SerializeField]
        internal float CustomGravityMultiplier;
        [SerializeField]
        internal float RotationSpeed;
        [SerializeField]
        internal float GroundCheckDistance;
    }
}
