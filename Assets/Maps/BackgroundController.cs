using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BackgroundParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    public bool autoScroll = false;
    public float autoScrollSpeed = 1f;
    
    [Header("Camera-based Parallax (when autoScroll is false)")]
    public GameObject camera;
    public float parallaxEffect;

    private float startPos, length;
    private float autoScrollOffset;

    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
        autoScrollOffset = 0f;
    }

    void FixedUpdate()
    {
        if (autoScroll)
        {
            // Auto-scroll mode for main menu - infinite loop
            autoScrollOffset += autoScrollSpeed * Time.fixedDeltaTime;
            
            // Keep offset looping infinitely using modulo
            if (autoScrollOffset > length)
            {
                autoScrollOffset = autoScrollOffset % length;
            }
            else if (autoScrollOffset < -length)
            {
                autoScrollOffset = autoScrollOffset % length;
            }
            
            transform.position = new Vector3(startPos + autoScrollOffset, transform.position.y, transform.position.z);
        }
        else
        {
            // Camera-based parallax for gameplay
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

}
