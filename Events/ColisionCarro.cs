using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColisionCarro : MonoBehaviour

{
    public float radioRespawn = 10f;
    public float Delay = 2f; 

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstaculo") || collision.gameObject.CompareTag("Limite"))
        {
            StartCoroutine(RespawnCar(collision.contacts[0].point));
        }
    }

    IEnumerator RespawnCar(Vector3 collisionPoint)
    {
        yield return new WaitForSeconds(Delay);

        Vector3 respawnPosition = collisionPoint + (Random.insideUnitSphere * radioRespawn);
        respawnPosition.y = transform.position.y;

        transform.position = respawnPosition;
        transform.rotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

    }
}

