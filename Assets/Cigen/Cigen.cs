using System.Collections;
using UnityEngine;
using Cigen.Factories;
using System.Collections.Generic;

namespace Cigen
{
    public class Cigen : MonoBehaviour {

    #region Variables

    #region Public
    public CitySettings settings;
    public Cigen waitUntilBuilt;
    public bool isBuilt = false;
    #endregion

    #region Private
    private City city;
    #endregion

    #endregion

    #region Unity Methods

    void Start() {
        StartCoroutine(BuildCity(transform.position));
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    #endregion

    public IEnumerator BuildCity(Vector3 initState) {
        if (waitUntilBuilt != null) {
            yield return new WaitUntil(()=>waitUntilBuilt.isBuilt);
        }
        
        if (settings == null) {
            Debug.LogError("No CigenSettings detected, drag and drop one into the inspector.");
            yield break;
        }

        //print("building city");
        int it = 0;
        city = CigenFactory.CreateCity(initState, settings);
        while (true) {
            it++;
            if (city.roads.Count >= settings.maxNumberOfRoads) {
                break;
            }
            
            //print("Starting next iteration...");
            city.AddRandomIntersectionToRoadNetwork();
            yield return new WaitForEndOfFrame();
        }

        //Because new roads can make an intersection need rebuilding, lets
        //wait until we finish generating every road before we generate intersection meshes.
        foreach (Intersection i in city.intersections) {
            yield return StartCoroutine(i.BuildMesh());
        }

        foreach (Road r in city.roads) {
            r.ZonePlots();
            yield return new WaitForEndOfFrame();
        }

        print("Intersections: " + city.intersections.Count);
        print("Roads: " + city.roads.Count);
        print("Plots: " + city.plots.Count);
        print("Iterations: " + it);
        this.isBuilt = true;
        yield break;
    }

    private void ConnectNodes(Intersection parent, Intersection child) {
        CigenFactory.CreateRoad(parent, child);
        //DrawLineBetweenPoints(parent.Position, child.Position);

    }

    #region Math methods

    #region Metric constraint methods

    #endregion

    #endregion
}
}
