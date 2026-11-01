using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrackSelect
{
    internal static class InteropHelpers
    {
        public static IEnumerable<Transform> Children(Transform parent)
        {
            for (int i = 0; i < parent.childCount; ++i) yield return parent.GetChild(i);
        }
        public static IEnumerable<Transform> Where(this Transform parent, Func<Transform, bool> predicate)
            => Children(parent).Where(predicate);
        public static Transform FirstOrDefault(this Transform parent, Func<Transform, bool> predicate)
            => Children(parent).FirstOrDefault(predicate);
    }
}
