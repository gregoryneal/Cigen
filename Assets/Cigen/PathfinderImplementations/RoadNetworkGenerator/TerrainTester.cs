using Cigen;
using Cigen.ImageAnalyzing;
using UnityEngine;

public class TerrainTester : MonoBehaviour {

    public Camera camera;
	public Collider terrainCollider;
    private AnisotropicLeastCostPathSettings settings;    
    private int resolution;
    public int pathPriority = 0;
    void Start()
    {
        camera = Camera.main;
        terrainCollider = GetComponent<Terrain>().GetComponent<Collider>();
        Texture2D terrainTexture = GetComponent<Terrain>().terrainData.terrainLayers[0].diffuseTexture;
        this.settings = FindFirstObjectByType<RoadNetworkGenerator>().settings;
        resolution = settings.GetSegmentMaskResolution(pathPriority) * settings.GetSegmentMaskValue(pathPriority);
    } 

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (terrainCollider.Raycast(ray, out hit, Mathf.Infinity)) {
                int newX = Mathf.RoundToInt(hit.point.x / this.resolution) * this.resolution;
                int newZ = Mathf.RoundToInt(hit.point.z / this.resolution) * this.resolution;
                Vector3 newPoint = new Vector3(newX, hit.point.y, newZ);
                settings.cigen.gameObjectPoint1.transform.position = newPoint;

                float y = ImageAnalysis.TerrainHeightAt(newPoint, settings);
                Debug.Log($"TerrainHeight at {newPoint} => {y}");
            }
        }
    }
}