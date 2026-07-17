using BeauUtil;
using ScriptableBake;
using System;
using UnityEngine;

namespace FieldDay.Scenes {
    /// <summary>
    /// Reparents children into various buckets
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReparentChildrenByName : MonoBehaviour, IBaked {

        public const int Order = FlattenHierarchy.Order - 9;

        [Serializable]
        public struct Rule {
            public string NamePattern;
            public Transform Target;
            [NonSerialized] internal WildcardMatch Matcher;
        }

        public Rule[] Rules;

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Baking.UnpackPrefabIfNecessary(transform);

            CompileRules(Rules);

            int childIndex = 0;
            while (childIndex < transform.childCount) {
                Transform child = transform.GetChild(childIndex);
                if (!AttemptReparent(Rules, child)) {
                    childIndex++;
                }
            }
            Baking.Destroy(this, true);
            return true;
        }

        static private void CompileRules(Rule[] rules) {
            for (int i = 0, len = rules.Length; i < len; i++) {
                rules[i].Matcher = WildcardMatch.Compile(rules[i].NamePattern);
            }
        }

        static private bool AttemptReparent(Rule[] rules, Transform child) {
            string goName = child.gameObject.name;
            for(int i = 0, len = rules.Length; i < len; i++) {
                if (rules[i].Matcher.Match(goName)) {
                    child.SetParent(rules[i].Target, true);
                    return true;
                }
            }

            return false;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}