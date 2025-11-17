using UnityEngine;

namespace Game.Vehicle.Wheel
{
    [System.Serializable]
    public class Friction
    {
        [Header("Current State")]
        [Tooltip("Current force in friction direction (N)")]
        public float force;
    
        [Tooltip("Current slip in friction direction (normalized)")]
        public float slip;
    
        [Tooltip("Speed at the point of contact with surface (m/s)")]
        public float speed;
    
        [Header("Settings")]
        [Range(0f, 2f)]
        [Tooltip("Multiplies the grip value from friction curve")]
        public float grip = 1f;
    
        [Range(0f, 2f)]
        [Tooltip("Multiplies the slip calculation (higher = more responsive)")]
        public float stiffness = 1f;
    
        [Range(0.5f, 3f)]
        [Tooltip("Relationship between maximum friction force and tire load")]
        public float loadFactor = 1.5f;
    }
}