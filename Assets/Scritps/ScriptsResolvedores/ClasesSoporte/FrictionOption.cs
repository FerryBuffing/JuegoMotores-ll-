using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FrictionOption
{
    public bool adjustStiffness = false;
    public AnimationCurve frwStiffnessCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
    public AnimationCurve sideStiffnessCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
    public bool adjustFrictionCurve = false;
    public AnimationCurve frwFrictionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
    public AnimationCurve sideFrictionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
    public bool useHandBreake = true;
    public AnimationCurve HandBrakeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1.0f, 0.3f));
}