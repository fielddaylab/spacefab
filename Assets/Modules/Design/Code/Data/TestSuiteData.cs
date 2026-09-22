using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using ScriptableBake;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceFab.Design
{
    public enum FlowState : byte
    {
        Empty,
        Hi,
        Lo,
        Unstable,
    }

    [Flags]
    public enum TestSuiteColumnMask : byte {
        InputA = 0x01,
        InputB = 0x02,
        OutputX = 0x04,
        OutputY = 0x08
    }

    [Serializable]
    public struct TestEntry
    {
        public InputOutputNodeTypeFlags Id;
        public FlowState State; // starting or target state
    }

    [Serializable]
    public struct TestData {
        [Header("Input")]
        public FlowState InputA;
        public FlowState InputB;
        [Header("Output")]
        public FlowState OutputX;
        public FlowState OutputY;
    }

    [CreateAssetMenu(menuName = "SpaceFab/Design/Test Suite Data")]
    public class TestSuiteData : ScriptableObject
    {
        public TestSuiteColumnMask ColumnMask;
        public TestData[] Rows;

        //[UnityEditor.MenuItem("SpaceFab/Design/Upgrade Test Suites")]
        //static private void UpgradeAll() {
        //    var allSuites = AssetUtility.Editor.FindAllAssets<TestSuiteData>();
        //    foreach(var suite in allSuites) {
        //        Baking.PrepareUndo(suite, "Upgrading data");
        //        if (UpgradeData(suite)) {
        //            Log.Msg("[TestSuiteData] Upgraded '{0}'", suite.name);
        //            Baking.SetDirty(suite);
        //        }
        //    }
        //}

        //static public bool UpgradeData(TestSuiteData suiteData) {
        //    if (suiteData.Tests.Length <= 0) {
        //        return false;
        //    }

        //    TestRow[] rows = new TestRow[suiteData.Tests.Length];
        //    TestSuiteColumnMask columns = default;

        //    for(int i = 0; i < suiteData.Tests.Length; i++) {
        //        ref TestRow rowData = ref rows[i];
        //        ref TestData testData = ref suiteData.Tests[i];

        //        foreach(var entry in testData.Bundle) {
        //            switch (entry.Id) {
        //                case InputOutputNodeTypeFlags.A: {
        //                    columns |= TestSuiteColumnMask.InputA;
        //                    rowData.InputA = entry.State;
        //                    break;
        //                }
        //                case InputOutputNodeTypeFlags.B: {
        //                    columns |= TestSuiteColumnMask.InputB;
        //                    rowData.InputB = entry.State;
        //                    break;
        //                }
        //                case InputOutputNodeTypeFlags.OUTX: {
        //                    columns |= TestSuiteColumnMask.OutputX;
        //                    rowData.OutputX = entry.State;
        //                    break;
        //                }
        //                case InputOutputNodeTypeFlags.OUTY: {
        //                    columns |= TestSuiteColumnMask.OutputY;
        //                    rowData.OutputY = entry.State;
        //                    break;
        //                }
        //                default: {
        //                    Log.Error("[TestSuiteData] Suite '{0}' is using unexpected node type {1}", suiteData.name, entry.Id);
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    suiteData.Tests = null;
        //    suiteData.Rows = rows;
        //    suiteData.ColumnMask = columns;
        //    return true;
        //}
    }

    public static class EvalUtility
    {
        // Finds the FlowState for a given node subtype in this test row's bundle. Returns
        // FlowState.Empty if no matching entry. Handles VPLUS/VMINUS as constant HI/LO without
        // requiring suite authors to include them in Bundle.
        public static FlowState GetTestValBySubType(InputOutputNodeTypeFlags subtype, TestSuiteColumnMask columnMask, TestData testData)
        {
            switch (subtype) {
                case InputOutputNodeTypeFlags.VPLUS: {
                    return FlowState.Hi;
                }
                case InputOutputNodeTypeFlags.VMINUS: {
                    return FlowState.Lo;
                }
                case InputOutputNodeTypeFlags.A: {
                    if ((columnMask & TestSuiteColumnMask.InputA) == 0) {
                        return FlowState.Empty;
                    }
                    return testData.InputA;
                }
                case InputOutputNodeTypeFlags.B: {
                    if ((columnMask & TestSuiteColumnMask.InputB) == 0) {
                        return FlowState.Empty;
                    }
                    return testData.InputB;
                }
                case InputOutputNodeTypeFlags.OUTX: {
                    if ((columnMask & TestSuiteColumnMask.OutputX) == 0) {
                        return FlowState.Empty;
                    }
                    return testData.OutputX;
                }
                case InputOutputNodeTypeFlags.OUTY: {
                    if ((columnMask & TestSuiteColumnMask.OutputY) == 0) {
                        return FlowState.Empty;
                    }
                    return testData.OutputY;
                }
                default: {
                    return FlowState.Empty;
                }
            }
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