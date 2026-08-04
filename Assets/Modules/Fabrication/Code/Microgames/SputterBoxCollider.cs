using System.Collections;
using UnityEngine;

namespace SpaceFab.Fabrication.Microgames
{
    public class SputterBoxCollider : MonoBehaviour
    {
        public SputterPatternData SputterPattern;
        public BoxCollider2D Collider;
        private Vector2[,] SlotCenters;
        private bool[,] SlotFilled;
        private float ProjectileSize;

        public int GenerateSlot(float size)
        {
            float padding = 0.02f;
            size += padding;
            ProjectileSize = size;

            int cols = (int)(Collider.bounds.size.x / size);
            int rows = (int)(Collider.bounds.size.y / size);

            SlotCenters = new Vector2[rows, cols];
            float startX = -(cols - 1) * 0.5f * size;
            float startY =  (rows - 1) * 0.5f * size;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    SlotCenters[i, j] = new Vector2(
                        Collider.bounds.center.x + startX + j * size,
                        Collider.bounds.center.y + startY - i * size
                    );
                }
            }
            SlotFilled = new bool[rows, cols];
            return rows * cols;
        }

        // Checks if the projectile can fill a slot, fills it if possible, and returns whether a slot was filled.
        public bool TryFillSlot(GameObject projectile)
        {
            //projectile.transform.parent = this.transform;
            Vector3 projectilePos = projectile.transform.position;
            for (int i = 0; i < SlotCenters.GetLength(0); i++)
            {
                for (int j = 0; j < SlotCenters.GetLength(1); j++)
                {
                    if (ProjectileSize >= Vector2.Distance(projectilePos, SlotCenters[i, j]))
                    {
                        if (!SlotFilled[i, j])
                        {
                            projectile.transform.position = SlotCenters[i, j];
                            SlotFilled[i, j] = true;
                            SputterPattern.m_FilledSlots++;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}