using System;
using System.Collections.Generic;
using System.IO;

namespace MachineLearning {
    public class CNN {
        private List<CNNFilter1D> CNNFilters = new List<CNNFilter1D>();
        private List<float[,]> generatedMaps = new List<float[,]>();

        /// <summary>
        /// Create a new <paramref name="filter"/> with dimensions equal to the given array's length. Filter must have the same length in both dimensions!
        /// </summary>
        /// <param name="filter"></param>
        public void AddNewFilter (float[,] filter, string filterName = "") {
            if (filter.GetLength(0) != filter.GetLength(1)) throw new ArgumentException("Filter dimensions aren't equal size!");
            CNNFilters.Add(new CNNFilter1D(filter, filterName));
        }

        /// <summary>
        /// Run the CNN on the given 2D float array and return the network's deceision.
        /// </summary>
        /// <param name="input"></param>
        public int Run (float[,] input) {
            return 0;
        }

        /// <summary>
        /// Add padding of zeros to the inputted map (<paramref name="input"/>)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        float[,] Padding (float[,] input) {
            int newSize = input.GetLength(0) + 2;
            float[,] newMap = new float[newSize, newSize];
            for (int x = 0; x < newSize; x++) {
                for (int y = 0; y < newSize; y++) {
                    if (x == 0 || y == 0) newMap[x, y] = 0;
                    else newMap[x, y] = input[x, y];
                }
            }
            return newMap;
        }

        /// <summary>
        /// Run the convolution on <paramref name="input"/> resulting in a list of maps: one map per filter used. 
        /// </summary>
        /// <param name="input"></param>
        void Convolution (float[,] map) {
            foreach (CNNFilter1D filter in CNNFilters) {
                float[,] newMap = new float[map.GetLength(0) - 2, map.GetLength(1) - 2];
                for (int mx = 1; mx < map.GetLength(0) - 1; mx++) {                          //mx is the map x-coord
                    for (int my = 1; my < map.GetLength(1) - 1; my++) {                          //my is the map y-coord
                        for (int fx = 0, fy = 0; fx < filter.dimensions; fx++, fy++) {                  //fx is the filter x-coord and fy is the filter y-coord

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pool the given <paramref name="map"/>, keeping the highest value found in the kernel for each stride.
        /// Kernel is a square of size <paramref name="kernelDimension"/>, and it's moved using <paramref name="stride"/>.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="filterDimension"></param>
        /// <param name="stride"></param>
        void Pooling (float[,] map, int kernelDimension = 2, int stride = 2) {

        }


        void FullyConnected () {

        }

        class CNNFilter1D {
            public string filterName = "";
            public int dimensions = 3;
            public float[,] filter;

            public CNNFilter1D (float[,] filter, string filterName) {
                this.filter = filter;
                dimensions = filter.GetLength(0);
                if (!filterName.Equals("")) this.filterName = filterName;
                else {

                }
            }
        }
    }
}