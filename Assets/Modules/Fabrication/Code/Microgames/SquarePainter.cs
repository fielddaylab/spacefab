using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Rendering;
using UnityEngine;

public class SquarePainter : MonoBehaviour
{
    [SerializeField] private BoxCollider2D m_BoxCollider;

    private int m_Rows, m_Columns;
    private float m_CellWidth, m_CellHeight;

    [SerializeField] private float m_Density;
    
    public Mesh IonMesh;
    public Material IonMaterial;

    public struct IonPoint
    {
        public Vector2 Position;
        public Matrix4x4 Matrix;
        public bool IsFilled;
    }

    private IonPoint[] IonPoints;

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        Vector2 bottomLeft = m_BoxCollider.bounds.min;
        Vector2 topRight = m_BoxCollider.bounds.max;
        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;
    
        m_Columns = Mathf.FloorToInt(width * m_Density);
        m_Rows = Mathf.FloorToInt(height * m_Density);

        // these should be the same but just in case
        m_CellWidth = width / m_Columns;
        m_CellHeight = height / m_Rows;
        float halfWidth = m_CellWidth * 0.5f;
        float halfHeight = m_CellHeight * 0.5f;

        Vector3 cellScale = new Vector3(m_CellWidth, m_CellHeight, 1);
        Vector3 forwardCamera = Game.Rendering.PrimaryCamera.transform.forward;
        Quaternion cellRotation = Quaternion.LookRotation(-forwardCamera, Vector3.up); // drawing to face the camera, should port specific line from bloom
        
        IonPoints = new IonPoint[m_Rows * m_Columns];

        for (int y = 0; y < m_Rows; y++)
        {
            for (int x = 0; x < m_Columns; x++)
            {
                Vector3 cellCenter = new Vector3(
                    bottomLeft.x + (x * m_CellWidth) + halfWidth,
                    bottomLeft.y + (y * m_CellHeight) + halfHeight,
                    -0.1f // place slightly towards camera to fix some rendering issues?
                );

                IonPoints[y * m_Columns + x] = new IonPoint()
                {
                    Position = cellCenter,
                    Matrix = Matrix4x4.TRS(cellCenter, cellRotation, cellScale),
                    IsFilled = false
                };
            }
        }
    }
    
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Game.Rendering.PrimaryCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider == m_BoxCollider)
            {
                Debug.Log("Box clicked at " + hit.point);

                Vector2 bottomLeft = hit.collider.bounds.min;
                Vector2 localClick = mousePosition - bottomLeft;

                int xPosition = Mathf.FloorToInt(localClick.x / m_CellWidth);
                int yPosition = Mathf.FloorToInt(localClick.y / m_CellHeight);

                Debug.Log("Corresponding to square at " + xPosition + ", " + yPosition);

                int positionalIndex = yPosition * m_Columns + xPosition;    
                if (positionalIndex >= 0 && positionalIndex < IonPoints.Length)
                {
                    IonPoints[positionalIndex].IsFilled = true;
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
        Mesh mesh = IonMesh;
        var instanceHelper = new InstancedMeshBuffer<DefaultInstancedMeshParams>(paramBuffer, 512, renderParams, mesh);
        
        for (int i = 0; i < IonPoints.Length; i++)
        {
            if (IonPoints[i].IsFilled)
            {
                DefaultInstancedMeshParams instParams = default;
                instParams.objectToWorld = IonPoints[i].Matrix;
                instanceHelper.Queue(instParams);
            }
        }

        instanceHelper.Flush();
        instanceHelper.Dispose();
    }
}
