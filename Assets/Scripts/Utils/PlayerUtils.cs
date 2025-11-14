using UnityEngine;

namespace Game.Utils
{
    public class PlayerUtils : MonoBehaviour
    {
        public static string ExtractPlayerNameFromAuthUserName(string authenticationUserName)
        {
            var lastHashIndex = authenticationUserName.LastIndexOf('#');
            return lastHashIndex != -1 ? authenticationUserName[..lastHashIndex] : authenticationUserName;
        }
    }
}
