using System.Collections;
using System.Collections.Generic;
using BeauUtil;
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

    [SortingLayer] public int SortingLayer = 0;
    public int OrderInLayer = 0;

    public float baseScale = 1f;

    public int layer;
    public uint renderingLayerMask;
    public int rendererPriority;

    public struct IonPoint
    {
        public Vector2 Position;
        public Matrix4x4 Matrix;
        public bool IsFilled;
    }

    public IonPoint[] ionPoints;

    void Start()
    {
        Setup();
    }

    public void Setup()
    {
        Debug.Log("Sorting layer set to: " + SortingLayer);
        
        ionPoints = new IonPoint[Rows * Columns];

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Vector2 bottomLeft = m_BoxCollider.bounds.min;
                float cellWidth = Width / Columns;
                float cellHeight = Height / Rows;

                Vector3 cellCenter = bottomLeft + new Vector2(x * cellWidth, y * cellHeight);
               cellCenter.z = -0.1f; // stops z fighting or getting completely masked out

                //GameObject point = Instantiate(m_PointPrefab, cellCenter, Quaternion.identity);
                //point.transform.localScale = new Vector3(cellWidth, cellHeight, 1f);

                ionPoints[y * Columns + x] = new IonPoint()
                {
                    Position = cellCenter,
                    Matrix = Matrix4x4.TRS(
                        cellCenter, // position of graphic within center of cell
                        Quaternion.Euler(0, 180, 0), // circle mesh used to render a point is incorrectly flipped around for some reason
                        1f * Vector3.one // scale for now, will change later
                        ),
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
        renderParams.layer = layer;
        renderParams.renderingLayerMask = renderingLayerMask;
        renderParams.rendererPriority = rendererPriority;
        //Transform cameraTransform = Game.Rendering.PrimaryCamera.transform;
        Mesh mesh = IonMesh;
        var instanceHelper = new InstancedMeshBuffer<DefaultInstancedMeshParams>(paramBuffer, 512, renderParams, mesh);
        
        for (int i = 0; i < ionPoints.Length; i++)
        {
            if (ionPoints[i].IsFilled)
            {
                RenderPoint(ionPoints[i].Matrix, ref instanceHelper);
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
