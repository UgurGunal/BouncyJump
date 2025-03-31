using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paralax : MonoBehaviour
{
    public GameObject cam;
    public float paralaxEffect;
    private float length, startPos;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position.y;
        length = GetComponent<SpriteRenderer>().bounds.size.y;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float relativeDist = (cam.transform.position.y * (1 - paralaxEffect));
        float dist = (cam.transform.position.y * paralaxEffect);
        transform.position = new Vector3(transform.position.x, startPos + dist, transform.position.z);
        if (relativeDist > startPos + length/3)
        {
            startPos += length/2;
        }
    }
}
