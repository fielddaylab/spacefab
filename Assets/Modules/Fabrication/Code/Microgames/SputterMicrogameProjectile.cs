using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public class SputterMicrogameProjectile : MonoBehaviour
    {
        public SpriteRenderer Sprite;
        public Rigidbody2D Rigidbody;
        private float Speed = 5f;
        private float InitialAngle = 0;
        private bool Reflected = false;

        public void SetDirection(float angle)
        {
            InitialAngle = angle;
            Vector2 direction = Quaternion.Euler(0, 0, InitialAngle) * Vector2.right;
            Rigidbody.velocity = direction * Speed;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.name == "Mirror")
            {
                Vector2 direction = Quaternion.Euler(0, 0, -InitialAngle) * Vector2.right;
                Rigidbody.velocity = direction * Speed;
                Reflected = true;
            }
            else if (collision.gameObject.GetComponent<SputterBoxCollider>() == null)
            {
                Destroy(this.gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!Reflected) { return; }

            SputterBoxCollider collider = collision.gameObject.GetComponent<SputterBoxCollider>();
            if (collider != null)
            {
                if (collider.TryFillSlot(this.gameObject))
                {
                    Rigidbody.velocity = Vector2.zero;
                    Rigidbody.simulated = false;
                }
            }
        }
    }
}