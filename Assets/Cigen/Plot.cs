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

    private Vector3 sideDirection;

	public void Init(Road road, PlotRoadSide side) {
        this.road = road;
        this.city = road.City;
        this.side = side;
        this.city.plots.Add(this);

        transform.parent = road.transform;
        Build();
    }

    public void Build() {
        /*
        sideDirection = Vector3.Cross(road.Direction, road.transform.up).normalized;
        if (side == PlotRoadSide.PLOTRIGHT) {
            sideDirection *= -1;
        }

        float halfRoadLength = road.Length / 2f;
        if (halfRoadLength < city.Settings.minPlotWidth) return;

        transform.position = road.transform.position +
                            (road.Direction * road.Length / 2f) + 
                            sideDirection * (city.Settings.plotPadding + ((city.Settings.plotWidth + city.Settings.roadDimensions.x) / 2f));
        transform.localScale = new Vector3(city.Settings.plotWidth, Random.value * 0.5f, Mathf.Abs(road.Length - (2*city.Settings.plotPadding)));
        transform.rotation = Quaternion.LookRotation(road.Direction);
        */
    }

    public bool PlaceBuilding(Building building) {
        Vector3 extents = building.GetComponent<Renderer>().bounds.extents;
        Vector3 pos = RandomPosition();
        for (int i = 0; i < 10; i++) {
            if (!Physics.CheckBox(pos, extents/2f, transform.rotation, LayerMask.NameToLayer("Default"), QueryTriggerInteraction.Collide)) { //we can place the building here
                building.obj.transform.position = pos;
                return true;
            }
            pos = RandomPosition();
        }
        return false;
    }


    public Vector3 RandomPosition() {
        float rnd1 = UnityEngine.Random.value;
        float rnd2 = UnityEngine.Random.value;
        /*Vector3 start = road.parentNode.Position + (road.Direction * city.Settings.plotPadding);
        Vector3 end = road.childNode.Position - (road.Direction * city.Settings.plotPadding);
        Vector3 pos = Vector3.Lerp(start, end, rnd1) + (sideDirection * (Mathf.Lerp(0, city.Settings.plotWidth, rnd2) + city.Settings.plotPadding + city.Settings.roadDimensions.x));
        return pos;*/
        return Vector3.zero;
    }
}
