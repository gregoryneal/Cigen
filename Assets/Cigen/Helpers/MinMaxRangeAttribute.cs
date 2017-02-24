using UnityEngine;

public class MinMaxRangeAttribute : PropertyAttribute {
    public float min;
    public float max;

    public MinMaxRangeAttribute(float from, float to) {
        this.min = from;
        this.max = to;
    }
}

[System.Serializable]
public class MinMaxRange {
    public float start;
    public float end;

    public float RandomValue() {
        return Random.Range(start, end);
    }
}