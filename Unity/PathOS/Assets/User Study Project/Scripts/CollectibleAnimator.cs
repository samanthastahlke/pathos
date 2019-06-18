using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleAnimator : MonoBehaviour
{
    public float ySpinSpeed = 0.0f;
    public float yOscillateSpeed = 0.0f;
    public float yOscillateMagnitude = 0.0f;
    public AnimationCurve yOscillateCurve;

    private float yMin, yMax;
    private bool reverse = false;
    private float oscillateTimer = 0.0f;
    private float oscillateTime;

    private Vector3 pos;

    void Start()
    {
        yMin = transform.position.y - 0.5f * yOscillateMagnitude;
        yMax = transform.position.y + 0.5f * yOscillateMagnitude;

        oscillateTime = 1.0f / yOscillateSpeed;
        pos = transform.position;
    }

    void Update()
    {
        oscillateTimer += Time.deltaTime;

        while(oscillateTimer > oscillateTime)
        {
            reverse = !reverse;
            oscillateTimer -= oscillateTime;
        }

        pos.y = Mathf.Lerp(
            (reverse) ? yMax : yMin,
            (reverse) ? yMin : yMax,
            yOscillateCurve.Evaluate(oscillateTimer / oscillateTime));

        transform.position = pos;

        transform.Rotate(Vector3.up, ySpinSpeed * Time.deltaTime, Space.World);  
    }
}
