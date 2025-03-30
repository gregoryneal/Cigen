using System;
using System.Collections.Generic;
using GeneralPathfinder;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class RoadSegmentRecord {
    
    [SerializeField] public Vector3 start;
    [SerializeField] public Vector3 end;
    [SerializeField] public int priority;
    [SerializeField] public bool startIsHead;
    [SerializeField] public bool endIsHead;
    [SerializeField] public bool isBridge;
    [SerializeField] public bool isTunnel;

    public RoadSegmentRecord(Vector3 start, Vector3 end, int priority, bool startIsHead, bool endIsHead, bool isBridge, bool isTunnel) {
        this.start = start;
        this.end = end;
        this.priority = priority;
        this.startIsHead = startIsHead;
        this.endIsHead = endIsHead;
        this.isBridge = isBridge;
        this.isTunnel = isTunnel;
    }
}

[Serializable]
public class RoadRecord {
    [SerializeField] List<RoadSegmentRecord> segments;
    public RoadRecord(List<RoadSegmentRecord> records) {
        this.segments = records;
    }
}