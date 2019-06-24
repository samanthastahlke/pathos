using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
*/

public class BasicFPController : MonoBehaviour
{
    public Camera cam;
    public bool invertY = true;
    public float speed = 8.0f;
    public float minPitch = -30.0f;
    public float maxPitch = 20.0f;
    public float camSpeed = 3.5f;

    private float yFac = 1.0f;
    private float pitch = 0.0f;
    private bool cursorLocked = false;

    void Start()
    {
        yFac = (invertY) ? -1.0f : 1.0f;
        ToggleCursor();
    }

    void Update()
    {
        Vector3 forward = transform.forward;
        forward.y = 0.0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0.0f;
        right.Normalize();

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
        input.Normalize();

        transform.position += input.z * forward * speed * Time.deltaTime;
        transform.position += input.x * right * speed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleCursor();

        if (cursorLocked)
        {
            Vector3 mouse = new Vector3(Input.GetAxis("Mouse X"),
            yFac * Input.GetAxis("Mouse Y"), 0.0f);

            mouse.Normalize();

            transform.Rotate(Vector3.up, mouse.x * camSpeed);
            pitch += mouse.y * camSpeed;

            if (pitch < minPitch)
                pitch = minPitch;

            else if (pitch > maxPitch)
                pitch = maxPitch;

            Vector3 euler = Vector3.zero;
            euler.x = pitch;
            cam.transform.localEulerAngles = euler;
        }
        
    }

    void ToggleCursor()
    {
        cursorLocked = !cursorLocked;

        Cursor.visible = !cursorLocked;
        Cursor.lockState = (cursorLocked) ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
