using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GearOption
{
    public float[] GearRatio = { 2.66f, 1.78f, 1.30f, 1.00f, 0.74f, 0.50f };
    [Range(1.0f, 10.0f)] public float DifferentialRatio = 3.42f; // Diferential ratio.
    public int CurrentGear = 0;
    public bool isAutomatic = true;
}
