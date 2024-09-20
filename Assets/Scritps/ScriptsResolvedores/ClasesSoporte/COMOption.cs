
[System.Serializable]
public class COMOption
{
    public float fwdCOMRatio = 1;
    public float fwdMaxValue = 0.4f;
    public float sideCOMRatio = 1;
    public float sideMaxValue = 0.2f;
    public bool modifyCenterOfMass = false;
    public float rollCorrectionY = -2;
    public float rollRatio = 1;
    public bool modifyRollCOM = false;
}