using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectWheelCollider : MonoBehaviour
{
    private WheelCollider carWheel;
    private WheelHit hit;
    private bool isHit = false;

    // Start is called before the first frame update
    void Start()
    {
        carWheel = GetComponent<WheelCollider>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        isHit = carWheel.GetGroundHit(out hit);
        if(isHit )
        {
            transform.parent.parent.SendMessage("HasContact");
        }
    }
}
