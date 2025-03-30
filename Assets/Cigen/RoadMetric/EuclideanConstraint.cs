using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using System;

/// <summary>
/// No restriction on intersection placement or road placement
/// </summary>
public class EuclideanConstraint : MetricConstraint {
    public EuclideanConstraint(AnisotropicLeastCostPathSettings settings) : base(settings) { }
}
