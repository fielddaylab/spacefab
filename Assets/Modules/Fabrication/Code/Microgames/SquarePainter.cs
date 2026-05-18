using System.Collections;
using System.Collections.Generic;
using FieldDay;
using FieldDay.Rendering;
using UnityEngine;

public class SquarePainter : MonoBehaviour
{
    [SerializeField] private BoxCollider2D m_BoxCollider;
    public int Rows, Columns;
    public float Height, Width;

    public Mesh IonMesh;
    public Material IonMaterial;

    public float baseScale = 1f;

    public struct IonPoint
    {
        public Vector2 Position;
        public float Size;
        public float Percentage;
        public bool IsFilled;
        public Matrix4x4 matrix;
    }

    public IonPoint[] ionPoints;

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        ionPoints = new IonPoint[Rows * Columns];

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Vector2 bottomLeft = m_BoxCollider.bounds.min;
                float cellWidth = Width / Columns;
                float cellHeight = Height / Rows;

                Vector3 cellCenter = bottomLeft + new Vector2(x * cellWidth, y * cellHeight);
                cellCenter.z = -1f;

                //GameObject point = Instantiate(m_PointPrefab, cellCenter, Quaternion.identity);
                //point.transform.localScale = new Vector3(cellWidth, cellHeight, 1f);

                ionPoints[y * Columns + x] = new IonPoint()
                {
                    Position = cellCenter,
                    matrix = Matrix4x4.TRS(cellCenter, Quaternion.identity, 1f * Vector3.one),
                    IsFilled = false
                };
            }
        }
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Game.Rendering.PrimaryCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("Box clicked at " + hit.point);

                Vector2 bottomLeft = hit.collider.bounds.min;
                Vector2 localClick = mousePosition - bottomLeft;

                int xPosition = Mathf.FloorToInt((localClick.x / Width) * Columns);
                int yPosition = Mathf.FloorToInt((localClick.y / Height) * Rows);

                Debug.Log("Corresponding to square at " + xPosition + ", " + yPosition);

                int positionalIndex = yPosition * Columns + xPosition;    
                if (positionalIndex >= 0 && positionalIndex < ionPoints.Length)
                {
                    ionPoints[positionalIndex].IsFilled = true;
                }

                Debug.Log("Leading to index of " + positionalIndex);
            }
        }
    }

    void LateUpdate()
    {
        PerformRendering();
    }

    unsafe void PerformRendering()
    {
        DefaultInstancedMeshParams* paramBuffer = stackalloc DefaultInstancedMeshParams[512]; 
        RenderParams renderParams = new RenderParams(IonMaterial);
        //Transform cameraTransform = Game.Rendering.PrimaryCamera.transform;
        Mesh mesh = IonMesh;
        var instanceHelper = new InstancedMeshBuffer<DefaultInstancedMeshParams>(paramBuffer, 512, renderParams, mesh);
        
        for (int i = 0; i < ionPoints.Length; i++)
        {
            if (ionPoints[i].IsFilled)
            {
                RenderPoint(ionPoints[i].matrix, ref instanceHelper);
            }
        }

        instanceHelper.Flush();
        instanceHelper.Dispose();
    }

    void RenderPoint(Matrix4x4 matrix, ref InstancedMeshBuffer<DefaultInstancedMeshParams> instancing)
    {
        DefaultInstancedMeshParams instParams = default;
        instParams.objectToWorld = matrix;
        instancing.Queue(instParams);
    }
}
