using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float speed = 10.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical") * speed * Time.deltaTime);
        transform.Translate(moveVector);
    }
}
