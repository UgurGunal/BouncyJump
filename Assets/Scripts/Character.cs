using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float speed = 15f;
    private Rigidbody2D rb;
    private float moveX;
    public float var = 0f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        moveX = Input.GetAxis("Horizontal") * speed;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveX + rb.velocity.x * 0.5f, rb.velocity.y + Mathf.Abs(rb.velocity.x) * var * Time.fixedDeltaTime);
    }
}
