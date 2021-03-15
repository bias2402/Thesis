using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace MachineLearning {
    public class CNN {
        private List<CNNFilter> cnnFilters = new List<CNNFilter>();
        private List<float[,]> convolutedMaps = new List<float[,]>();
        private List<float[,]> pooledMaps = new List<float[,]>();
        private List<double> outputs = new List<double>();
        private ANN ann = null;
        private bool isDebugging = false;

        //Get methods
        #region
        /// <summary>
        /// Return the list of filters from this CNN
        /// </summary>
        /// <returns></returns>
        public List<CNNFilter> GetFilters() { return cnnFilters; }

        /// <summary>
        /// Return the list of convoluted maps from this CNN
        /// </summary>
        /// <returns></returns>
        public List<float[,]> GetConvolutedMaps() { return convolutedMaps; }

        /// <summary>
        /// Return the list of pooled maps from this CNN
        /// </summary>
        /// <returns></returns>
        public List<float[,]> GetPooledMaps() { return pooledMaps; }

        /// <summary>
        /// Return the internal ANN of this CNN
        /// </summary>
        /// <returns></returns>
        public ANN GetANN() { return ann; }

        /// <summary>
        /// Return the list of outputs from this CNN
        /// </summary>
        /// <returns></returns>
        public List<double> GetOutputs() { return outputs; }
        #endregion

        /// <summary>
        /// Allows setting a custom made ANN as the CNN's internal ANN for the Fully-Connected layer calculations
        /// </summary>
        /// <param name="ann"></param>
        public void SetANN(ANN ann) => this.ann = ann;

        /// <summary>
        /// Enables debugging. This method should never be called from anything but the MLDebugger!
        /// </summary>
        internal void EnableDebugging() => isDebugging = true;

        /// <summary>
        /// Clear the CNN completely. Set the parameters to only clear some of the data.
        /// </summary>
        /// <param name="clearOutputs"></param>
        /// <param name="clearGeneratedMaps"></param>
        /// <param name="clearFilters"></param>
        /// <param name="clearANN"></param>
        public void Clear(bool clearOutputs = true, bool clearGeneratedMaps = true, bool clearFilters = true, bool clearANN = true) {
            if (isDebugging) MLDebugger.AddToDebugOutput("Clearing specified data from the CNN", false);
            if (clearOutputs) outputs.Clear();
            if (clearGeneratedMaps) convolutedMaps.Clear();
            if (clearFilters) cnnFilters.Clear();
            if (clearANN) ann = null;
        }

        /// <summary>
        /// Create a new <paramref name="filter"/> with dimensions equal to the given array's length. Filter must have the same length in both dimensions!
        /// </summary>
        /// <param name="filter"></param>
        public void AddNewFilter(float[,] filter, string filterName = "") {
            if (cnnFilters.Find(x => x.filterName == filterName) != null) return;

            if (filter.GetLength(0) != filter.GetLength(1)) throw new ArgumentException("Filter dimensions must be equal in both x and y direction!");
            cnnFilters.Add(new CNNFilter(filter, filterName));
        }

        //CNN methods for generating outputs and training network
        #region
        /// <summary>
        /// Run the CNN on the given <paramref name="input"/> using default settings and return outputs.
        /// Default settings: padding = 0, convolution stride = 1, pooling kernel = 2, pooling stride = 2, outputs = 3
        /// Flow: Convolution, Pooling, Fully Connected (Decision Making)
        /// </summary>
        /// <param name="input"></param>
        public List<double> Run(float[,] input) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting CNN default Running", false);
            }

            Convolution(input);

            Pooling();

            FullyConnected(pooledMaps, "pooled maps", 3);
            return outputs;
        }

        /// <summary>
        /// Train the CNN on the given <paramref name="input"/> using default settings and return outputs.
        /// Default settings: padding = 0, convolution stride = 1, pooling kernel = 2, pooling stride = 2, outputs = <paramref name="desiredOutputs"/>.Count
        /// Flow: Convolution, Pooling, Fully Connected (Decision Making)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="desiredOutputs"></param>
        /// <returns></returns>
        public List<double> Train(float[,] input, List<double> desiredOutputs) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting CNN default Training", false);
            }
            Convolution(input);
            Pooling();
            
            FullyConnected(pooledMaps, "pooled maps", desiredOutputs, desiredOutputs.Count);
            return outputs;
        }

        /// <summary>
        /// Add padding of zeros to the inputted map (<paramref name="map"/>)
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public float[,] Padding(float[,] map) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting padding with a map of size " + map.GetLength(0) + "x" + map.GetLength(1) + ", " +
                    "resulting in a map of size " + (map.GetLength(0) + 2) + "x" + (map.GetLength(1) + 2), false);
                MLDebugger.RestartOperationWatch();
            }

            int newSizeX = map.GetLength(0) + 2;
            int newSizeY = map.GetLength(1) + 2;
            float[,] newMap = new float[newSizeX, newSizeY];
            for (int x = 0; x < newSizeX; x++) {
                for (int y = 0; y < newSizeY; y++) {
                    if (x == 0 || y == 0 || x == newSizeX - 1 || y == newSizeY - 1) newMap[x, y] = 0;
                    else newMap[x, y] = map[x - 1, y - 1];
                }
            }
            if (isDebugging) MLDebugger.AddToDebugOutput("Padding Layer complete", true);
            return newMap;
        }

        /// <summary>
        /// Run the convolution on <paramref name="input"/> resulting in a list of maps: one map per filter used. 
        /// </summary>
        /// <param name="input"></param>
        public List<float[,]> Convolution(float[,] map, int stride = 1) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting convolution with a map of size " + map.GetLength(0) + "x" + map.GetLength(1) + ", " + cnnFilters.Count + " filters, and a stride of " + stride, false);
                MLDebugger.RestartOperationWatch();
            }

            float dimx, dimy, value;
            Coord newMapCoord, mapCoord;
            float[,] newMap;

            foreach (CNNFilter filter in cnnFilters) {
                //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
                //It is described and reference in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
                dimx = (1 + (map.GetLength(0) - filter.dimensions) / stride);
                dimy = (1 + (map.GetLength(1) - filter.dimensions) / stride);

                //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
                //new map could have the wrong dimensions, which would break the convolution!
                if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                    throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or filter(s) so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                newMap = new float[(int)dimx, (int)dimy];                                                       //The map is created using the dimensions calculated above
                newMapCoord = new Coord(0, 0, newMap.GetLength(0), newMap.GetLength(1));                        //Coordinates on the new map, where a the calculated value will be placed
                mapCoord = new Coord(0, 0, map.GetLength(0) - (int)filter.dimensions, map.GetLength(1) - (int)filter.dimensions); //Coordinates on the old map, from which the filter is applied

                while (true) {
                    value = 0;                                                                                      //Value that is calculated during convolution and applied to the new map at newMapCoord
                    //Apply filter from current mapCoord
                    for (int y = mapCoord.y, fy = 0; y < mapCoord.y + filter.dimensions; y++, fy++) {
                        for (int x = mapCoord.x, fx = 0; x < mapCoord.x + filter.dimensions; x++, fx++) {
                            value += map[x, y] * filter.filter[fx, fy];
                        }
                    }

                    //Increment mapCoord
                    mapCoord.Increment(stride);

                    //Apply the value to newMap and increment newMapCoord
                    newMap[newMapCoord.x, newMapCoord.y] = value;
                    if (newMapCoord.Increment()) break;
                }
                convolutedMaps.Add(newMap);
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Convolution Layer complete", true);
            return convolutedMaps;
        }

        /// <summary>
        /// Pool the given <paramref name="map"/>, keeping the highest value found in the kernel for each stride.
        /// Kernel is a square of size <paramref name="kernelDimension"/>, and it's moved using <paramref name="stride"/>.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="filterDimension"></param>
        /// <param name="stride"></param>
        public List<float[,]> Pooling(int kernelDimension = 2, int stride = 2) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting pooling of all maps with a kernel with dimensions of " + kernelDimension + ", and a stride of " + stride, false);
                MLDebugger.RestartOperationWatch();
            }

            foreach (float[,] map in convolutedMaps) {
                //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
                //The formula is described and referenced in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
                float dimx = (1 + (map.GetLength(0) - kernelDimension) / stride);
                float dimy = (1 + (map.GetLength(1) - kernelDimension) / stride);

                //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
                //new map could have the wrong dimensions, which would break the pooling!
                if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                    throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or kernel dimension so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                float[,] newMap = new float[(int)dimx, (int)dimy];                                              //The map will shrink during pooling, so the new map is smaller in both dimensions
                Coord newMapCoord = new Coord(0, 0, newMap.GetLength(0), newMap.GetLength(1));                  //Coordinates on the new map, where a the calculated value will be placed
                Coord mapCoord = new Coord(0, 0, map.GetLength(0) - kernelDimension, map.GetLength(1) - kernelDimension); //Coordinates on the old map, from which the filter is applied
                List<float> kernelValues = new List<float>();

                while (true) {
                    kernelValues.Clear();
                    //Store values found through the kernel from current mapCoord
                    for (int y = mapCoord.y; y < mapCoord.y + kernelDimension; y++) {
                        for (int x = mapCoord.x; x < mapCoord.x + kernelDimension; x++) {
                            kernelValues.Add(map[x, y]);
                        }
                    }

                    //Increment mapCoord
                    mapCoord.Increment(stride);

                    //Apply the value to newMap and increment newMapCoord
                    newMap[newMapCoord.x, newMapCoord.y] = kernelValues.Max();
                    if (newMapCoord.Increment()) break;
                }
                pooledMaps.Add(newMap);
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Pooling Layer complete", true);
            return pooledMaps;
        }

        /// <summary>
        /// Run the list of <paramref name="maps"/> through the ANN (creates a new using the <paramref name="nOutputs"/> parameter if necessary)
        /// and return the calculated outputs from the ANN. <paramref name="mapListsName"/> is used by the MLDebugger
        /// </summary>
        /// <param name="maps"></param>
        /// <param name="mapListsName"></param>
        /// <param name="nOutputs"></param>
        /// <returns></returns>
        public List<double> FullyConnected(List<float[,]> maps, string mapListsName, int nOutputs = 3) {
            //Generate a list of inputs
            List<double> inputs = GenerateANNInputs(maps, mapListsName);

            //Create a new ANN if one doesn't exist already
            if (ann == null) ann = new ANN(inputs.Count, 0, 0, nOutputs,
                ActivationFunctionHandler.ActivationFunction.ReLU, ActivationFunctionHandler.ActivationFunction.ReLU);

            if (isDebugging) MLDebugger.EnableDebugging(ann);
            outputs = ann.Run(inputs);

            if (isDebugging) MLDebugger.AddToDebugOutput("Fully Connected Layer complete", true);
            return outputs;
        }

        /// <summary>
        /// Train the ANN using the list of <paramref name="maps"/> (if an ANN doesn't exist, one is created a new using the <paramref name="nOutputs"/> 
        /// parameter). Backpropagation of the ANN is performed using the <paramref name="desiredOutputs"/> parameter
        /// </summary>
        /// <param name="maps"></param>
        /// <param name="listName"></param>
        /// <param name="desiredOutputs"></param>
        /// <param name="nOutputs"></param>
        /// <returns></returns>
        public List<double> FullyConnected(List<float[,]> maps, string listName, List<double> desiredOutputs, int nOutputs = 3) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting Fully Connected with a map of size expecting " + nOutputs + " number of outputs, given " + maps.Count + " maps", false);
                MLDebugger.RestartOperationWatch();
            }
            //Generate a list of inputs
            List<double> inputs = GenerateANNInputs(maps, listName);

            //Create a new ANN if one doesn't exist already
            if (ann == null) ann = new ANN(inputs.Count, 0, 0, nOutputs,
                ActivationFunctionHandler.ActivationFunction.ReLU, ActivationFunctionHandler.ActivationFunction.ReLU);

            if (isDebugging) MLDebugger.EnableDebugging(this.ann);
            outputs = ann.Train(inputs, desiredOutputs);

            if (isDebugging) MLDebugger.AddToDebugOutput("Fully Connected Layer complete", true);
            return outputs;
        }

        /// <summary>
        /// Input a list of float[,] <paramref name="maps"/> and get a list of double values
        /// </summary>
        /// <param name="maps"></param>
        /// <returns></returns>
        List<double> GenerateANNInputs(List<float[,]> maps, string listName) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting input generation from maps in " + listName, false);
            }

            List<double> inputs = new List<double>();
            for (int i = 0; i < maps.Count; i++) {
                for (int x = 0; x < maps[i].GetLength(0); x++) {
                    for (int y = 0; y < maps[i].GetLength(1); y++) {
                        inputs.Add(maps[i][x, y]);
                    }
                }
            }

            MLDebugger.AddToDebugOutput("Input generation complete with " + inputs.Count + " inputs for the ANN", false);
            return inputs;
        }
        #endregion

        //Serilization and deserialization methods, used by the MLSerializer
        #region
        public string SerializeMap(float[,] map) {
            string output = "";
            for (int i = 0; i < map.GetLength(0); i++) {
                for (int j = 0; j < map.GetLength(1); j++) {
                    output += map[i, j].ToString();
                }
            }
            return output;
        }

        /// <summary>
        /// Pass a serialized <paramref name="filterString"/> to deserialize and add a new filter to the CNN
        /// </summary>
        /// <param name="filterString"></param>
        public void ParseFilter(string filterString) => cnnFilters.Add(new CNNFilter(filterString));

        /// <summary>
        /// Pass a serialized <paramref name="mapString"/> to deserialize and add a new convoluted map to the CNN
        /// </summary>
        /// <param name="mapString"></param>
        public void ParseConvolutedMap(string mapString) {
            Queue<string> parts = new Queue<string>(mapString.Split(char.Parse(",")).ToList());
            while (parts.Count > 0) {
                if (parts.Count < 3) break;
                float[,] map = new float[int.Parse(parts.Dequeue()), int.Parse(parts.Dequeue())];
                Coord coord = new Coord(0, 0, map.GetLength(0), map.GetLength(1));
                foreach (char c in parts.Dequeue()) {
                    switch (c.ToString()) {
                        case ",":
                            break;
                        case ";":
                            return;
                        default:
                            map[coord.x, coord.y] = float.Parse(c.ToString());
                            coord.Increment();
                            break;
                    }
                }
                convolutedMaps.Add(map);
            }
        }

        /// <summary>
        /// Pass a serialized <paramref name="mapString"/> to deserialize and add a new pooled map to the CNN
        /// </summary>
        /// <param name="mapString"></param>
        public void ParsePooledMap(string mapString) {
            Queue<string> parts = new Queue<string>(mapString.Split(char.Parse(",")).ToList());
            while (parts.Count > 0) {
                if (parts.Count < 3) break;
                float[,] map = new float[int.Parse(parts.Dequeue()), int.Parse(parts.Dequeue())];
                Coord coord = new Coord(0, 0, map.GetLength(0), map.GetLength(1));
                foreach (char c in parts.Dequeue()) {
                    switch (c.ToString()) {
                        case ",":
                            break;
                        case ";":
                            return;
                        default:
                            map[coord.x, coord.y] = float.Parse(c.ToString());
                            coord.Increment();
                            break;
                    }
                }
                pooledMaps.Add(map);
            }
        }

        /// <summary>
        /// Pass a serialized <paramref name="outputString"/> to deserialize and add a new output to the CNN
        /// </summary>
        /// <param name="outputString"></param>
        public void ParseOutputs(string outputString) {
            Queue<string> parts = new Queue<string>(outputString.Split(char.Parse(";")).ToList());

            while (parts.Count > 0) {
                outputs.Add(double.Parse(parts.Dequeue()));
            }
        }

        /// <summary>
        /// Pass a serialized <paramref name="annString"/> to deserialize and add a new ANN to the CNN
        /// </summary>
        /// <param name="annString"></param>
        public void ParseANN(string annString) => ann = new ANN(annString);
        #endregion

        public class CNNFilter {
            [SerializeReference] public string filterName = "";
            [SerializeReference] public float dimensions = 3;
            [SerializeReference] public float[,] filter;

            /// <summary>
            /// Create a new CNNFilter based on the given <paramref name="filter"/>. If the <paramref name="filterName"/> isn't given, it will be generated from the 
            /// <paramref name="filter"/>
            /// </summary>
            /// <param name="filter"></param>
            /// <param name="filterName"></param>
            public CNNFilter(float[,] filter, string filterName = "") {
                this.filter = filter;
                dimensions = filter.GetLength(0);
                if (!filterName.Equals("")) this.filterName = filterName;
                else {
                    string s = "";
                    for (int i = 0; i < filter.GetLength(0); i++) {
                        for (int j = 0; j < filter.GetLength(1); j++) {
                            s += filter[i, j].ToString();
                        }
                        if (i != filter.GetLength(0) - 1) s += "-";
                    }
                    this.filterName = s;
                }
            }

            /// <summary>
            /// Create a new CNNFilter by deserializing a filter string. The new filter will be <paramref name="filterName"/> with size a of <paramref name="dimension"/> and the
            /// filter will consist of <paramref name="filterString"/>
            /// </summary>
            /// <param name="filterName"></param>
            /// <param name="filterString"></param>
            /// <param name="dimension"></param>
            public CNNFilter(string filterString) {
                Queue<string> filterParts = new Queue<string>(filterString.Split(new char[] { char.Parse(";"), char.Parse(":") }).ToList());

                while (filterParts.Count > 0) {
                    switch (filterParts.Dequeue()) {
                        case "name":
                            if (!filterParts.Peek().Equals("")) filterName = filterParts.Dequeue();
                            else {
                                string s = "";
                                for (int i = 0; i < filter.GetLength(0); i++) {
                                    for (int j = 0; j < filter.GetLength(1); j++) {
                                        s += filter[i, j].ToString();
                                    }
                                    if (i != filter.GetLength(0) - 1) s += "-";
                                }
                                this.filterName = s;
                            }
                            break;
                        case "dimension":
                            dimensions = float.Parse(filterParts.Dequeue());
                            break;
                        case "filter":
                            ParseSerializedFilter(filterParts.Dequeue());
                            break;
                    }

                }
            }

            /// <summary>
            /// Get a string representation of the filter
            /// </summary>
            /// <returns></returns>
            public string GetSerializedFilter() {
                string s = "name:" + filterName + ";";
                s += "dimension:" + dimensions + ";";
                s += "filter:";
                for (int i = 0; i < filter.GetLength(0); i++) {
                    for (int j = 0; j < filter.GetLength(1); j++) {
                        s += filter[i, j].ToString();
                    }
                }
                s += ";";
                return s;
            }

            void ParseSerializedFilter(string filterString) {
                filter = new float[(int)dimensions, (int)dimensions];
                Coord coord = new Coord(0, 0, (int)dimensions, (int)dimensions);
                foreach (char c in filterString) {
                    filter[coord.x, coord.y] = int.Parse(c.ToString());
                    coord.Increment();
                }
            }
        }

        struct Coord {
            public int x;
            public int y;
            private int xMin;
            private int yMin;
            private int xMax;
            private int yMax;

            public Coord(int xMin, int yMin, int xMax, int yMax) {
                x = this.xMin = xMin;
                y = this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            /// <summary>
            /// Increment the x by <paramref name="amount"/>. If x reaches its max, it will reset to its min and increment y by <paramref name="amount"/>.
            /// Returns true if y reaches its max, otherwise it returns false.
            /// </summary>
            /// <param name="amount"></param>
            /// <returns></returns>
            public bool Increment(int amount = 1) {
                x += amount;
                if (x >= xMax) {
                    x = xMin;
                    y += amount;
                    if (y >= yMax) {
                        y = yMin;
                        return true;
                    }
                }
                return false;
            }
        }
    }

    public class ANN {
        private int epochs = 1000;
        private double alpha = 0.05;
        private List<Layer> layers = new List<Layer>();
        private static System.Random random = new System.Random();
        private bool isDebugging = false;

        /// <summary>
        /// Create a new ANN consisting of an input layer with <paramref name="nInputsN"/> neurons, <paramref name="nHiddenL"/> hidden layers each with <paramref name="nhiddenN"/>
        /// neurons and the activation function <paramref name="hiddenLAF"/>, and lastly the output layer with <paramref name="nOutputN"/> neurons and
        /// activation function <paramref name="outputLAF"/>. Epochs (runs) and alpha (learning rate) can be adjusted, but will otherwise default to <paramref name="epochs"/> = 1000
        /// and <paramref name="alpha"/> = 0.05.
        /// </summary>
        /// <param name="nInputsN"></param>
        /// <param name="nHiddenL"></param>
        /// <param name="nhiddenN"></param>
        /// <param name="nOutputN"></param>
        /// <param name="hiddenLAF"></param>
        /// <param name="outputLAF"></param>
        /// <param name="epochs"></param>
        /// <param name="alpha"></param>
        public ANN(int nInputsN, int nHiddenL, int nhiddenN, int nOutputN, ActivationFunctionHandler.ActivationFunction hiddenLAF, ActivationFunctionHandler.ActivationFunction outputLAF, int epochs = 1000, double alpha = 0.05) {
            this.epochs = epochs;
            this.alpha = alpha;

            //Create the input layer
            layers.Add(new Layer(nInputsN));

            //Create the hidden layers
            for (int i = 0; i < nHiddenL; i++) {
                layers.Add(new Layer(nhiddenN, layers[layers.Count - 1], hiddenLAF));
            }

            //Create the output layer
            layers.Add(new Layer(nOutputN, layers[layers.Count - 1], outputLAF));
        }

        /// <summary>
        /// Creates a new ANN from an <paramref name="annString"/> by deserializing the ANN
        /// </summary>
        /// <param name="annString"></param>
        public ANN(string annString) => DeserializeANN(annString);

        //Get methods
        #region
        /// <summary>
        /// Get the total number of neurons in the network.
        /// </summary>
        /// <returns></returns>
        public int GetNeuronCount() {
            int count = 0;
            foreach (Layer l in layers) {
                foreach (Neuron n in l.neurons) {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Return the layers inside this ANN
        /// </summary>
        /// <returns></returns>
        public List<Layer> GetLayers() { return layers; }

        /// <summary>
        /// Get the latest calculated outputs
        /// </summary>
        /// <returns></returns>
        public List<double> GetOutputs() {
            List<double> outputs = new List<double>();
            foreach (Neuron n in layers[layers.Count - 1].neurons) outputs.Add(n.outputValue);
            return outputs;
        }

        /// <summary>
        /// Get the epochs value of this ANN
        /// </summary>
        /// <returns></returns>
        public int GetEpochs() { return epochs; }

        /// <summary>
        /// Get the alpha value of this ANN
        /// </summary>
        /// <returns></returns>
        public double GetAlpha() { return alpha; }
        #endregion

        /// <summary>
        /// Enables debugging. This method should never be called from anything but the MLDebugger!
        /// </summary>
        internal void EnableDebugging() => isDebugging = true;

        //ANN methods for calculating outputs and training network
        #region
        /// <summary>
        /// Run the network with the given inputs.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public List<double> Run(List<double> inputs) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Running ANN", false);
                MLDebugger.RestartOperationWatch();
            }

            PassInputs(inputs);
            CalculateOutput();

            if (isDebugging) MLDebugger.AddToDebugOutput("Run complete", true);
            return GetOutputs();
        }

        /// <summary>
        /// Run the network with <paramref name="inputs"/>, after which backpropagation is run using <paramref name="desiredOutputs"/>.
        /// Number of iterations is based on the network's 'epochs' setting!
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="desiredOutputs"></param>
        /// <returns></returns>
        public List<double> Train(List<double> inputs, List<double> desiredOutputs) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Training ANN for " + epochs + " epochs", false);
                MLDebugger.RestartOperationWatch();
            }

            for (int i = 0; i < epochs; i++) {
                PassInputs(inputs);
                CalculateOutput();
                Backpropagation(desiredOutputs);
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Training complete", true);
            return GetOutputs();
        }

        /// <summary>
        /// Run the network with <paramref name="inputs"/>, after which backpropagation is run using <paramref name="desiredOutputs"/>.
        /// Number of iterations is based on the amount of data sets nested in lists, as well as network's 'epochs' setting. The network will
        /// iterate for list's count times epochs (which can become a lot)!
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="desiredOutputs"></param>
        /// <returns></returns>
        public List<double> Train(List<List<double>> inputs, List<List<double>> desiredOutputs) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Training ANN for " + epochs + " epochs", false);
                MLDebugger.RestartOperationWatch();
            }

            for (int i = 0; i < inputs.Count; i++) {
                for (int j = 0; j < epochs; j++) {
                    PassInputs(inputs[i]);
                    CalculateOutput();
                    Backpropagation(desiredOutputs[i]);
                }
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Training complete", true);
            return GetOutputs();
        }

        /// <summary>
        /// Pass the inputs to the neuron's of the input layer.
        /// </summary>
        /// <param name="inputs"></param>
        void PassInputs(List<double> inputs) {
            for (int i = 0; i < layers[0].neurons.Count; i++) {
                layers[0].neurons[i].inputValue = inputs[i];
            }
        }

        /// <summary>
        /// Run the calculations through the network    
        /// </summary>
        void CalculateOutput() {
            double value;
            Neuron neuron;

            //Run through all layers
            for (int i = 0; i < layers.Count; i++) {
                //Run through all neurons for each layer
                for (int j = 0; j < layers[i].neurons.Count; j++) {
                    neuron = layers[i].neurons[j];

                    if (neuron.isInputNeuron) {
                        //An input neuron passes its input on as its output without any calculations
                        neuron.outputValue = neuron.inputValue;
                    } else {
                        //Set the neuron's inputs to the outputs of the neurons from the previous layer. Also check if the number of inputs equals the number of weights
                        neuron.inputs.Clear();
                        foreach (Neuron n in layers[i - 1].neurons) neuron.inputs.Add(n.outputValue);
                        if (neuron.inputs.Count != neuron.weights.Count) throw new IndexOutOfRangeException("Neuron has " + neuron.inputs.Count + " inputs and " + neuron.weights.Count + " weights!");

                        //Iterate through all the neuron's inputs and calculate the input value
                        value = 0;
                        for (int k = 0; k < neuron.inputs.Count; k++) {
                            value += neuron.inputs[k] * neuron.weights[k];
                        }

                        //Subtract the neuron's bias and run the value through the chosen activation function to get the neuron's final output
                        value -= neuron.bias;
                        neuron.outputValue = ActivationFunctionHandler.TriggerActivationFunction(neuron.activationFunction, value);
                    }
                }
            }
        }

        /// <summary>
        /// Backpropagation for the ANN to update weights and biases of the neurons
        /// </summary>
        /// <param name="desiredOutputs"></param>
        void Backpropagation(List<double> desiredOutputs) {
            int outputLayer = layers.Count - 1;
            int hiddenLayers = layers.Count > 2 ? layers.Count - 2 : 0;
            Neuron neuron;

            //Output layer
            for (int i = 0; i < layers[outputLayer].neurons.Count; i++) {
                neuron = layers[outputLayer].neurons[i];

                //Calculate the error and errorGradient
                double error = desiredOutputs[i] - neuron.outputValue;
                double errorGradient = ActivationFunctionHandler.TriggerDerativeFunction(neuron.activationFunction, neuron.outputValue * error);

                //Update the neuron's weights
                for (int j = 0; j < neuron.weights.Count; j++) neuron.weights[j] += alpha * neuron.inputValue * error;

                //Update the neuron's bias and errorGradient
                neuron.bias = alpha * -1 * errorGradient;
                neuron.errorGradient = errorGradient;
            }

            //Hidden layers
            if (hiddenLayers != 0) {
                for (int i = hiddenLayers; i > 0; i--) {
                    //Calculate the errorGradientSum for the previous layer
                    double errorGradientSum = 0;
                    for (int j = 0; j < layers[i + 1].neurons.Count; j++) {
                        errorGradientSum += layers[i + 1].neurons[j].errorGradient;
                    }

                    //Update the neurons in this hidden layer
                    for (int j = 0; j < layers[i].neurons.Count; j++) {
                        neuron = layers[i].neurons[j];

                        //Calculate the errorGradient
                        double errorGradient = ActivationFunctionHandler.TriggerDerativeFunction(neuron.activationFunction, outputLayer) * errorGradientSum;

                        //Update the neuron's weights
                        for (int k = 0; k < neuron.weights.Count; k++) neuron.weights[k] += alpha * neuron.inputValue * errorGradient;

                        //Update the neuron's bias and errorGradient
                        neuron.bias = alpha * -1 * neuron.errorGradient;
                        neuron.errorGradient = errorGradient;
                    }
                }
            }
        }
        #endregion

        //Deserialization methods, used by the MLSerializer
        #region
        public string SerializeANN() {
            string output = "";
            output += "alpha:" + alpha + ";\n";
            output += "epochs:" + epochs + ";\n";
            return output;
        }

        void DeserializeANN(string annString) {
            Queue<string> parts = new Queue<string>(annString.Split(char.Parse(";")).ToList());
            Queue<string> subparts;

            while (parts.Count > 0) {
                subparts = new Queue<string>(parts.Dequeue().Split(char.Parse(":")).ToList());

                switch (subparts.Dequeue()) {
                    case "epochs":
                        epochs = int.Parse(subparts.Dequeue());
                        break;
                    case "alpha":
                        alpha = double.Parse(subparts.Dequeue());
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a new Layer to the ANN's layers by deserializing the given <paramref name="layerString"/>
        /// </summary>
        /// <param name="layerString"></param>
        public void DeserializeLayer(string layerString) => layers.Add(new Layer(layerString));
        #endregion

        public class Layer {
            public List<Neuron> neurons = new List<Neuron>();

            /// <summary>
            /// Create a new layer with <paramref name="numberOfNeuronsForLayer"/> number for neurons.
            /// If <paramref name="prevLayer"/> is null, the layer will be made as an input layer.
            /// If <paramref name="activationFunction"/> isn't set, the layer will default to ReLU.
            /// </summary>
            /// <param name="numberOfNeuronsForLayer"></param>
            /// <param name="prevLayer"></param>
            /// <param name="activationFunction"></param>
            public Layer(int numberOfNeuronsForLayer, Layer prevLayer = null, ActivationFunctionHandler.ActivationFunction activationFunction = ActivationFunctionHandler.ActivationFunction.ReLU) {
                for (int i = 0; i < numberOfNeuronsForLayer; i++) {
                    if (prevLayer != null)
                        neurons.Add(new Neuron(prevLayer.neurons.Count));
                    else neurons.Add(new Neuron());
                }
                if (prevLayer != null) foreach (Neuron n in neurons) n.activationFunction = activationFunction;
            }

            /// <summary>
            /// Create a new layer by deserializing the <paramref name="layerString"/>
            /// </summary>
            /// <param name="layerString"></param>
            public Layer(string layerString) => DeserializeLayer(layerString);

            /// <summary>
            /// Set the activation function in each neuron of this layer to <paramref name="activationFunction"/>.
            /// </summary>
            /// <param name="activationFunction"></param>
            public void SetActivationFunctionForLayer(ActivationFunctionHandler.ActivationFunction activationFunction) {
                foreach (Neuron n in neurons) n.activationFunction = activationFunction;
            }

            /// <summary>
            /// Serializes the layer and its neurons and returns the output as a string
            /// </summary>
            /// <returns></returns>
            public string SerializeLayer() {
                string output = "";

                for (int i = 0; i < neurons.Count; i++) {
                    output += "neuron[\n";
                    output += neurons[i].SerializeNeuron();
                    output += "]\n";
                }
                return output;
            }

            void DeserializeLayer(string layerString) {
                Queue<string> neuronStrings = new Queue<string>(layerString.Split(new char[] { char.Parse("["), char.Parse("]") }).ToList());

                while (neuronStrings.Count > 0) {
                    switch (neuronStrings.Dequeue()) {
                        case "neuron":
                            neurons.Add(new Neuron(neuronStrings.Dequeue()));
                            break;
                    }
                }
            }
        }

        public class Neuron {
            public ActivationFunctionHandler.ActivationFunction activationFunction = ActivationFunctionHandler.ActivationFunction.ReLU;
            public bool isInputNeuron = false;
            public double inputValue = 0;
            public double bias = 0;
            public double outputValue = 0;
            public double errorGradient = 0;
            public List<double> weights = new List<double>();
            public List<double> inputs = new List<double>();

            /// <summary>
            /// Create a new input neuron
            /// </summary>
            public Neuron() => isInputNeuron = true;

            /// <summary>
            /// Create a new hidden or output neuron. <paramref name="nInputsToNeuron"/> defines the number of weights generated for neuron
            /// </summary>
            /// <param name="nInputsToNeuron"></param>
            public Neuron(int nInputsToNeuron) {
                if (nInputsToNeuron <= 0) return;

                bias = random.NextDouble() * (1 + 1) - 1;
                for (int i = 0; i < nInputsToNeuron; i++) {
                    weights.Add(random.NextDouble() * (1 + 1) - 1);
                }
            }

            /// <summary>
            /// Create a new neuron by deserializing the <paramref name="neuronString"/>
            /// </summary>
            /// <param name="neuronString"></param>
            public Neuron(string neuronString) => DeserializeNeuron(neuronString);

            /// <summary>
            /// Serializes the neuron's fields and return them as a string
            /// </summary>
            /// <returns></returns>
            public string SerializeNeuron() {
                string output = "";
                output += "AF:" + activationFunction + ";\n";
                output += "isInput:" + isInputNeuron + ";\n";
                output += "inputValue:" + inputValue + ";\n";
                output += "bias:" + bias + ";\n";
                output += "outputValue:" + outputValue + ";\n";
                output += "errorGradient:" + errorGradient + ";\n";
                for (int i = 0; i < weights.Count; i++) output += "weight:" + weights[i] + ";\n";
                for (int i = 0; i < inputs.Count; i++) output += "input:" + inputs[i] + ";\n";
                return output;
            }

            void DeserializeNeuron(string neuronString) {
                Queue<string> parts = new Queue<string>(neuronString.Split(char.Parse(";")).ToList());
                Queue<string> subparts;

                while (parts.Count > 0) {
                    subparts = new Queue<string>(parts.Dequeue().Split(char.Parse(":")).ToList());

                    switch (subparts.Dequeue()) {
                        case "AF":
                            activationFunction.Parse(subparts.Dequeue());
                            break;
                        case "isInput":
                            isInputNeuron = bool.Parse(subparts.Dequeue());
                            break;
                        case "inputValue":
                            inputValue = double.Parse(subparts.Dequeue());
                            break;
                        case "bias":
                            bias = double.Parse(subparts.Dequeue());
                            break;
                        case "outputValue":
                            outputValue = double.Parse(subparts.Dequeue());
                            break;
                        case "errorGradient":
                            errorGradient = double.Parse(subparts.Dequeue());
                            break;
                        case "weight":
                            weights.Add(double.Parse(subparts.Dequeue()));
                            break;
                        case "input":
                            inputs.Add(double.Parse(subparts.Dequeue()));
                            break;
                    }
                }
            }
        }
    }

    public static class ActivationFunctionHandler {
        public enum ActivationFunction { ReLU, Sigmoid, TanH }

        /// <summary>
        /// Parse the given string into a value from the ActivationFunction enum
        /// </summary>
        /// <param name="actFunc"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ActivationFunction Parse(this ActivationFunction actFunc, string value) {
            foreach (ActivationFunction af in Enum.GetValues(typeof(ActivationFunction))) {
                if (af.ToString().Equals(value)) return af;
            }
            throw new TypeLoadException("Couldn't find the requested value (" + value + ") in the ActivationFunction enum");
        }

        /// <summary>
        /// Triggers the given <paramref name="activationFunction"/>'s calculation of <paramref name="value"/> and returns the result
        /// </summary>
        /// <param name="activationFunction"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double TriggerActivationFunction(ActivationFunction activationFunction, double value) {
            switch (activationFunction) {
                case ActivationFunction.ReLU:
                    return ReLU(value);
                case ActivationFunction.Sigmoid:
                    return Sigmoid(value);
                case ActivationFunction.TanH:
                    return TanH(value);
                default:
                    throw new System.NullReferenceException("The activation function wasn't set properly!");
            }
        }

        /// <summary>
        /// Triggers the given <paramref name="activationFunction"/>'s derivative calculation of <paramref name="value"/> and returns the result
        /// </summary>
        /// <param name="activationFunction"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double TriggerDerativeFunction(ActivationFunction activationFunction, double value) {
            switch (activationFunction) {
                case ActivationFunction.ReLU:
                    return ReLUDerivative(value);
                case ActivationFunction.Sigmoid:
                    return SigmoidDerivative(value);
                case ActivationFunction.TanH:
                    return TanHDerivative(value);
                default:
                    throw new NullReferenceException("The activation function wasn't set properly!");
            }
        }

        static double ReLU(double value) { return value > 0 ? value : 0; }

        static double Sigmoid(double value) {
            double k = Math.Exp(value);
            return k / (1.0f + k);
        }

        static double TanH(double value) {
            double k = Math.Exp(-2 * value);
            return 2 / (1.0f + k) - 1;
        }

        static double SigmoidDerivative(double value) { return value * (1 - value); }

        static double ReLUDerivative(double value) { return value > 0 ? value : 0; }

        static double TanHDerivative(double value) { return 1 - value * value; }
    }

    /// <summary>
    /// The MLDebugger is used to debug the execution of any of the ML classes in the MachineLearning namespace.
    /// It can print to either the default console or the Unity Editor's console, depending on environment
    /// </summary>
    public static class MLDebugger {
        public static bool isRunning { get; private set; } = false;
        private static readonly Stopwatch operationStopwatch = new Stopwatch();
        private static readonly Stopwatch totalStopwatch = new Stopwatch();
        private static string output = "";
        private static int outputLineLength = 135;

        /// <summary>
        /// Enables debugging of the given CNN. This also enables debugging of the CNN's internal ANN
        /// </summary>
        /// <param name="cnn"></param>
        public static void EnableDebugging(CNN cnn) => cnn.EnableDebugging();

        /// <summary>
        /// Enables debugging of the given ANN
        /// </summary>
        /// <param name="ann"></param>
        public static void EnableDebugging(ANN ann) => ann.EnableDebugging();

        /// <summary>
        /// Start the total watch
        /// </summary>
        public static void Start() {
            if (isRunning) return;
            totalStopwatch.Start();
            operationStopwatch.Start();
            AddLineToOutput();
            isRunning = true;
        }

        /// <summary>
        /// Restart the watch used to mearsure an operation
        /// </summary>
        public static void RestartOperationWatch() {
            operationStopwatch.Reset();
            operationStopwatch.Start();
        }

        /// <summary>
        /// Add information to the debuggers output string
        /// </summary>
        /// <param name="state"></param>
        /// <param name="includeDurationTime"></param>
        public static void AddToDebugOutput(string state, bool includeDurationTime) {
            state = "|   Operation: " + state;
            if (includeDurationTime) {
                state += "   |   Duration: " + operationStopwatch.ElapsedMilliseconds + "ms";
                RestartOperationWatch();
            }

            state += "   |   Total time: " + totalStopwatch.ElapsedMilliseconds + "ms   |";

            output += state + "\n";
            AddLineToOutput();
        }

        static void AddLineToOutput() {
            string s = "|";
            while (s.Length <= outputLineLength - 1) {
                s += "-";
            }
            output += s + "|\n";
        }

        /// <summary>
        /// Print the generated output string to the console. Can write to either the default console or to Unity Developer Console
        /// </summary>
        public static void Print() {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(output);
#else
            Console.WriteLine(output);
#endif
            totalStopwatch.Reset();
            operationStopwatch.Reset();
            output = "";
        }
    }

    public static class MLSerializer {

        /// <summary>
        /// Serialize the given <paramref name="cnn"/> object and return the resulting string
        /// </summary>
        /// <param name="cnn"></param>
        /// <returns></returns>
        public static string SerializeCNN(CNN cnn) {
            if (cnn == null) throw new NullReferenceException("You didn't pass a CNN for serialization");
            string serializedCNN = "";

            //From this point, CNN informations are saved
            serializedCNN += "CNN{}\n";     //CNN (doesn't contain anything, since it doesn't have any single fields that are saved directly (only lists)

            //Filters
            foreach (CNN.CNNFilter filter in cnn.GetFilters()) {
                serializedCNN += "filter{";
                serializedCNN += filter.GetSerializedFilter();
                serializedCNN += "}\n";
            }

            //Convoluted maps
            foreach (float[,] map in cnn.GetConvolutedMaps()) {
                serializedCNN += "convolutedMap{";
                serializedCNN += cnn.SerializeMap(map);
                serializedCNN += "}\n";
            }

            //Pooled map
            foreach (float[,] map in cnn.GetPooledMaps()) {
                serializedCNN += "pooledMap{";
                serializedCNN += cnn.SerializeMap(map);
                serializedCNN += "}\n";
            }

            //Outputs
            serializedCNN += "outputs{";
            for (int i = 0; i < cnn.GetOutputs().Count; i++) {
                serializedCNN += cnn.GetOutputs()[i];
                if (i != cnn.GetOutputs().Count - 1) serializedCNN += ";";
            }
            serializedCNN += "}\n";

            //From this point, ANN informtaions are saved
            ANN ann = cnn.GetANN();
            serializedCNN += "ANN{\n";     //Start ANN. Opposite the CNN, the ANN contains some single fields, while also having lists
            serializedCNN += cnn.GetANN().SerializeANN();
            serializedCNN += "}";           //End ANN

            //Layers
            foreach (ANN.Layer layer in ann.GetLayers()) {
                serializedCNN += "layer{\n";
                serializedCNN += layer.SerializeLayer();
                serializedCNN += "}\n";
            }



            return serializedCNN;
        }

        /// <summary>
        /// Deserialize the given <paramref name="cnnString"/> and return a new CNN object
        /// </summary>
        /// <param name="cnnString"></param>
        /// <returns></returns>
        public static CNN DeserializeCNN(string cnnString) {
            if (cnnString.Equals("")) throw new ArgumentException("The given CNN string is empty");
            cnnString = Regex.Replace(cnnString, @"\t|\n|\r", "");
            Queue<string> cnnObjects = new Queue<string>(cnnString.Split(new char[] { char.Parse("{"), char.Parse("}") }).ToList());

            CNN cnn = new CNN();

            while (cnnObjects.Count > 0) {
                switch (cnnObjects.Dequeue()) {
                    case "filter":
                        cnn.ParseFilter(cnnObjects.Dequeue());
                        break;
                    case "convolutedMap":
                        cnn.ParseConvolutedMap(cnnObjects.Dequeue());
                        break;
                    case "pooledMap":
                        cnn.ParsePooledMap(cnnObjects.Dequeue());
                        break;
                    case "outputs":
                        cnn.ParseOutputs(cnnObjects.Dequeue());
                        break;
                    case "ANN":
                        cnn.ParseANN(cnnObjects.Dequeue());
                        break;
                    case "layer":
                        if (cnn.GetANN() == null) throw new NullReferenceException("DEV ERROR: ANN was not deserialized before the layer!");
                        cnn.GetANN().DeserializeLayer(cnnObjects.Dequeue());
                        break;
                }
            }

            return cnn;
        }
    }
}