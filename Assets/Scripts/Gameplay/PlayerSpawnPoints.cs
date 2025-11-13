using System;
using UnityEngine;

namespace Gameplay
{
    class PlayerSpawnPoints : MonoBehaviour
    {
        internal static PlayerSpawnPoints Instance;

        [SerializeField] private Transform[] playerSpawnPoints;

        void Awake()
        {
            Instance = this;
        }

        internal Transform GetRandomSpawnPoint()
        {
            if (playerSpawnPoints.Length == 0)
            {
                throw new Exception("No player Transforms found in m_PlayerSpawnPoints");
            }
            return playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)];
        }
    }
}
