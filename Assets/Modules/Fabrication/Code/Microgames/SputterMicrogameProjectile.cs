using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SputterMicrogameProjectile : MonoBehaviour
{
    private Rigidbody2D Rigidbody;
    private float Speed = 5f;
    private float InitialAngle = 0;

    public void SetDirection(float angle)
    {
        Rigidbody = this.GetComponent<Rigidbody2D>();
        InitialAngle = angle;
        Vector2 direction = Quaternion.Euler(0, 0, InitialAngle) * Vector2.right;
        Rigidbody.velocity = direction * Speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Mirror")
        {
            Vector2 direction = Quaternion.Euler(0, 0, -InitialAngle) * Vector2.right;
            Rigidbody.velocity = direction * Rigidbody.velocity;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
