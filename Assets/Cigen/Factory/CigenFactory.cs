using UnityEngine;
using Cigen.MetricConstraint;

namespace Cigen.Factories { 
    public class CigenFactory {
        /// <summary>
        /// Creates a straight line Road connected to two Intersections.
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Road CreateRoad(Intersection head, Intersection tail, string name = "Road") {
            Road road = new GameObject(name).AddComponent<Road>();
            road.Init(head, tail, head.city);
            return road;
        }

        /// <summary>
        /// Creates a path of Roads constraining to the city metric between two Intersections.
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        /// <returns></returns>
        public static RoadPath CreatePath(Intersection head, Intersection tail) {
            RoadPath rp = new RoadPath(head, tail);
            rp.BuildPath();
            return rp;
        }

        /// <summary>
        /// Searches for an Intersection near a position, the search radius is
        /// given by CigenSettings.maxIntersectionMergeRadius, if none are found
        /// it creates a new Intersection at the position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="city"></param>
        /// <param name="name"></param>
        /// <returns></returns>
	    public static Intersection CreateOrMergeIntersection(Vector3 position, City city, string name = "Intersection") {
            Intersection c = city.CreateOrMergeNear(position);
            c.name = name;
            return c;
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
            return m;
        }
    }
}
