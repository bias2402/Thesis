using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineLearning {
    public class CNN {
        private List<CNNFilter1D> CNNFilters = new List<CNNFilter1D>();
        private List<float[,]> generatedMaps = new List<float[,]>();

        /// <summary>
        /// Create a new <paramref name="filter"/> with dimensions equal to the given array's length. Filter must have the same length in both dimensions!
        /// </summary>
        /// <param name="filter"></param>
        public void AddNewFilter(float[,] filter, string filterName = "") {
            if (filter.GetLength(0) != filter.GetLength(1)) throw new ArgumentException("Filter dimensions aren't equal size!");
            CNNFilters.Add(new CNNFilter1D(filter, filterName));
        }

        /// <summary>
        /// Run the CNN on the given 2D float array with the default settings and return the network's decision.
        /// Default settings: padding = 0, pooling kernel = 2, pooling stride = 2
        /// Flow: Convolution, Pooling, Fully Connected (Decision Making)
        /// </summary>
        /// <param name="input"></param>
        public int Run(float[,] input) {
            return 0;
        }

        /// <summary>
        /// Add padding of zeros to the inputted map (<paramref name="map"/>)
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public float[,] Padding(float[,] map) {
            int newSizeX = map.GetLength(0) + 2;
            int newSizeY = map.GetLength(1) + 2;
            float[,] newMap = new float[newSizeX, newSizeY];
            for (int x = 0; x < newSizeX; x++) {
                for (int y = 0; y < newSizeY; y++) {
                    if (x == 0 || y == 0 || x == newSizeX - 1 || y == newSizeY - 1) newMap[x, y] = 0;
                    else newMap[x, y] = map[x - 1, y - 1];
                }
            }
            return newMap;
        }

        /// <summary>
        /// Run the convolution on <paramref name="input"/> resulting in a list of maps: one map per filter used. 
        /// </summary>
        /// <param name="input"></param>
        public List<float[,]> Convolution(float[,] map, int stride = 1) {
            /*
             * The following handling has four sets of variables representing different aspects of the convolution:
             * nmx/nmy: these give the index to set the calculated value in newMap
             * kx/ky: these give the index for the kernel, which act as a lens moved across the map, through which the filter is applied
             * fx/fy: these give the index for the filter, which is applied on the given map
             * mx/my: these give the index for the given map, and they are set by the kernel's current position
            */
            foreach (CNNFilter1D filter in CNNFilters) {
                int offset = (int)Math.Floor((double)filter.dimensions / 2);                                //Offset the center depending on filter dimensions
                float[,] newMap = new float[map.GetLength(0) - offset * 2, map.GetLength(1) - offset * 2];  //The map will shrink during convolution, so the new map is smaller in both dimensions
                for (int nmy = 0, ky = offset; ky < map.GetLength(0) - offset; nmy++, ky++) {               //nmy is the new map x-coord and ky is the kernel x-coord
                    for (int nmx = 0, kx = offset; kx < map.GetLength(1) - offset; nmx++, kx++) {               //nmx is the new map y-coord and kx is the kernel y-coord
                        float value = 0;                                                                            //value is the calculated value of the current kernel position
                        for (int fx = 0, fy = 0, mx = kx, my = ky; fy < filter.dimensions; fx++, mx++) {            //fx is the filter x-coord, fy is the filter y-coord, mx is the map x-coord, and my is the map y-coord
                            value += map[mx - offset, my - offset] * filter.filter[fx, fy];

                            if (fx == filter.dimensions - 1) {                                                          //If fx reached the end of the current row, continue to the next row
                                fy++;
                                fx = 0;
                                mx = kx - 1;
                                my++;
                            }
                        }

                        newMap[nmy, nmx] = value;
                    }
                }
                generatedMaps.Add(newMap);
            }
            return generatedMaps;
        }

        /// <summary>
        /// Pool the given <paramref name="map"/>, keeping the highest value found in the kernel for each stride.
        /// Kernel is a square of size <paramref name="kernelDimension"/>, and it's moved using <paramref name="stride"/>.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="filterDimension"></param>
        /// <param name="stride"></param>
        public float[,] Pooling(float[,] map, int kernelDimension = 2, int stride = 2) {
            /*
             * The following handling has three sets of variables representing different aspects of the pooling:
             * nmx/nmy: these give the index to set the calculated value in newMap
             * kx/ky: these give the index for the kernel, which act as a lens moved across the map, through which the filter is applied
             * mx/my: these give the index for the given map, and they are set inside the kernel's current position
            */
            float[,] newMap = new float[map.GetLength(0) / stride, map.GetLength(1) / stride];              //The map will shrink during pooling, so the new map is smaller in both dimensions
            List<float> kernelValues = new List<float>();
            for (int ky = 0, nmy = 0; ky < map.GetLength(0); ky += stride, nmy++) {
                for (int kx = 0, nmx = 0; kx < map.GetLength(1); kx += stride, nmx++) {
                    kernelValues.Clear();
                    for (int mx = 0, my = 0; my < kernelDimension; mx++) {
                        if (kx + mx < map.GetLength(0) && ky + my < map.GetLength(1)) kernelValues.Add(map[kx + mx, ky + my]);
                        if (mx == kernelDimension - 1) {
                            mx = -1;
                            my++;
                        }
                    }
                    newMap[nmx, nmy] = kernelValues.Max();
                }
            }
            return newMap;
        }
        
        public double FullyConnected() {
            throw new NotImplementedException("FullyConnected not implemented");
        }

        class CNNFilter1D {
            public string filterName = "";
            public float dimensions = 3;
            public float[,] filter;

            public CNNFilter1D(float[,] filter, string filterName) {
                this.filter = filter;
                dimensions = filter.GetLength(0);
                if (!filterName.Equals("")) this.filterName = filterName;
                else {
                    string s = "";
                    for (int i = 0; i < filter.GetLength(0); i++) {
                        for (int j = 0; j < filter.GetLength(1); j++) {
                            s += filter[i, j].ToString();
                        }
                        if (i < filter.GetLength(0) - 1) s += ",";
                    }
                }
            }
        }
    }
}