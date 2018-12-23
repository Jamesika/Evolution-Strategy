using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForceToMouse : MonoBehaviour {

    public Rigidbody2D rb2d;
    public float force = 100f; 

	// Update is called once per frame
	void FixedUpdate () 
	{
        if (Input.GetMouseButtonDown(0))
        {
            var mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            rb2d.AddForce((mPos - transform.position).normalized * force);
        }
	}
}
