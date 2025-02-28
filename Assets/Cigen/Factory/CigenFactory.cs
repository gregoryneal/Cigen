using UnityEngine;
using Cigen.MetricConstraint;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Cigen.Factories { 

    public class CigenFactory {
        /// <summary>
        /// Creates a straight line Road connected to two Intersections.
        /// Will not create a road that overlaps another road
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void CreateRoad(Intersection head, Intersection tail, string name = "Road") {
            City city = head.city;
            //check if this road is close to another road
            /*if (city.intersections.Where(i => (i.Position - head.Position).magnitude <= city.settings.maxIntersectionMergeRadius).Count() > 0 ||
                city.intersections.Where(i => (i.Position - tail.Position).magnitude <= city.settings.maxIntersectionMergeRadius).Count() > 0) {
                    return;
            }*/
            /*
            if (city.intersections.Where(i => i.Position == head.Position).Count() > 0 ||
                city.intersections.Where(i => i.Position == tail.Position).Count() > 0) {
                    return;
            }*/
            Road road = new GameObject(name).AddComponent<Road>();
            road.Init(head, tail, head.city);
            return;
        }

        /// <summary>
        /// Found yourself a brand spankin' new city! Make sure to give it some settings
        /// so it knows how to build itself.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="settings"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static City CreateCity(Vector3 position, CitySettings settings, string name = "City") {
            City temp = new GameObject(name).AddComponent<City>();
            temp.Init(position, settings);
            return temp;
        }

        /// <summary>
        /// Zones a plot along both sides of a given Road.
        /// </summary>
        /// <param name="road"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Plot[] CreatePlots(Road road, string name = "Plot") {
            BoxCollider bc;

            Plot[] plots = new Plot[2];
            Plot plot = new GameObject(name, typeof(BoxCollider)).gameObject.AddComponent<Plot>();
            plot.Init(road, PlotRoadSide.PLOTLEFT);
            plots[0] = plot;
            bc = plot.GetComponent<BoxCollider>();
            bc.isTrigger = true;

            plot = new GameObject(name, typeof(BoxCollider)).AddComponent<Plot>();
            plot.Init(road, PlotRoadSide.PLOTRIGHT);
            plots[1] = plot;
            bc = plot.GetComponent<BoxCollider>();
            bc.isTrigger = true;            

            return plots;
        }

        /// <summary>
        /// Creates a Building on a Plot, currently does nothing.
        /// </summary>
        /// <param name="plot"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Building CreateBuilding(Plot plot, string name = "Building") {
            Building b = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Building>();
            b.Init(plot);
            return b;
        }
    }

    public class MetricFactory {
        /// <summary>
        /// Convert an enum MetricSpace to it's corresponding MetricConstraint.
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static MetricConstraint.MetricConstraint Process(MetricSpace metric, CitySettings settings) {
            MetricConstraint.MetricConstraint m = null;

            switch (metric) {
                case MetricSpace.EUCLIDEAN:
                    m = new EuclideanConstraint(settings);
                    break;
                case MetricSpace.MANHATTAN:
                    m = new ManhattanConstraint(settings);
                    break;
                case MetricSpace.GRID:
                    m = new GridConstraint(settings);
                    break;
            }

            Debug.Log(metric);

            return m;
        }
    }
}
