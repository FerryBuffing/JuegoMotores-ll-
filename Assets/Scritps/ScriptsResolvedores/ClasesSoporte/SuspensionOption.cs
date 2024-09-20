
using UnityEngine;

[System.Serializable]
public class SuspensionOption
{
    [Range(0.1f, 1.0f)] public float fwdSuspensionDistance = 0.1f;
    [Range(0.1f, 1.0f)] public float backSuspensionDistance = 0.1f;
    [Range(100, 50000)] public int fwdSuspensionSpring = 35000;
    [Range(100, 50000)] public int backSuspensionSpring = 35000;
    [Range(1, 9000)] public int fwdSpringDamper = 4500;
    [Range(1, 9000)] public int backSpringDamper = 4500;
}
