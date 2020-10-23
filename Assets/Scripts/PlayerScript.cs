using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    private Rigidbody rb;
    public bool active = false;

    void Start() {
        rb = GetComponent<Rigidbody>();

        if (gameObject.name == FindObjectOfType<NetworkClient>().myID) {
            active = true;
        }
    }

    void Update() {
        if (active) {
            GetComponent<Renderer>().material.SetColor("_Color", Color.red);

            if (Input.GetKey("w")) {
                rb.AddForce(Vector3.forward);
            }
            else if (Input.GetKey("s")) {
                rb.AddForce(Vector3.back);
            }

            if (Input.GetKey("a")) {
                rb.AddForce(Vector3.left);
            }
            else if (Input.GetKey("d")) {
                rb.AddForce(Vector3.right);
            }
        }
    }
}
