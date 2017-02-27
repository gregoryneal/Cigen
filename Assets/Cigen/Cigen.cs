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
    #endregion

    #region Private
    private City city;
    #endregion

    #endregion

    #region Unity Methods

    void Start() {
        StartCoroutine(BuildCity(settings.initialCityPosition));
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    #endregion

    public IEnumerator BuildCity(Vector3 initState) {
        if (settings == null) {
            Debug.LogError("No CigenSettings detected, drag and drop one into the inspector.");
            yield break;
        }

        //print("building city");
        int it = 0;
        transform.position = initState;
        city = CigenFactory.CreateCity(initState, settings);
        while (true) {
            it++;
            if (city.intersections.Count >= settings.maxNumberOfIntersections || it > settings.maxNumberOfIntersections * settings.intersectionPlacementAttempts) {
                break;
            }
            
            print("Starting next iteration...");
            city.AddRandomIntersectionToRoadNetwork();            
            yield return new WaitForEndOfFrame();
            //yield return new WaitUntil(()=>Input.GetKeyDown(KeyCode.Space));
        }
        
        while (true) {
            if (city.buildings.Count >= settings.numBuildings)
                break;
            CigenFactory.CreateBuilding(city.RandomPlot());
            yield return new WaitForEndOfFrame();
        }

        print("Intersections: " + city.intersections.Count);
        print("Roads: " + city.roads.Count);
        print("Plots: " + city.plots.Count);
        print("Iterations: " + it);
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
