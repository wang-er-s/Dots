using UnityEngine;

public class CameraTargetSingleton : MonoBehaviour
{
    public   static CameraTargetSingleton Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
}