using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BackgroundParallax : MonoBehaviour
{
    private float startPos, length;
    public GameObject camera;
    public float parallaxEffect;

    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float movement = camera.transform.position.x * (1 - parallaxEffect);
        float dist = camera.transform.position.x * parallaxEffect;

        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        if (movement > startPos + length)
        {
            startPos += length;
        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }

}
