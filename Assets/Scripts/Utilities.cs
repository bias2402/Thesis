using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities {
    public static class UnityUtil {
        /// <summary>
        /// Returns the distance vector from <paramref name="origin"/> to <paramref name="target"/>. 
        /// Can be called directly on a Vector3
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector3 DistanceTo(this Vector3 origin, Vector3 target) { return target - origin; }

        /// <summary>
        /// Returns the normalized direction from <paramref name="origin"/> to <paramref name="target"/>. 
        /// Can be called directly on a Vector3
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector3 DirectionTo(this Vector3 origin, Vector3 target) { return (target - origin) / (target - origin).magnitude; }
    }

    public static class CSUtil {
        /// <summary>
        /// Moves the item at <paramref name="oldIndex"/> to <paramref name="newIndex"/> inside <paramref name="list"/>. 
        /// Can be called directly on lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="oldIndex"></param>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        public static List<T> MoveItem<T>(this List<T> list, int oldIndex, int newIndex) {
            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            return list;
        }

        /// <summary>
        /// Moves <paramref name="item"/> to <paramref name="newIndex"/> inside <paramref name="list"/>. 
        /// Can be called directly on lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="item"></param>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        public static List<T> MoveItem<T>(this List<T> list, T item, int newIndex) {
            list.Remove(list.Find(x => x.Equals(item)));
            list.Insert(newIndex, item);
            return list;
        }

        /// <summary>
        /// Checks if the string contains a float, returning true if it does
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsFloat(this string s) {
            try {
                float.Parse(s);
                return true;
            } catch {
                return false;
            }
        }
    }

    public static class MathUtil {
        /// <summary>
        /// Maps <paramref name="value"/> from input range (<paramref name="inputMin"/>, <paramref name="inputMax"/>) 
        /// to output range (<paramref name="outputMin"/>, <paramref name="outputMax"/>). 
        /// Can be called directly on floats
        /// </summary>
        /// <param name="value"></param>
        /// <param name="inputMin"></param>
        /// <param name="inputMax"></param>
        /// <param name="outputMin"></param>
        /// <param name="outputMax"></param>
        /// <returns></returns>
        public static float Map(this float value, float inputMin, float inputMax, float outputMin, float outputMax) {
            return (value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin;

            //Step by step for this function:
            #region
            //float fromAbs = value - inputMin;
            //float fromMaxAbs = inputMax - inputMin;
            //float normal = fromAbs / fromMaxAbs;
            //float toMaxAbs = outputMax - outputMin;
            //float toAbs = toMaxAbs * normal;
            //float to = toAbs + outputMin;
            //return to;
            #endregion
        }

        /// <summary>
        /// Set <paramref name="input"/> to a random value between <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Random(this double input, double min, double max) {
            System.Random random = new System.Random();
            return random.NextDouble() * (max - min) + min;
        }
    }
}