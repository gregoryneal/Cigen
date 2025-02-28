using UnityEngine;
using OpenCvSharp;
using Cigen;
using Cigen.ImageAnalyzing;
using Cigen.Conversions;
using System;
using System.Collections.Generic;

/// <summary>
/// Attach this to a Terrain GameObject to enable pixel value detection when clicking on a point on the Terrain.
/// The pixel value that will be detected is the diffuseTexture on the first TerrainLayer object attached to the Terrain.
/// </summary>
public class TerrainHit : MonoBehaviour {
	public Collider terrainCollider;
    Mat material;
    private Vector3 point1 = Vector3.one * -1;
    private Vector3 point2 = Vector3.one * -1; 
    void Start() {
        terrainCollider = GetComponent<Terrain>().GetComponent<Collider>();
        Texture2D terrainTexture = GetComponent<Terrain>().terrainData.terrainLayers[0].diffuseTexture;
        material = FindFirstObjectByType<CityGenerator>().CVMaterials[terrainTexture];
    }

    void Update() {
        if (point1 != Vector3.one * -1 && point2 != Vector3.one * -1) {
            Debug.Log($"Running pathfinder! {point1} | {point2}");
            CitySettings.instance.cigen._RunPathfinder(point1, point2);
            point1 = Vector3.one * -1;
            point2 = Vector3.one * -1;
        } else {
            //left click
            if (Input.GetMouseButtonDown(0)) {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (terrainCollider.Raycast(ray, out hit, Mathf.Infinity)) {
                    CitySettings.instance.cigen.gameObjectPoint1.transform.position = hit.point;
                    point1 = hit.point;
                }
            }
            //right click
            if (Input.GetMouseButtonDown(1)) {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (terrainCollider.Raycast(ray, out hit, Mathf.Infinity)) {
                    CitySettings.instance.cigen.gameObjectPoint2.transform.position = hit.point;
                    point2 = hit.point;
                }
            }

        }
	}
}
