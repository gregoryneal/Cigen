using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cigen.Factories;

public class Intersection : MonoBehaviour {
    
	public Vector3 Position { get { return _position; } }
    public List<Intersection> AllConnections {
        get {
            List<Intersection> ret = new List<Intersection>();
            if (parent != null)
                ret.Add(parent);
            if (children.Count > 0)
                ret.AddRange(children);
            return ret;
        }
    }
    private Vector3 _position;
    private Intersection parent = null;
    private List<Intersection> children = new List<Intersection>();
    public City city { get; private set; }
    public List<Road> roads { get; private set; }

    //Builds each road starting from this intersection and flowing down the tree
    //Doesn't work yet.
    public IEnumerator BuildRoadTree() {
        if (roads.Count > 0) { 
            List<Coroutine> roadRoutines = new List<Coroutine>();
            foreach (Road road in roads) {
                roadRoutines.Add(StartCoroutine(road.Build()));
            }
            List<Road> roadsToWatch = roads;
            while (roadsToWatch.Count > 0) {
                List<Road> toRemove = roadsToWatch.FindAll(r=>r.built);
                if (toRemove.Count > 0) { 
                    toRemove.ForEach(r=>r.childNode.StartCoroutine(r.childNode.BuildRoadTree()));
                    roadsToWatch.RemoveAll(r=>r.built);
                }

                yield return new WaitForEndOfFrame();
            }
        }

        yield break;
    }

    public void MoveIntersection(Vector3 newPosition) {
        _position = newPosition;
    }

    public void AddRoad(Road road) {
        if (roads == null)
            roads = new List<Road>();
        roads.Add(road);
    }

    public void Init(Vector3 position, City city) {
        _position = position;
        this.city = city;
        transform.parent = city.transform;
    }

    public void ConnectToIntersection(Intersection child) {
        children.Add(child);
        child.parent = this;
    }

    public void RemoveConnection(Intersection child) {
        child.parent = null;
        children.Remove(child);
        /*foreach (Road road in roads) {
            if (road.childNode == child) {
                Destroy(road.gameObject);
            }
        }*/
    }

    public static Intersection CreateIntersectionBetweenIntersections(Intersection int1, Intersection int2, Vector3 position) {
        Intersection intersectionNode = CigenFactory.CreateIntersection(position, int1.city);
        if (int1.parent == int2) { //we are further upstream, connect flow in one direction
            int2.RemoveConnection(int1);
            int2.ConnectToIntersection(intersectionNode);
            intersectionNode.ConnectToIntersection(int1);
        } else { //the other node is the parent, we are further downstream, connect flow in opposite direction
            int1.RemoveConnection(int2);
            int1.ConnectToIntersection(intersectionNode);
            intersectionNode.ConnectToIntersection(int2);
        }
        return intersectionNode;
    }
}