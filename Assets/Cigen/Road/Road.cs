using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.Factories;

public class Road : MonoBehaviour {

	public City city { get; private set; }
	public Intersection parentNode;
	public Intersection childNode;
    public bool built { get; private set; }
    public float length { get; private set; }
    public Vector3 direction { get; private set; }    
    public Vector3 Midpoint {
        get {
           return Vector3.Lerp(parentNode.Position, childNode.Position, 0.5f); 
        }
    }
    public Plot leftPlot { get; private set; }
    public Plot rightPlot { get; private set; }

	public void Init(Intersection parent, Intersection child, City city) {
        if (parent == child) {
            Destroy(gameObject);
        }

		this.parentNode = parent;
		this.childNode = child;
		this.city = city;
        this.built = false;
        this.length = Vector3.Distance(parentNode.Position, childNode.Position);
        this.direction = (childNode.Position - parentNode.Position).normalized;
        transform.position = Midpoint;
        parent.AddRoad(this);
        child.AddRoad(this);
        parent.ConnectToIntersection(child);
        city.roads.Add(this);
        transform.parent = city.transform;
        StartCoroutine(Build());
	}

    public void Rebuild() {
        built = false;
        StartCoroutine(Build());
    }

    public IEnumerator Build() {
        if (built) {
            yield break;
        }

        Vector3 start = parentNode.Position + (direction * city.settings.roadDimensions.x/2f);
        Vector3 end = childNode.Position - (direction * city.settings.roadDimensions.x/2f);
          
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.GetComponent<Renderer>().material.mainTexture = city.settings.roadTexture;
        Vector3 btwn = end - start;
        float dist = btwn.magnitude;
        Vector3 targetLocalScale = new Vector3(city.settings.roadDimensions.x, city.settings.roadDimensions.y, dist);
        Vector3 targetPosition = start + (btwn / 2f);    
        cube.transform.LookAt(end);
        System.Action setEndState = () =>
        {
            cube.transform.localScale = targetLocalScale;
            cube.transform.position = targetPosition;
            cube.transform.LookAt(end);
        };

        if (city.settings.animateRoadBuilding) {
            float zScale = 0.1f;
            Vector3 initScale = city.settings.roadDimensions;
            initScale.z = zScale;
            cube.transform.localScale = initScale;
            float timeToBuild = dist / city.settings.roadBuildSpeed;
            float currTime = 0f;
            while (currTime < timeToBuild) {
                cube.transform.position = Vector3.Lerp(start, targetPosition, currTime/timeToBuild);
                zScale = Mathf.Lerp(0.1f, dist, currTime/timeToBuild);
                {
                    Vector3 currScale = cube.transform.localScale;
                    currScale.z = zScale;
                    cube.transform.localScale = currScale;
                }
                cube.transform.LookAt(end);
                yield return new WaitForEndOfFrame();
                currTime += Time.deltaTime;
            }
        }

        setEndState();
        cube.transform.parent = transform;
        ZonePlots();
        built = true;
        yield break;
    }

    public void ZonePlots() {
        if (length < city.settings.minimumRoadLength)
            return;

        if (this.leftPlot != null)
            Destroy(this.leftPlot.gameObject);
        if (this.rightPlot != null)
            Destroy(this.rightPlot.gameObject);

        Plot[] plots = CigenFactory.CreatePlots(this);
        this.leftPlot = plots[0];
        this.rightPlot = plots[1];
    }

    public void Remove() {
        parentNode.RemoveConnection(childNode);
        city.roads.Remove(this);
        if (leftPlot != null) { 
            city.plots.Remove(leftPlot);
            Destroy(leftPlot.gameObject);
        }
        if (rightPlot != null) { 
            city.plots.Remove(rightPlot);
            Destroy(rightPlot.gameObject);
        }
        Destroy(gameObject);
    }
}
