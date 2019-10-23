using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public static class VectorExtension
{
    public static Vector2 GetParallelComponent(this Vector2 vec, Vector2 unitBasis)
    {
        return Vector2.Dot(vec, unitBasis) * unitBasis;
    }
    public static Vector2 GetVerticalComponent(this Vector2 vec, Vector2 unitBasis)
    {
        return vec - vec.GetParallelComponent(unitBasis);
    }
}
