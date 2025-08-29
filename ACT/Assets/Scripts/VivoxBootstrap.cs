using UnityEngine;
using Unity.Services.Vivox;

public class VivoxBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void TouchVivox()
    {
        var t = typeof(VivoxService);
    }
}

