using UnityEngine;
using System.Collections.Generic;

public class HeatmapGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform trackedObject; // The AI agent
    [SerializeField] private Material heatmapMaterial;

    [Header("Heatmap Settings")]
    [SerializeField] private Vector2 mapSize = new Vector2(30f, 30f);
    [SerializeField] private int gridResolution = 50; // 50x50 grid
    [SerializeField] private float updateInterval = 0.1f; // Sample every 0.1s
    [SerializeField] private bool showHeatmap = true;

    [Header("Colors")]
    [SerializeField] private Color coldColor = Color.blue;
    [SerializeField] private Color hotColor = Color.red;

    private int[,] heatmapData;
    private float nextUpdateTime;
    private Texture2D heatmapTexture;
    private GameObject heatmapPlane;
    private int maxHeat = 1;

    void Start()
    {
        InitializeHeatmap();
    }

    void InitializeHeatmap()
    {
        // Create 2D array for heatmap data
        heatmapData = new int[gridResolution, gridResolution];

        // Create texture
        heatmapTexture = new Texture2D(gridResolution, gridResolution);
        heatmapTexture.filterMode = FilterMode.Bilinear;

        // Create plane to display heatmap
        heatmapPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        heatmapPlane.name = "Heatmap_Display";
        heatmapPlane.transform.position = new Vector3(0, 0.05f, 0); // Slightly above ground
        heatmapPlane.transform.localScale = new Vector3(mapSize.x / 10f, 1, mapSize.y / 10f);

        // Apply material
        if (heatmapMaterial != null)
        {
            heatmapPlane.GetComponent<Renderer>().material = heatmapMaterial;
            heatmapMaterial.mainTexture = heatmapTexture;
        }

        // Disable collider
        Destroy(heatmapPlane.GetComponent<Collider>());

        heatmapPlane.SetActive(showHeatmap);
    }

    void Update()
    {
        if (!showHeatmap || trackedObject == null) return;

        // Record position at intervals
        if (Time.time >= nextUpdateTime)
        {
            RecordPosition(trackedObject.position);
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    void RecordPosition(Vector3 worldPos)
    {
        // Convert world position to grid coordinates
        int x = Mathf.RoundToInt(((worldPos.x + mapSize.x / 2f) / mapSize.x) * gridResolution);
        int z = Mathf.RoundToInt(((worldPos.z + mapSize.y / 2f) / mapSize.y) * gridResolution);

        // Clamp to grid bounds
        x = Mathf.Clamp(x, 0, gridResolution - 1);
        z = Mathf.Clamp(z, 0, gridResolution - 1);

        // Increment heat at this position
        heatmapData[x, z]++;

        // Track max heat for normalization
        if (heatmapData[x, z] > maxHeat)
        {
            maxHeat = heatmapData[x, z];
        }

        // Update texture
        UpdateHeatmapTexture();
    }

    void UpdateHeatmapTexture()
    {
        for (int x = 0; x < gridResolution; x++)
        {
            for (int z = 0; z < gridResolution; z++)
            {
                // Normalize heat value (0 to 1)
                float normalizedHeat = (float)heatmapData[x, z] / maxHeat;

                // Interpolate color based on heat
                Color pixelColor = Color.Lerp(coldColor, hotColor, normalizedHeat);

                // Set alpha based on heat (transparent if no visits)
                if (heatmapData[x, z] == 0)
                {
                    pixelColor.a = 0f;
                }
                else
                {
                    pixelColor.a = 0.5f + (normalizedHeat * 0.5f); // 0.5 to 1.0 alpha
                }

                heatmapTexture.SetPixel(x, z, pixelColor);
            }
        }

        heatmapTexture.Apply();
    }

    public void ClearHeatmap()
    {
        heatmapData = new int[gridResolution, gridResolution];
        maxHeat = 1;
        UpdateHeatmapTexture();
    }

    public void ToggleHeatmap()
    {
        showHeatmap = !showHeatmap;
        if (heatmapPlane != null)
        {
            heatmapPlane.SetActive(showHeatmap);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw heatmap bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, 0.1f, mapSize.y));
    }
}