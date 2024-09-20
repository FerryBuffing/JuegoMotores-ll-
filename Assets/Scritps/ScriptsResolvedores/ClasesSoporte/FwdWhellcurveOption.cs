
using UnityEngine;
[System.Serializable]
public class WhellcurveOption
{
    public float fwdExtremumSlip = 0.4f;   // 1 - slip ratio where the tire grips at most;
    public float fwdExtremumValue = 1;//20000 - 100% of the available friction is used;
    public float fwdAsymptoteSlip = 2;// 2 - slip ratio where the curve has no grip;
    public float fwdAsymptoteValue = 0.5f;   //10000 - 0% of the friction available is used;
    [Range(0.01f, 1000.0f)] public float fwdStiffness = 1; // Multipliers for the values of the curve.
    public float sideExtremumSlip = 0.2f;   // 1 - slip ratio where the tire grips at most;
    public float sideExtremumValue = 1000;//20000 - 100% of the available friction is used;
    public float sideAsymptoteSlip = 2;// 2 - slip ratio where the curve has no grip;
    public float sideAsymptoteValue = 0.5f; //10000 - 0% of the friction available is used;
    [Range(0.01f, 2000.0f)] public float sideStiffness = 1; // Multipliers for the values of the curve.
}