using System;
using UnityEngine;

namespace Gameplay
{
    public class PlayerSpawnPoints : MonoBehaviour
    {
        internal static PlayerSpawnPoints Instance;
        [SerializeField] private Transform[] spawnPoints;


        internal Transform GetRandomSpawnPoint()
        {
            if (spawnPoints.Length == 0)
            {
                throw new Exception("No player Transforms found");
            }
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        }
    }
}
