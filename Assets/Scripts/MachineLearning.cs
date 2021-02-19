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
             * The following handling has five sets of variables representing different aspects of the convolution:
             * dimx/dimy: these give the dimensions for the new map
             * offsetx/offsety: these give the offset for the convolution
             * nmx/nmx: these give the index to apply the calculated value on the new map
             * kx/ky: these give the index for the kernel, which act as a lens moved across the map, through which the filter is applied
             * mx/my: these give the index for the given map, and they are set by the kernel's current position
            */
            foreach (CNNFilter1D filter in CNNFilters) {
                float dimx = (1 + (map.GetLength(0) - filter.dimensions) / stride);                             //O = 1 + (N - F) / S  --> Understanding of a Convolutional Neural Network (Albawi, Al-Zawi, & Mohammed, 2017)
                float dimy = (1 + (map.GetLength(1) - filter.dimensions) / stride);
                if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001) 
                    throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or filter(s) so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                int offsetx = (int)Math.Floor(filter.dimensions / 2);                                           //Used to offset the kernel center along the x axis based on the filter dimensions
                int offsety = (int)Math.Floor(filter.dimensions / 2);                                           //Used to offset the kernel center along the y axis based on the filter dimensions
                float[,] newMap = new float[(int)dimx, (int)dimy];                                              //The map is created using the dimensions calculated above
                for (int nmy = offsety; nmy < dimy; nmy++) {                                                    //nmy is the y-coord on the new map
                    for (int nmx = offsetx; nmx < dimx; nmx++) {                                                    //nmx the x-coord on the new map
                        float value = 0;                                                                            //value is the calculated value of the current kernel position
                        for (int kx = 0, ky = 0, mx = nmx, my = nmy; ky < filter.dimensions; kx++, mx++) {              //kx is the filter x-coord, ky is the filter y-coord, mx is the map x-coord, and my is the map y-coord
                            value += map[mx - offsetx, my - offsety] * filter.filter[kx, ky];

                            if (kx == filter.dimensions - 1) {                                                              //If kx reached the end of the current row, continue to the next row
                                ky++;
                                kx = 0;
                                mx = nmx - 1;
                                my++;
                            }
                        }

                        newMap[nmx, nmy] = value;
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
            float[,] newMap = new float[(int)Math.Ceiling((double)((float)map.GetLength(0) / stride)), 
                                        (int)Math.Ceiling((double)((float)map.GetLength(1) / stride))];     //The map will shrink during pooling, so the new map is smaller in both dimensions
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