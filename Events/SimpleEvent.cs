using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEvent : MonoBehaviour
{
    [SerializeField] GameObject campo;
    [SerializeField] GameObject MegaRocas;
    public Transform[] spawnPositions;
    private int indicioIndex = 0;
    public float tiempoSpawn = 1f;
    public bool chequeo;
    private float timer = 0f;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if(chequeo == true && timer > tiempoSpawn)
        {
            TremendaLluvia();
            timer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        chequeo = true;
        Debug.Log(chequeo);

    }

    void TremendaLluvia()
    {
        if (spawnPositions.Length == 0) return; 
        Vector3 spawnPosition = spawnPositions[indicioIndex].position; 
        Instantiate(MegaRocas, spawnPosition, Quaternion.identity); 
        indicioIndex = (indicioIndex + 1) % spawnPositions.Length;

    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; foreach (Transform spawnPosition in spawnPositions) { Gizmos.DrawSphere(spawnPosition.position, 1f); }

    }
    
    private void OnTriggerExit(Collider other)
    {
    
       chequeo = false;
    
    }
    

}
