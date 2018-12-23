using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControll : MonoBehaviour
{
    public float minSize = 4f;
    public float maxSize = 20f;

    public float minX = -10f;
    public float maxX = 10f;

    public Camera cam;

	void Update ()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + Input.mouseScrollDelta.y * Time.deltaTime, minSize, maxSize);
        float moveX = 0f;
        if (Input.mousePosition.x < 30)
        {
            moveX = -10f;
        }
        else if (Input.mousePosition.x > Screen.width - 30)
        {
            moveX = 10f;
        }
        var x = transform.position.x + moveX * Time.deltaTime;
        x = Mathf.Clamp(x, minX, maxX);
        transform.position = new Vector3(x, transform.position.y, -10f);
    }
}
