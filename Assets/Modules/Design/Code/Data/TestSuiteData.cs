using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceFab.Design
{
    public enum FlowState
    {
        Empty,
        Hi,
        Lo,
        Unstable,
    }

    [Serializable]
    public struct TestEntry
    {
        public InputOutputNodeTypeFlags Id;
        public FlowState State; // starting or target state
    }

    [Serializable]
    public struct TestData
    {
        public TestEntry[] Bundle; // all tests that run in the same pass
    }

    [CreateAssetMenu(menuName = "SpaceFab/Design/Test Suite Data")]
    public class TestSuiteData : ScriptableObject
    {
        // public SerializedHash32[] Headers;
        public TestData[] Tests;
    }

    public static class EvalUtility
    {
        // Finds the FlowState for a given node subtype in this test row's bundle. Returns
        // FlowState.Empty if no matching entry. Handles VPLUS/VMINUS as constant HI/LO without
        // requiring suite authors to include them in Bundle.
        public static FlowState GetTestValBySubType(InputOutputNodeTypeFlags subtype, TestData testData)
        {
            // VPLUS / VMINUS are rail constants — not bundled per test.
            if (subtype == InputOutputNodeTypeFlags.VPLUS) { return FlowState.Hi; }
            if (subtype == InputOutputNodeTypeFlags.VMINUS) { return FlowState.Lo; }

            if (testData.Bundle == null) { return FlowState.Empty; }
            for (int i = 0; i < testData.Bundle.Length; i++)
            {
                if (testData.Bundle[i].Id == subtype) { return testData.Bundle[i].State; }
            }
            return FlowState.Empty;
        }

        /*
        public static string GetSubtypeByPlacableID(Placeable placeable)
        {
            switch (placeable)
            {
                case Placeable.IN:
                    return GameConsts.IN_SUBTYPE;
                case Placeable.A:
                    return GameConsts.A_SUBTYPE;
                case Placeable.B:
                    return GameConsts.B_SUBTYPE;
                case Placeable.VPLUS:
                    return GameConsts.VPLUS_SUBTYPE;
                case Placeable.VMINUS:
                    return GameConsts.VMINUS_SUBTYPE;
                case Placeable.OUT:
                    return GameConsts.OUT_SUBTYPE;
                case Placeable.OUTX:
                    return GameConsts.X_SUBTYPE;
                case Placeable.OUTY:
                    return GameConsts.Y_SUBTYPE;
                default:
                    return "";
            }

            return "";
        }
        */

        /*
        public static FlowState GetTestValBySubType(string subtype, TestData testData)
        {
            if (subtype.Equals(GameConsts.IN_SUBTYPE))
            {
                return testData.InVal;
            } 
            else if (subtype.Equals(GameConsts.A_SUBTYPE))
            {
                return testData.AVal;
            }
            else if (subtype.Equals(GameConsts.B_SUBTYPE))
            {
                return testData.BVal;
            }
            else if (subtype.Equals(GameConsts.OUT_SUBTYPE))
            {
                return testData.OutVal;
            }
            else if (subtype.Equals(GameConsts.X_SUBTYPE))
            {
                return testData.OutXVal;
            }
            else if (subtype.Equals(GameConsts.Y_SUBTYPE))
            {
                return testData.OutYVal;
            }
            else if (subtype.Equals(GameConsts.VPLUS_SUBTYPE))
            {
                return FlowState.Hi;
            }
            else if (subtype.Equals(GameConsts.VMINUS_SUBTYPE))
            {   
                return FlowState.Lo;
            }

            return FlowState.Empty;
        }
        */

        /*
        public static int GetColIndexInHeaders(Placeable[] headers, string subtype)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (GetSubtypeByPlacableID(headers[i]).Equals(subtype)) 
                {
                    return i;
                }
            }

            return - 1;
        }
        */
    }

}