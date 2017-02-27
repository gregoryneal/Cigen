using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Defines which side of the road the plot will be placed on.
//Imagine standing at the roads parentNode and looking toward
//the childNode, PLOTLEFT is the left side of the road from 
//this viewpoint, PLOTRIGHT the right side.
public enum PlotRoadSide {
    PLOTLEFT,
    PLOTRIGHT,
}

public class Plot : MonoBehaviour {
    public Road road { get; private set; }
    public City city;
    public PlotRoadSide side;
    private Color c;
    public GameObject propertyBounds { get; private set; }

	public void Init(Road road, PlotRoadSide side) {
        transform.rotation = road.transform.rotation;
        transform.SetParent(road.transform, true);
        this.road = road;
        this.city = road.city;
        this.side = side;
        c = Random.ColorHSV();
        Build();
    }

    //(Re)Generates the propertyBounds GameObject
    public void CreatePropertyBounds() {
        if (propertyBounds != null) { 
            Destroy(propertyBounds);
            propertyBounds = null;
        }

        Renderer r = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
        r.material.color = c;
        r.transform.position = transform.position;
        r.transform.rotation = transform.rotation;
        r.transform.localScale = transform.localScale;
        r.transform.SetParent(transform, true);
        propertyBounds = r.gameObject;
    }

    public void Build() {
        Vector3 sideDirection = Vector3.Cross(road.direction, road.transform.up).normalized;
        if (side == PlotRoadSide.PLOTRIGHT) {
            sideDirection *= -1;
        }

        transform.position = road.Midpoint + (sideDirection * (city.settings.plotPadding + ((city.settings.maxPlotWidth + city.settings.roadDimensions.x) / 2f)));
        transform.localScale = new Vector3(city.settings.maxPlotWidth, Random.value * 0.5f, road.length - (2*city.settings.plotPadding));
        transform.rotation = Quaternion.LookRotation(road.direction);
        transform.SetParent(road.transform, true);
    }

    public bool PlaceBuilding(Building building) {
        Vector3 extents = building.obj.transform.localScale;
        Vector3 pos = RandomPosition();
        for (int i = 0; i < 10; i++) {
            if (!Physics.CheckBox(pos, extents/2f)) { //we can place the building here
                building.obj.transform.position = pos;
                return true;
            }
            pos = RandomPosition();
        }
        return false;
    }

    public Vector3 RandomPosition() {
        Bounds bounds = propertyBounds.GetComponent<MeshFilter>().mesh.bounds;
        float minX = propertyBounds.transform.position.x - propertyBounds.transform.localScale.x * bounds.size.x * 0.5f;
        float minZ = propertyBounds.transform.position.z - propertyBounds.transform.localScale.z * bounds.size.z * 0.5f;

        return new Vector3(Random.Range(-minX, minX), transform.position.y, Random.Range(-minZ, minZ));
    }
}
