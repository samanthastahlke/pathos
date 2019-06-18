using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
PathOSWorldCamera.cs 
PathOSWorldCamera (c) Nine Penguins (Samantha Stahlke) 2018
*/

public class PathOSWorldCamera : MonoBehaviour 
{
    public float scrollSpeed = 100.0f;
    public float panSpeed = 5.0f;

    private Vector3 mouseDelta;
    private Vector3 lastCursor;

    private bool regainedFocus = false;

    private void Start()
    {
        lastCursor = Input.mousePosition;
    }

    void Update() 
	{
        Vector3 delta = Vector3.zero;

        if (regainedFocus)
        {
            regainedFocus = false;
            lastCursor = Input.mousePosition; 
        }

        //Left click (0) or middle click (2) can be used for panning.
        if (Input.GetMouseButton(0) || Input.GetMouseButton(2))
            mouseDelta = Input.mousePosition - lastCursor;
        else
            mouseDelta = Vector3.zero;

        lastCursor = Input.mousePosition;

        delta.x -= Time.deltaTime * panSpeed * mouseDelta.x;
        delta.z -= Time.deltaTime * panSpeed * mouseDelta.y;
        delta.y -= Time.deltaTime * scrollSpeed * Input.GetAxisRaw("Mouse ScrollWheel");

        transform.position = transform.position + delta;
	}

    private void OnApplicationFocus(bool focus)
    {
        regainedFocus = focus;
    }
}
