using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace SpaceFab.Design
{
    public enum StackLayer : int
    {
        Metal = 0,
        Transistor = 1,
    }

    public struct GridCoord
    {
        public int Layer;
        public int Col;
        public int Row;

        public GridCoord(int layer, int col, int row)
        {
            Layer = layer;
            Col = col;
            Row = row;
        }

        public static bool operator ==(GridCoord c1, GridCoord c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(GridCoord c1, GridCoord c2)
        {
            return !c1.Equals(c2);
        }

        public override bool Equals(object obj)
        {
            var other = (GridCoord)obj;
            return (Layer == other.Layer) && (Col == other.Col) && (Row == other.Row);
        }
    }

    /// <summary>
    /// Underlying data representation of the whole grid stack, containing both metal and transistor layers
    /// Inputs in grid start at 0, 0 in the bottom left. Row indices increase from bottom to top.
    /// </summary>
    public class GridStack
    {
        public Dimensions LayerDims; // x and y dims of each layer
        [HideInInspector] public GridLayer[] GridLayers; // layers ordered from highest to lowest
    }
}