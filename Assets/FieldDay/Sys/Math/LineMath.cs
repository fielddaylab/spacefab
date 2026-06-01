using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Mathematics {
    static public class LineMath {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float DistanceFromPointToLineSegment(Vector2 point, Vector2 lineA, Vector2 lineB) {
            return (point - ClosestPointOnLineSegment(point, lineA, lineB)).magnitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float DistanceFromPointToLineSegmentSquared(Vector2 point, Vector2 lineA, Vector2 lineB) {
            return Vector2.SqrMagnitude(point - ClosestPointOnLineSegment(point, lineA, lineB));
        }

        /// <summary>
        /// Returns the closest point on a given line segment to the given point.
        /// </summary>
        static public Vector2 ClosestPointOnLineSegment(Vector2 point, Vector2 lineA, Vector2 lineB) {
            Vector2 ab = lineB - lineA;
            Vector2 ap = point - lineA;

            float abMagSq = Math.Max(ab.sqrMagnitude, Mathf.Epsilon);
            float projected = Math.Clamp(Vector2.Dot(ap, ab) / abMagSq, 0, 1);
            return new Vector2(
                lineA.x + ab.x * projected,
                lineA.y + ab.y * projected
            );
        }
    }
}