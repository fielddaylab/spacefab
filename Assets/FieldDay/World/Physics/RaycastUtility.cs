using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TinyIL;
using UnityEngine;

namespace FieldDay.Physics {
    static public class RaycastUtility {
        /// <summary>
        /// Fills a preallocated array with intersection data between the two given points.
        /// </summary>
        /// <param name="pointA">Start point</param>
        /// <param name="pointB">End point</param>
        /// <param name="filter">Filter to use for contact testing</param>
        /// <param name="results">Preallocated result buffer</param>
        /// <param name="buffers">Temporary raycast buffers</param>
        /// <returns>Number of unique intersections</returns>
        static public int LinecastIntersections2D(Vector2 pointA, Vector2 pointB, in ContactFilter2D filter, RaycastIntersection2D[] results, Raycast2DBuffers buffers) {
            Assert.NotNull(results);
            Assert.True(results.Length > 0, "Results must not be zero length");

            Assert.NotNull(buffers.BufferA, "BufferA must not be null");
            Assert.NotNull(buffers.BufferB, "BufferB must not be null");

            Assert.True(buffers.BufferA.Length == buffers.BufferB.Length && buffers.BufferA.Length > 0, "Buffers must not be zero length");

            int forwardCount = Physics2D.Linecast(pointA, pointB, filter, buffers.BufferA);
            if (forwardCount == 0) {
                return 0;
            }

            Assert.True(forwardCount <= results.Length, "Not enough room in results buffer (length {0}) for {1} intersections", results.Length, forwardCount);

            float distance = Vector2.Distance(pointA, pointB);

            int reverseCount = Physics2D.Linecast(pointB, pointA, filter, buffers.BufferB);

            Array.Sort(buffers.BufferA, 0, forwardCount, RaycastHitDistanceSorter2D.Instance);
            Array.Sort(buffers.BufferB, 0, reverseCount, RaycastHiReverseDistanceSorter2D.Instance);

            const float epsilon = 1f / 1024;

            int count = 0;
            for(int forwardIndex = 0; forwardIndex < forwardCount; forwardIndex++) {
                RaycastHit2D forwardHit = buffers.BufferA[forwardIndex];
                int hitColliderId = GetColliderId(forwardHit);
                float forwardDistance = forwardHit.distance;
                
                RaycastIntersection2D intersection;
                intersection.ColliderId = hitColliderId;
                intersection.EnterDistance = forwardDistance;
                intersection.ExitDistance = -1;
                intersection.Flags = 0;
                if (forwardHit.distance <= epsilon) {
                    intersection.Flags |= RaycastIntersectionFlags.IntersectsStart;
                }

                float reversedDistanceThreshold = distance - forwardDistance;
                for(int reverseIndex = 0; reverseIndex < reverseCount; reverseIndex++) {
                    RaycastHit2D reverseHit = buffers.BufferB[reverseIndex];
                    float reverseDistance = reverseHit.distance;
                    if (GetColliderId(reverseHit) == hitColliderId && reverseDistance <= reversedDistanceThreshold) {
                        intersection.ExitDistance = distance - reverseDistance;
                        if (reverseDistance <= epsilon) {
                            intersection.Flags |= RaycastIntersectionFlags.IntersectsEnd;
                        }
                        break;
                    }
                }
                results[count++] = intersection;
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; ldfld [arg hit]::m_Collider; ret")]
        static private int GetColliderId(RaycastHit2D hit) {
            throw new NotImplementedException();
        }

        private class RaycastHitDistanceSorter2D : IComparer<RaycastHit2D> {
            static internal readonly RaycastHitDistanceSorter2D Instance = new RaycastHitDistanceSorter2D();

            public int Compare(RaycastHit2D x, RaycastHit2D y) {
                return x.distance.CompareTo(y.distance);
            }
        }

        private class RaycastHiReverseDistanceSorter2D : IComparer<RaycastHit2D> {
            static internal readonly RaycastHiReverseDistanceSorter2D Instance = new RaycastHiReverseDistanceSorter2D();

            public int Compare(RaycastHit2D x, RaycastHit2D y) {
                return y.distance.CompareTo(x.distance);
            }
        }
    }

    /// <summary>
    /// Raycast intersection information.
    /// </summary>
    public struct RaycastIntersection2D {
        public int ColliderId;
        public float EnterDistance;
        public float ExitDistance;
        public RaycastIntersectionFlags Flags;
    }

    [Flags]
    public enum RaycastIntersectionFlags : byte {
        IntersectsStart = 0x01,
        IntersectsEnd = 0x02
    }

    public struct Raycast2DBuffers {
        public RaycastHit2D[] BufferA;
        public RaycastHit2D[] BufferB;

        public Raycast2DBuffers(int capacity) {
            BufferA = new RaycastHit2D[capacity];
            BufferB = new RaycastHit2D[capacity];
        }
    }
}