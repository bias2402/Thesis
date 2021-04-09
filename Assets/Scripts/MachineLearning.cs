using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace MachineLearning {
    public enum AITypes { None, ANN, CNN }
    public class CNN {
        public enum LayerType { None, AveragePooling, Convolution, FullyConnected, GenerateANNInputs, MaxPooling, Padding }
        private bool isDebugging = false;
        //Calculation fields
        private List<CNNFilter> cnnFilters = new List<CNNFilter>();
        private List<float[,]> convolutedMaps = new List<float[,]>();
        private List<float[,]> pooledMaps = new List<float[,]>();
        private List<double> outputs = new List<double>();
        private ANN currentANN = null;
        private List<ANN> interalANNs = new List<ANN>();
        private Stack<ExecutionStep> executionMemory = new Stack<ExecutionStep>();

        /// <summary>
        /// Allows setting a custom made ANN as the CNN's internal ANN for the Fully-Connected layer calculations
        /// </summary>
        /// <param name="ann"></param>
        public void SetANN(ANN ann) => this.currentANN = ann;

        /// <summary>
        /// Enables debugging. This method should never be called from anything but the MLDebugger!
        /// </summary>
        public void EnableDebugging() => isDebugging = true;

        /// <summary>
        /// Clear the CNN completely. Set the parameters to only clear some parts
        /// </summary>
        /// <param name="clearExecutionMemory"></param>
        /// <param name="clearOutputs"></param>
        /// <param name="clearGeneratedMaps"></param>
        /// <param name="clearFilters"></param>
        /// <param name="clearANN"></param>
        public void Clear(bool clearExecutionMemory = true, bool clearOutputs = true, bool clearGeneratedMaps = true, bool clearFilters = true, bool clearANN = true) {
            if (isDebugging) MLDebugger.AddToDebugOutput("Clearing specified data from the CNN", false);
            if (clearExecutionMemory) executionMemory.Clear();
            if (clearOutputs) outputs.Clear();
            if (clearGeneratedMaps) convolutedMaps.Clear();
            if (clearFilters) cnnFilters.Clear();
            if (clearANN) currentANN = null;
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
        public ANN GetANN() { return currentANN; }

        /// <summary>
        /// Return the list of outputs from this CNN
        /// </summary>
        /// <returns></returns>
        public List<double> GetOutputs() { return outputs; }
        #endregion

        //CNN methods for generating outputs and training the network
        #region
        /// <summary>
        /// Run the CNN on the given <paramref name="input"/> using the given <paramref name="cnnConfig"/>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="cnnConfig"></param>
        /// <returns></returns>
        public List<double> Run(float[,] input, in Configuration.CNNConfig cnnConfig) {
            LayerType prevLayer = LayerType.None;
            int fcCount = 0;
            while (!cnnConfig.GetLayerInfo(out Configuration.CNNConfig.LayerInfo layer)) {        //While the end isn't reached
                switch (layer.layerType) {
                    case LayerType.AveragePooling:
                        //TO DO
                        break;
                    case LayerType.Convolution:
                        if (prevLayer == LayerType.Convolution) Convolution(new List<float[,]>(convolutedMaps), layer.af, layer.stride);
                        else if (prevLayer == LayerType.MaxPooling || prevLayer == LayerType.AveragePooling)
                            Convolution(new List<float[,]>(pooledMaps), layer.af, layer.stride);
                        else Convolution(new List<float[,]> { input }, layer.af, layer.stride);
                        break;
                    case LayerType.FullyConnected:
                        fcCount++;
                        if (prevLayer == LayerType.FullyConnected) {
                            if (interalANNs.Count < fcCount) interalANNs.Add(currentANN = new ANN(layer.annConfig, interalANNs[interalANNs.Count - 1].GetOutputs().Count));
                        } else {
                            List<float[,]> mapsForANN = prevLayer == LayerType.MaxPooling ? GetPooledMaps() : GetConvolutedMaps();
                            if (currentANN == null) if (interalANNs.Count == 0) interalANNs.Add(currentANN = new ANN(layer.annConfig, GetPixelCount(mapsForANN)));
                        }
                        switch (prevLayer) {
                            case LayerType.AveragePooling:
                            case LayerType.MaxPooling:
                                FullyConnected(GetPooledMaps(), "Pooled maps");
                                break;
                            case LayerType.Convolution:
                                FullyConnected(GetConvolutedMaps(), "Convoluted maps");
                                break;
                            case LayerType.FullyConnected:
                                FullyConnected(interalANNs[interalANNs.Count - 1], new List<double>(interalANNs[interalANNs.Count - 2].GetOutputs()));
                                break;
                        }
                        break;
                    case LayerType.MaxPooling:
                        if (prevLayer == LayerType.MaxPooling) MaxPooling(new List<float[,]>(pooledMaps), layer.af, layer.kernelDimension, layer.stride);
                        else MaxPooling(new List<float[,]>(convolutedMaps), layer.af, layer.kernelDimension, layer.stride);
                        break;
                    case LayerType.Padding:
                        switch (prevLayer) {
                            case LayerType.Convolution:
                                for (int i = 0; i < convolutedMaps.Count; i++) convolutedMaps[i] = Padding(convolutedMaps[i], false);
                                break;
                            case LayerType.MaxPooling:
                                for (int i = 0; i < pooledMaps.Count; i++) pooledMaps[i] = Padding(pooledMaps[i], false);
                                break;
                            case LayerType.None:
                            case LayerType.Padding:
                                input = Padding(input, false);
                                break;
                        }
                        break;
                }
                prevLayer = layer.layerType;
            }
            return outputs;
        }

        /// <summary>
        /// Run the CNN on the given <paramref name="input"/> using default settings and return outputs.
        /// Default settings: padding = 0, convolution stride = 1, pooling kernel = 2, pooling stride = 2, outputs = 3, ActivationFunction = Sigmoid
        /// Flow: Convolution, Pooling, Fully Connected (Decision Making)
        /// </summary>
        /// <param name="input"></param>
        public List<double> Run(float[,] input, int desiredNumberOfOutputs = 3) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting CNN default Running", false);
            }

            Convolution(new List<float[,]> { input }, ActivationFunctionHandler.ActivationFunction.Sigmoid);
            MaxPooling(convolutedMaps, ActivationFunctionHandler.ActivationFunction.Sigmoid);
            FullyConnected(pooledMaps, "pooled maps", desiredNumberOfOutputs);
            return outputs;
        }

        /// <summary>
        /// Train the CNN using the given <paramref name="input"/> and correcting for the given <paramref name="desiredOutputs"/>.
        /// The CNN is run using the given <paramref name="cnnConfig"/>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="desiredOutputs"></param>
        /// <param name="cnnConfig"></param>
        /// <returns></returns>
        public List<double> Train(float[,] input, List<double> desiredOutputs, in Configuration.CNNConfig cnnConfig) {
            outputs = Run(input, cnnConfig);
            Backpropagation(desiredOutputs);
            return outputs;
        }

        /// <summary>
        /// Train the CNN on the given <paramref name="input"/> using default settings and return outputs.
        /// Default settings: padding = 0, convolution stride = 1, pooling kernel = 2, pooling stride = 2, outputs = <paramref name="desiredOutputs"/>.Count,
        /// ActivationFunction = Sigmoid
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
            Convolution(new List<float[,]> { input }, ActivationFunctionHandler.ActivationFunction.Sigmoid);
            MaxPooling(convolutedMaps, ActivationFunctionHandler.ActivationFunction.Sigmoid);
            FullyConnected(pooledMaps, "pooled maps", desiredOutputs.Count);
            Backpropagation(desiredOutputs);

            return outputs;
        }

        /// <summary>
        /// Add padding of zeros to the inputted map (<paramref name="map"/>) increasing the size with two in both dimensions
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public float[,] Padding(float[,] map, bool backpropPadding) {
            if (isDebugging && !backpropPadding) {
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

            if (isDebugging && !backpropPadding) MLDebugger.AddToDebugOutput("Padding Layer complete", true);
            return newMap;
        }

        /// <summary>
        /// Run the convolution on <paramref name="map"/> resulting in a list of maps: one map per filter. Apply the activation function to each value of the new map after creation
        /// </summary>
        /// <param name="map"></param>
        /// <param name="af"></param>
        /// <param name="stride"></param>
        public List<float[,]> Convolution(List<float[,]> maps, ActivationFunctionHandler.ActivationFunction af, int stride = 1) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting convolution with " + maps.Count + " map(s), " + cnnFilters.Count +
                                            " filters, and a stride of " + stride, false);
                MLDebugger.RestartOperationWatch();
            }

            convolutedMaps.Clear();
            float value;
            double dimx, dimy;
            Coord newMapCoord, mapCoord;
            float[,] newMap;

            foreach (CNNFilter filter in cnnFilters) {
                foreach (float[,] map in maps) {
                    //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
                    //It is described and reference in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
                    dimx = Math.Ceiling((double)(1 + (double)(map.GetLength(0) - filter.dimensions) / stride));
                    dimy = Math.Ceiling((double)(1 + (double)(map.GetLength(1) - filter.dimensions) / stride));

                    //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
                    //new map could have the wrong dimensions, which would break the convolution!
                    if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                        throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or filter(s) so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                    newMap = new float[(int)dimx, (int)dimy];                               //The map is created using the dimensions calculated above
                    newMapCoord = new Coord(0, 0, newMap.GetLength(0), newMap.GetLength(1));                        //Coordinates on the new map, where a the calculated value will be placed
                    mapCoord = new Coord(0, 0, map.GetLength(0) - filter.dimensions, map.GetLength(1) - filter.dimensions); //Coordinates on the old map, from which the filter is applied

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
                    UnityEngine.Debug.Log("Conv AF " + maps.Count);
                    convolutedMaps.Add(ApplyActivationFunctionToMap(newMap, af));

                    executionMemory.Push(new ExecutionStep(LayerType.Convolution, map, convolutedMaps[convolutedMaps.Count - 1],
                        af, cnnFilters.FindIndex(x => x == filter), map.GetLength(0)));
                }
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Convolution Layer complete", true);
            return convolutedMaps;
        }

        /// <summary>
        /// Pool the convoluted maps, keeping the highest value found in the kernel for each stride.
        /// Kernel is a square of size <paramref name="kernelDimension"/>, and it is moved using <paramref name="stride"/>.
        /// </summary>
        /// <param name="af"></param>
        /// <param name="kernelDimension"></param>
        /// <param name="stride"></param>
        public List<float[,]> MaxPooling(List<float[,]> maps, ActivationFunctionHandler.ActivationFunction af, int kernelDimension = 2, int stride = 2) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting pooling of all maps with a kernel with dimensions of " + kernelDimension + ", and a stride of " + stride, false);
                MLDebugger.RestartOperationWatch();
            }

            pooledMaps.Clear();

            float[,] newMap;
            float?[,] derivedMap;
            Coord newMapCoord;
            Coord mapCoord;
            float maxValue = 0;
            foreach (float[,] map in maps) {
                //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
                //The formula is described and referenced in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
                double dimx = Math.Ceiling((double)(1 + (double)(map.GetLength(0) - kernelDimension) / stride));
                double dimy = Math.Ceiling((double)(1 + (double)(map.GetLength(1) - kernelDimension) / stride));

                //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
                //new map could have the wrong dimensions, which would break the pooling!
                if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                    throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or kernel dimension so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                newMap = new float[(int)dimx, (int)dimy];                           //The map will shrink during pooling, so the new map is smaller in both dimensions
                derivedMap = new float?[map.GetLength(0), map.GetLength(1)];                                //This map is created and saved for later use during backpropagation
                newMapCoord = new Coord(0, 0, newMap.GetLength(0), newMap.GetLength(1));                    //Coordinates on the new map, where a the calculated value will be placed
                mapCoord = new Coord(0, 0, map.GetLength(0) - kernelDimension, map.GetLength(1) - kernelDimension); //Coordinates on the old map, from which the filter is applied

                if (map.GetLength(0) == 1 && map.GetLength(1) == 1) {
                    newMap[0, 0] = map[0, 0];
                } else {
                    while (true) {
                        //Store values found through the kernel from current mapCoord
                        for (int y = mapCoord.y; y < mapCoord.y + kernelDimension; y++) {
                            for (int x = mapCoord.x; x < mapCoord.x + kernelDimension; x++) {
                                if (x == mapCoord.x && y == mapCoord.y) {
                                    derivedMap[x, y] = maxValue = map[x, y];
                                } else if (map[x, y] > maxValue) {
                                    derivedMap[x, y] = maxValue = map[x, y];

                                    if ((x - 1) < mapCoord.x) {
                                        derivedMap[x, y - 1] = null;
                                    } else {
                                        derivedMap[x - 1, y] = null;
                                    }
                                }
                            }
                        }

                        //Increment mapCoord
                        mapCoord.Increment(stride);

                        //Apply the value to newMap and increment newMapCoord
                        newMap[newMapCoord.x, newMapCoord.y] = maxValue;
                        if (newMapCoord.Increment()) break;
                    }
                }
                UnityEngine.Debug.Log("Pool AF");
                pooledMaps.Add(ApplyActivationFunctionToMap(newMap, af));
                executionMemory.Push(new ExecutionStep(LayerType.MaxPooling, derivedMap, af));
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Pooling Layer complete", true);
            return pooledMaps;
        }

        float[,] ApplyActivationFunctionToMap(float[,] map, ActivationFunctionHandler.ActivationFunction af) {
            for (int i = 0; i < map.GetLength(0); i++) {
                for (int j = 0; j < map.GetLength(1); j++) {
                    map[i, j] = (float)ActivationFunctionHandler.TriggerActivationFunction(af, map[i, j]);
                }
            }
            return map;
        }

        /// <summary>
        /// Run the list of <paramref name="maps"/> through the ANN (creates a new ANN using the <paramref name="nOutputs"/> parameter if necessary)
        /// and return the calculated outputs from the ANN. <paramref name="mapListsName"/> is used by the MLDebugger for debugging
        /// </summary>
        /// <param name="maps"></param>
        /// <param name="mapListsName"></param>
        /// <param name="nOutputs"></param>
        /// <returns></returns>
        public List<double> FullyConnected(List<float[,]> maps, string mapListsName, int nOutputs = 3) {
            //Generate a list of inputs
            List<double> inputs = GenerateANNInputs(maps, mapListsName);

            //Create a new ANN if one doesn't exist already
            if (currentANN == null) currentANN = new ANN(inputs.Count, 0, 0, nOutputs,
                ActivationFunctionHandler.ActivationFunction.Sigmoid, ActivationFunctionHandler.ActivationFunction.Sigmoid);
            if (!interalANNs.Contains(currentANN)) interalANNs.Add(currentANN);

            if (isDebugging) MLDebugger.EnableDebugging(currentANN);
            outputs = currentANN.Run(inputs);
            executionMemory.Push(new ExecutionStep(LayerType.FullyConnected, currentANN));

            if (isDebugging) MLDebugger.AddToDebugOutput("Fully Connected Layer complete", false);
            return outputs;
        }

        /// <summary>
        /// Run a premade <paramref name="ann"/> with the given <paramref name="inputs"/>
        /// </summary>
        /// <param name="ann"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public List<double> FullyConnected(ANN ann, List<double> inputs) {
            currentANN = ann;
            if (!interalANNs.Contains(ann)) interalANNs.Add(ann);

            if (isDebugging) MLDebugger.EnableDebugging(currentANN);
            outputs = currentANN.Run(inputs);
            executionMemory.Push(new ExecutionStep(LayerType.FullyConnected, currentANN));

            if (isDebugging) MLDebugger.AddToDebugOutput("Fully Connected Layer complete", false);
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
                executionMemory.Push(new ExecutionStep(LayerType.GenerateANNInputs, maps[i].GetLength(0)));
                for (int x = 0; x < maps[i].GetLength(0); x++) {
                    for (int y = 0; y < maps[i].GetLength(1); y++) {
                        inputs.Add(maps[i][x, y]);
                    }
                }
            }

            if (isDebugging) MLDebugger.AddToDebugOutput("Input generation complete with " + inputs.Count + " inputs for the ANN", false);
            return inputs;
        }

        int GetPixelCount(float[,] map) {
            int counter = 0;
            for (int y = 0; y < map.GetLength(1); y++) {
                for (int x = 0; x < map.GetLength(0); x++) {
                    counter++;
                }
            }
            return counter;
        }

        int GetPixelCount(List<float[,]> maps) {
            int counter = 0;
            foreach (float[,] map in maps) {
                for (int y = 0; y < map.GetLength(1); y++) {
                    for (int x = 0; x < map.GetLength(0); x++) {
                        counter++;
                    }
                }
            }
            return counter;
        }

        /// <summary>
        /// Perform the backpropagation of the CNN to update values of the ANN's and filters
        /// </summary>
        public void Backpropagation(List<double> desiredOutputs) {
            if (isDebugging) {
                MLDebugger.Start();
                MLDebugger.AddToDebugOutput("Starting backpropagation", false);
                MLDebugger.RestartOperationWatch();
            }

            int annIndex = interalANNs.Count - 1;
            Stack<double> annErrorAdjustedInputs = null;                        //This is a stack, as the values are outputted in the order they were inputted (not flipped)
            Queue<float[,]> errorMaps = new Queue<float[,]>();                  //This is a queue, as the process will flip the order along the way
            float[,] currentErrorMap;
            float[,] filter;
            int prevFilterIndex = -1;
            Coord pos;
            int convCount = 0, maxPoolCount = 0, fcCount = 0, avgPoolCount = 0, inGenCount = 0;
            string debug = "";

            while (executionMemory.Count > 0) {
                ExecutionStep exeStep = executionMemory.Pop();
                switch (exeStep.operation) {
                    case LayerType.AveragePooling:
                        //TO DO: Generate error masks
                        avgPoolCount++;
                        break;
                    case LayerType.Convolution:
                        currentErrorMap = errorMaps.Dequeue();
                        filter = cnnFilters[exeStep.filterIndex].filter;
                        float[,] newErrorMap;
                        if (prevFilterIndex != exeStep.filterIndex) {
                            newErrorMap = exeStep.inputMap;
                        } else {
                            newErrorMap = errorMaps.ToList()[errorMaps.Count - 1];
                        }

                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPre-AF errorMap: " + SerializeMap(currentErrorMap);
                        //Apply the derivative activation function to each value of the mask
                        for (int y = 0; y < currentErrorMap.GetLength(1); y++) {
                            for (int x = 0; x < currentErrorMap.GetLength(0); x++) {
                                currentErrorMap[x, y] = (float)ActivationFunctionHandler.TriggerDerativeFunction(exeStep.af, currentErrorMap[x, y]);
                            }
                        }
                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPost-AF errorMap: " + SerializeMap(currentErrorMap);

                        //Pos is the current upper-left corner of the input map and the current position in the filter (because of positional relation when doing the comparison process)
                        pos = new Coord(0, 0, exeStep.inputMap.GetLength(0) - currentErrorMap.GetLength(0),
                                                                 exeStep.inputMap.GetLength(1) - currentErrorMap.GetLength(1));

                        float delta = 0;
                        //While pos doesn't reach its end, for each position of pos iterate through the errorMask and multiply with the input value at the same position
                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPre-FilterUpdate: " + SerializeMap(filter);
                        do {
                            for (int y = 0; y < exeStep.outputMap.GetLength(1); y++) {
                                for (int x = 0; x < exeStep.outputMap.GetLength(0); x++) {
                                    delta += currentErrorMap[x, y] * exeStep.inputMap[pos.x + x, pos.y + y];
                                    newErrorMap[pos.x + x, pos.y + y] += delta;
                                    if (delta > 1000 || delta < -1000) 
                                        UnityEngine.Debug.Log("Uhm, delta? " + delta + ", " + currentErrorMap[x, y] + ", " + exeStep.inputMap[pos.x + x, pos.y + y]);
                                }
                            }
                            filter[pos.x, pos.y] += delta;
                        } while (!pos.Increment());
                        if (isDebugging && MLDebugger.depth >= 2) debug += "\nPost-FilterUpdate: " + SerializeMap(filter);

                        cnnFilters[exeStep.filterIndex].filter = filter;

                        convCount++;

                        if (prevFilterIndex != exeStep.filterIndex) {
                            if (exeStep.mapDimensions > newErrorMap.GetLength(0)) errorMaps.Enqueue(Padding(newErrorMap, true));
                            else errorMaps.Enqueue(newErrorMap);
                            prevFilterIndex = exeStep.filterIndex;
                        } else {
                            List<float[,]> modifiedErrorMapsQueue = errorMaps.ToList();
                            modifiedErrorMapsQueue.RemoveAt(modifiedErrorMapsQueue.Count - 1);
                            errorMaps = new Queue<float[,]>(modifiedErrorMapsQueue);
                            if (exeStep.mapDimensions > newErrorMap.GetLength(0)) errorMaps.Enqueue(Padding(newErrorMap, true));
                            else errorMaps.Enqueue(newErrorMap);
                        }
                        break;
                    case LayerType.FullyConnected:
                        currentANN = interalANNs[annIndex];
                        annIndex--;
                        exeStep.ann.Backpropagation(annErrorAdjustedInputs == null ? desiredOutputs : annErrorAdjustedInputs.ToList(), true);
                        annErrorAdjustedInputs = new Stack<double>(currentANN.inputErrors);

                        if (isDebugging && MLDebugger.depth >= 3) {
                            debug += "\nAnnErrorAdjustedInputs: | ";
                            foreach (double d in annErrorAdjustedInputs) {
                                debug += d + " | ";
                            }
                        }

                        fcCount++;
                        break;
                    case LayerType.GenerateANNInputs:
                        newErrorMap = new float[exeStep.mapDimensions, exeStep.mapDimensions];
                        for (int y = 0; y < newErrorMap.GetLength(1); y++) {
                            for (int x = 0; x < newErrorMap.GetLength(0); x++) {
                                newErrorMap[x, y] = (float)annErrorAdjustedInputs.Pop();
                            }
                        }
                        errorMaps.Enqueue(newErrorMap);

                        if (isDebugging && MLDebugger.depth >= 2) debug += "\nInputErrorMap: " + SerializeMap(newErrorMap);

                        inGenCount++;
                        break;
                    case LayerType.MaxPooling:
                        currentErrorMap = errorMaps.Dequeue();

                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPre-AF InputErrorMap: " + SerializeMap(currentErrorMap);
                        for (int y = 0; y < currentErrorMap.GetLength(1); y++) {
                            for (int x = 0; x < currentErrorMap.GetLength(0); x++) {
                                currentErrorMap[x, y] = (float)ActivationFunctionHandler.TriggerDerativeFunction(exeStep.af, currentErrorMap[x, y]);
                            }
                        }
                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPost-AF InputErrorMap: " + SerializeMap(currentErrorMap);

                        if (isDebugging && MLDebugger.depth >= 3) debug += "\nPre-Mask: " + SerializeMap(exeStep.mask);
                        Coord errorMapCoord = new Coord(0, 0, currentErrorMap.GetLength(0), currentErrorMap.GetLength(1));
                        float[,] newMap = new float[exeStep.mask.GetLength(0), exeStep.mask.GetLength(1)];
                        for (int y = 0; y < exeStep.mask.GetLength(1); y++) {
                            for (int x = 0; x < exeStep.mask.GetLength(0); x++) {
                                if (exeStep.mask[x, y] == null) {
                                    newMap[x, y] = 0;
                                } else {
                                    newMap[x, y] = currentErrorMap[errorMapCoord.x, errorMapCoord.y];
                                    errorMapCoord.Increment();
                                }
                            }
                        }

                        errorMaps.Enqueue(newMap);
                        if (isDebugging && MLDebugger.depth >= 2) debug += "\nPost-Mask: " + SerializeMap(newMap);

                        maxPoolCount++;
                        break;
                }
            }
            if (isDebugging) {
                if (MLDebugger.depth >= 2) MLDebugger.AddToDebugOutput("CNN Backpropagation:" + debug + "\n", false, false);
                MLDebugger.AddToDebugOutput("Backpropagation completed. " + convCount + " x Convolution, " + maxPoolCount + " x MaxPooling, " + avgPoolCount +
                                                         " x AveragePooling, " + inGenCount + " x InputGeneration, " + fcCount + " x FullyConnected", true);
            }
        }
        #endregion

        //Serilization and deserialization methods, used by the MLSerializer
        #region
        public string SerializeMap(float[,] map) {
            string output = " | ";
            for (int j = 0; j < map.GetLength(1); j++) {
                for (int i = 0; i < map.GetLength(0); i++) {
                    output += map[i, j].ToString() + " | ";
                }
            }
            return output;
        }

        public string SerializeMap(float?[,] map) {
            string output = " | ";
            for (int j = 0; j < map.GetLength(1); j++) {
                for (int i = 0; i < map.GetLength(0); i++) {
                    if (map[i, j] == null) output += "null | ";
                    else output += map[i, j].ToString() + " | ";
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
        public void ParseANN(string annString) => currentANN = new ANN(annString);
        #endregion

        public class CNNFilter {
            public string filterName = "";
            public int dimensions = 3;
            public float[,] filter;

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
                            dimensions = int.Parse(filterParts.Dequeue());
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
                s += "filter: | ";
                for (int i = 0; i < filter.GetLength(0); i++) {
                    for (int j = 0; j < filter.GetLength(1); j++) {
                        s += filter[i, j].ToString() + " | ";
                    }
                }
                s += ";";
                return s;
            }

            void ParseSerializedFilter(string filterString) {
                string[] values = filterString.Split(char.Parse("|"));
                filter = new float[(int)dimensions, (int)dimensions];
                Coord coord = new Coord(0, 0, (int)dimensions, (int)dimensions);
                foreach (string s in values) {
                    filter[coord.x, coord.y] = float.Parse(s.ToString());
                    coord.Increment();
                }
            }
        }

        struct Coord {
            public int x { get; private set; }
            public int y { get; private set; }
            public int xMin { get; }
            public int yMin { get; }
            public int xMax { get; }
            public int yMax { get; }

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

        struct ExecutionStep {
            public LayerType operation { get; }
            public float[,] inputMap { get; }
            public float[,] outputMap { get; }
            public int filterIndex { get; }
            public float?[,] mask { get; }
            public int mapDimensions { get; }
            public ActivationFunctionHandler.ActivationFunction af { get; }
            public Queue<double> annInputs { get; }
            public ANN ann { get; }

            /// <summary>
            /// Constructor for the fully-connected layer
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="ann"></param>
            public ExecutionStep(LayerType operation, ANN ann) {
                this.ann = ann;
                this.operation = operation;
                annInputs = null;
                mapDimensions = 0;
                inputMap = null;
                outputMap = null;
                mask = null;
                filterIndex = 0;
                af = ActivationFunctionHandler.ActivationFunction.None;
            }

            /// <summary>
            /// Constructor for the input generation layer
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="mapDimensions"></param>
            public ExecutionStep(LayerType operation, int mapDimensions) {
                this.operation = operation;
                this.mapDimensions = mapDimensions;
                ann = null;
                annInputs = null;
                inputMap = null;
                outputMap = null;
                mask = null;
                filterIndex = 0;
                af = ActivationFunctionHandler.ActivationFunction.None;
            }

            /// <summary>
            /// Constructor for the convolutional layer
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="inputMap"></param>
            /// <param name="outputMap"></param>
            /// <param name="af"></param>
            /// <param name="filterIndex"></param>
            public ExecutionStep(LayerType operation, float[,] inputMap, float[,] outputMap, ActivationFunctionHandler.ActivationFunction af, int filterIndex,
                int mapDimensions) {
                this.operation = operation;
                this.inputMap = inputMap;
                this.outputMap = outputMap;
                this.af = af;
                this.filterIndex = filterIndex;
                mask = null;
                this.mapDimensions = mapDimensions;
                ann = null;
                annInputs = null;
            }

            /// <summary>
            /// Constructor for the max pooling layer
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="inputMap"></param>
            /// <param name="outputMap"></param>
            /// <param name="af"></param>
            /// <param name="filterIndex"></param>
            public ExecutionStep(LayerType operation, float?[,] mask, ActivationFunctionHandler.ActivationFunction af) {
                this.operation = operation;
                this.mask = mask;
                this.af = af;
                inputMap = null;
                outputMap = null;
                filterIndex = 0;
                mapDimensions = 0;
                ann = null;
                annInputs = null;
            }
        }
    }

    public class ANN {
        private int epochs = 1000;
        private double alpha = 0.05;
        private List<Layer> layers = new List<Layer>();
        private static Random random = new Random();
        private bool isDebugging = false;
        public List<double> inputErrors { get; private set; }

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
        public ANN(int nInputsN, int nHiddenL, int nhiddenN, int nOutputN, ActivationFunctionHandler.ActivationFunction hiddenLAF,
            ActivationFunctionHandler.ActivationFunction outputLAF, int epochs = 1000, double alpha = 0.05) {
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
        /// Create a new ANN based on the given <paramref name="annConfig"/>
        /// </summary>
        /// <param name="annConfig"></param>
        public ANN(Configuration.ANNConfig annConfig, int nInputNeurons) {
            epochs = annConfig.epochs;
            alpha = annConfig.alpha;

            //Create the input layer
            layers.Add(new Layer(nInputNeurons));

            //Create the hidden layers
            for (int i = 0; i < annConfig.nHiddenLayers; i++) {
                layers.Add(new Layer(annConfig.nHiddenNeuronsPerLayer, layers[layers.Count - 1], annConfig.hiddenAF));
            }

            //Create the output layer
            layers.Add(new Layer(annConfig.nOutputNeurons, layers[layers.Count - 1], annConfig.outputAF));
        }

        /// <summary>
        /// Creates a new ANN from an <paramref name="annString"/> by deserializing the ANN
        /// </summary>
        /// <param name="annString"></param>
        public ANN(string annString) => DeserializeANN(annString);

        /// <summary>
        /// Enables debugging. This method should never be called from anything but the MLDebugger!
        /// </summary>
        public void EnableDebugging() => isDebugging = true;

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
        /// Number of iterations is based on the given <paramref name="epochs"/> parameter
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="desiredOutputs"></param>
        /// <param name="epochs"></param>
        /// <returns></returns>
        public List<double> Train(List<double> inputs, List<double> desiredOutputs, int epochs) {
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
        /// Number of iterations is based on the network's 'epochs' setting
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
        /// Number of iterations is based on the amount of data sets nested in lists, as well as the network's 'epochs' setting. The network will
        /// iterate each list in input times epochs (which can become a lot)
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

                        //Ensure that the neuron's output is above the threshold for activation
                        if (neuron.outputValue < neuron.activationThreshold) neuron.outputValue = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Backpropagation for the ANN to update weights and biases of the neurons. If <paramref name="saveWeightSumForEachInputNeurons"/> is true, a list is generated to save the
        /// error of the inputs (used for CNN backpropagation)
        /// </summary>
        /// <param name="desiredOutputs"></param>
        /// <param name="saveWeightSumForEachInputNeurons"
        public void Backpropagation(List<double> desiredOutputs, bool saveWeightSumForEachInputNeurons = false) {
            int outputLayer = layers.Count - 1;
            int hiddenLayers = layers.Count > 2 ? layers.Count - 2 : 0;
            Neuron neuron;

            string output = "";
            if (isDebugging && MLDebugger.depth >= 2) {
                output += "\nDesiredOuputs: | ";
                foreach (double d in desiredOutputs) output += d + " | ";

                output += "\nActualOutputs: | ";
                foreach (Neuron n in layers[outputLayer].neurons) output += n.outputValue + " | ";
            }

            //Output layer
            for (int i = 0; i < layers[outputLayer].neurons.Count; i++) {
                if (isDebugging && MLDebugger.depth >= 2) output += "\nOutNeuronBackprop: | ";
                neuron = layers[outputLayer].neurons[i];

                //Calculate the error and errorGradient
                double error = desiredOutputs[i] - neuron.outputValue;
                double errorGradient = ActivationFunctionHandler.TriggerDerativeFunction(neuron.activationFunction, neuron.outputValue * error);

                if (isDebugging && MLDebugger.depth >= 2) {
                    output += "Error: " + error + " | ";
                    output += "Gradient: " + errorGradient + " | ";
                }
                if (isDebugging && MLDebugger.depth >= 3) {
                    output += "OldWeights: | ";
                    foreach (double w in neuron.weights) {
                        output += w + " | ";
                    }
                }
                if (isDebugging && MLDebugger.depth >= 3) output += "UpdatedWeights: | ";

                //Update the neuron's weights
                for (int j = 0; j < neuron.weights.Count; j++) {
                    neuron.weights[j] += alpha * neuron.inputValue * error;
                    if (isDebugging && MLDebugger.depth >= 3) output += neuron.weights[j] + " | ";
                }

                //Update the neuron's bias and errorGradient
                neuron.bias = alpha * -1 * errorGradient;
                neuron.errorGradient = errorGradient;

                if (isDebugging && MLDebugger.depth >= 2) output += "Bias: " + neuron.bias + " | ";
            }

            //Hidden layers
            if (hiddenLayers != 0) {
                for (int i = hiddenLayers; i > 0; i--) {
                    if (isDebugging && MLDebugger.depth >= 2) output += "\nHidden" + (layers.Count - 1 - i) + "NeuronBackprop: | ";

                    //Calculate the errorGradientSum for the previous layer
                    double errorGradientSum = 0;
                    for (int j = 0; j < layers[i + 1].neurons.Count; j++) {
                        errorGradientSum += layers[i + 1].neurons[j].errorGradient;
                    }

                    if (isDebugging && MLDebugger.depth >= 2) output += "GradientSum: " + errorGradientSum + " | ";
                    if (isDebugging && MLDebugger.depth >= 3) output += "UpdatedWeights: | ";

                    //Update the neurons in this hidden layer
                    for (int j = 0; j < layers[i].neurons.Count; j++) {
                        neuron = layers[i].neurons[j];

                        //Calculate the errorGradient
                        double errorGradient = ActivationFunctionHandler.TriggerDerativeFunction(neuron.activationFunction, outputLayer) * errorGradientSum;

                        //Update the neuron's weights
                        for (int k = 0; k < neuron.weights.Count; k++) {
                            neuron.weights[k] += alpha * neuron.inputValue * errorGradient;
                            if (isDebugging && MLDebugger.depth >= 3) output += neuron.weights[k] + " | ";
                        }

                        //Update the neuron's bias and errorGradient
                        neuron.bias = alpha * -1 * neuron.errorGradient;
                        neuron.errorGradient = errorGradient;

                        if (isDebugging && MLDebugger.depth >= 2) {
                            output += "Gradient: " + errorGradient + " | ";
                            output += "Bias: " + neuron.bias + " | ";
                        }
                    }
                }
            }

            if (isDebugging && MLDebugger.depth >= 2) MLDebugger.AddToDebugOutput("ANN Backpropagation:" + output + "\n", false, false);

            //If the backpropgation is part of the CNN, this will be required for extracting the errors
            if (saveWeightSumForEachInputNeurons) {
                inputErrors = new List<double>();
                double value;
                for (int i = 0; i < layers[0].neurons.Count; i++) {
                    value = 0;
                    foreach (Neuron n in layers[1].neurons) {
                        value += layers[0].neurons[i].inputValue * n.weights[i] * alpha;
                    }
                    inputErrors.Add(value);
                }
            }
        }
        #endregion

        //Deserialization methods, used by the MLSerializer
        #region
        public string SerializeANN() {
            string output = "";
            output += "alpha:" + alpha + "; ";
            output += "epochs:" + epochs + "; ";
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
                    output += "neuron[ ";
                    output += neurons[i].SerializeNeuron();
                    output += "]";
                    if (i != neurons.Count - 1) output += "\n";
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
            public double activationThreshold = 0;

            /// <summary>
            /// Create a new input neuron
            /// </summary>
            public Neuron() => isInputNeuron = true;

            /// <summary>
            /// Create a new hidden or output neuron. <paramref name="nInputsToNeuron"/> defines the number of weights generated for neuron.
            /// The <paramref name="activationThreshold"/> can be set to adjust the required value for the neuron to activate
            /// </summary>
            /// <param name="nInputsToNeuron"></param>
            /// <param name="activationThreshold"></param>
            public Neuron(int nInputsToNeuron, double activationThreshold = 0) {
                if (nInputsToNeuron <= 0) return;

                bias = random.NextDouble() * (1 + 1) - 1;
                for (int i = 0; i < nInputsToNeuron; i++) {
                    weights.Add(random.NextDouble() * (1 + 1) - 1);
                }
                this.activationThreshold = activationThreshold;
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
                output += "AF:" + activationFunction + "; ";
                output += "isInput:" + isInputNeuron + "; ";
                output += "inputValue:" + inputValue + "; ";
                output += "bias:" + bias + "; ";
                output += "outputValue:" + outputValue + "; ";
                output += "errorGradient:" + errorGradient + "; ";
                output += "activationThreshold:" + activationThreshold + "; ";
                for (int i = 0; i < weights.Count; i++) output += "weight:" + weights[i] + "; ";
                for (int i = 0; i < inputs.Count; i++) output += "input:" + inputs[i] + "; ";
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
                        case "activationThreshold":
                            activationThreshold = double.Parse(subparts.Dequeue());
                            break;
                    }
                }
            }
        }
    }

    public static class ActivationFunctionHandler {
        public enum ActivationFunction { None, ReLU, Sigmoid, TanH }

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
                case ActivationFunction.None:
                    return value;
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
                case ActivationFunction.None:
                    return value;
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
            if (double.IsInfinity(k)) throw new NotFiniteNumberException("To infinity and... Oh, we already reached infinity?");
            return k / (1.0f + k);
        }

        static double TanH(double value) {
            double k = Math.Exp(-2 * value);
            if (double.IsInfinity(k)) throw new NotFiniteNumberException("To infinity and... Oh, we already reached infinity?");
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
        private static string deepOutput = "";
        private static int outputLineLength = 135;
        /// <summary>
        /// The depth of the debugger decides the amount of details that are included
        /// 1: Operations and times; 
        /// 2: Include stats and results from operations; 
        /// 3: Include every detail from the operations;
        /// </summary>
        public static int depth { get; private set; } = 1;

        /// <summary>
        /// Set the depth of the debugger, thereby including more details. 
        /// 1: Operations and times; 
        /// 2: Include stats and results from operations; 
        /// 3: Include every detail from the operations;
        /// </summary>
        public static void SetDepth(int depth) => MLDebugger.depth = depth <= 1 ? 1 : depth >= 3 ? 3 : depth;

        /// <summary>
        /// Enables debugging of the given CNN. This also enables debugging of the CNN's internal ANN. 
        /// Set <paramref name="depth"/> to increase the amount of details that are recorded
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="depth"></param>
        public static void EnableDebugging(CNN cnn, int? depth = null) {
            cnn.EnableDebugging();
            if (depth != null) SetDepth((int)depth);

        }

        /// <summary>
        /// Enables debugging of the given ANN.
        /// Set <paramref name="depth"/> to increase the amount of details that are recorded
        /// </summary>
        /// <param name="ann"></param>
        /// <param name="depth"></param>
        public static void EnableDebugging(ANN ann, int? depth = null) {
            ann.EnableDebugging();
            if (depth != null) SetDepth((int)depth);
        }

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
        public static void AddToDebugOutput(string state, bool includeDurationTime, bool isOperation = true) {
            if (isOperation) {
                state = "|   Operation: " + state;
                if (includeDurationTime) {
                    state += "   |   Duration: " + operationStopwatch.Elapsed.TotalMilliseconds * 1000 + "μs";
                    RestartOperationWatch();
                }

                state += "   |   Total time: " + totalStopwatch.Elapsed.TotalMilliseconds * 1000 + "μs   |";

                output += state + "\n";
                AddLineToOutput();
            } else deepOutput += state;
        }

        static void AddLineToOutput() {
            string s = "|";
            while (s.Length <= outputLineLength - 1) {
                s += "-";
            }
            output += s + "|\n";
        }

        /// <summary>
        /// Resets the debugger and returns the output string
        /// </summary>
        public static string GetOutputAndReset() {
            string temp = output + deepOutput;

            totalStopwatch.Reset();
            operationStopwatch.Reset();
            output = "";
            deepOutput = "";
            isRunning = false;

            return temp;
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
            serializedCNN += "CNN { }\n";     //CNN (doesn't contain anything, since it doesn't have any single fields that are saved directly (only lists)

            //Filters
            foreach (CNN.CNNFilter filter in cnn.GetFilters()) {
                serializedCNN += "filter { ";
                serializedCNN += filter.GetSerializedFilter();
                serializedCNN += " }\n";
            }

            //Convoluted maps
            foreach (float[,] map in cnn.GetConvolutedMaps()) {
                serializedCNN += "convolutedMap { ";
                serializedCNN += cnn.SerializeMap(map);
                serializedCNN += " }\n";
            }

            //Pooled map
            foreach (float[,] map in cnn.GetPooledMaps()) {
                serializedCNN += "pooledMap { ";
                serializedCNN += cnn.SerializeMap(map);
                serializedCNN += " }\n";
            }

            //Outputs
            serializedCNN += "outputs { ";
            for (int i = 0; i < cnn.GetOutputs().Count; i++) {
                serializedCNN += cnn.GetOutputs()[i];
                if (i != cnn.GetOutputs().Count - 1) serializedCNN += ";";
            }
            serializedCNN += " }\n";

            //From this point, ANN informations are saved
            ANN ann = cnn.GetANN();
            serializedCNN += "ANN { ";     //Start ANN. Opposite the CNN, the ANN contains some single fields, while also having lists
            serializedCNN += cnn.GetANN().SerializeANN();

            //Layers
            foreach (ANN.Layer layer in ann.GetLayers()) {
                serializedCNN += "layer { \n";
                serializedCNN += layer.SerializeLayer();
                serializedCNN += " } ";
            }
            serializedCNN += " } ";           //End ANN

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

    public abstract class Configuration {
        protected static bool isDeserializingCNN = false;
        public AITypes aiType { get; private set; } = AITypes.None;
        public string id { get; private set; }

        public Configuration(AITypes aiType, string id) {
            this.aiType = aiType;
            this.id = id;
        }

        /// <summary>
        /// Serialize the given <paramref name="cnnConfig"/>
        /// </summary>
        /// <param name="cnnConfig"></param>
        /// <returns></returns>
        public static string Serialize(CNNConfig cnnConfig) { return "<!---" + "\n" + cnnConfig.SerializeCNN() + "\n" + "---!>"; }

        /// <summary>
        /// Serialize the given <paramref name="annConfig"/>
        /// </summary>
        /// <param name="annConfig"></param>
        /// <returns></returns>
        public static string Serialize(ANNConfig annConfig) { return "<!---" + "\n" + annConfig.SerializeANN() + "\n" + "---!>"; }

        protected abstract string SerializeCNN();

        protected abstract string SerializeANN();

        public static CNNConfig DeserializeCNN(string path, StreamReader sr = null) {
            isDeserializingCNN = true;
            CNNConfig cnnConfig = null;
            if (sr == null) sr = new StreamReader(path);
            string input;
            string[] inputParts;

            while ((input = sr.ReadLine()) != "---!>") {
                if (input == null) break;
                inputParts = input.Split(':');
                switch (inputParts[0]) {
                    case "#CNNConfig":
                        cnnConfig = new CNNConfig(AITypes.CNN, inputParts[1].Trim());
                        break;
                    case "Layer":
                        cnnConfig.Deserialize(sr, inputParts[1].Trim());
                        break;
                }
            }

            isDeserializingCNN = false;
            sr.Close();
            return cnnConfig;
        }

        public static ANNConfig DeserializeANN(string path, StreamReader sr = null) {
            if (sr == null) sr = new StreamReader(path);
            ANNConfig annConfig = null;
            string input;
            string[] inputParts;

            while ((input = sr.ReadLine()) != "---!>") {
                inputParts = input.Split(':');
                switch (inputParts[0]) {
                    case "#ANNConfig":
                        annConfig = new ANNConfig(AITypes.CNN, inputParts[1].Trim());
                        annConfig.Deserialize(sr);
                        if (isDeserializingCNN) return annConfig;
                        break;
                }
            }
            if (!isDeserializingCNN) sr.Close();
            return annConfig;
        }

        protected abstract void Deserialize(StreamReader sr, string additionalInfo = "");

        public class CNNConfig : Configuration {
            public List<LayerInfo> layers { get; private set; }
            private int index = 0;

            public CNNConfig(AITypes aiType, string id) : base(aiType, id) => layers = new List<LayerInfo>();

            /// <summary>
            /// Configure and add a new padding layer
            /// </summary>
            public void AddLayer() => layers.Add(new LayerInfo(CNN.LayerType.Padding));

            /// <summary>
            /// Configure and add a new convolutional layer
            /// </summary>
            /// <param name="af"></param>
            /// <param name="stride"></param>
            public void AddLayer(ActivationFunctionHandler.ActivationFunction af, int stride) =>
                layers.Add(new LayerInfo(CNN.LayerType.Convolution, af, stride));

            /// <summary>
            /// Configure and add a new max-pooling layer
            /// </summary>
            /// <param name="af"></param>
            /// <param name="stride"></param>
            /// <param name="kernelDimension"></param>
            public void AddLayer(ActivationFunctionHandler.ActivationFunction af, int stride, int kernelDimension) =>
                layers.Add(new LayerInfo(CNN.LayerType.MaxPooling, af, stride, kernelDimension));

            /// <summary>
            /// Configure and add a new fully-connected layer
            /// </summary>
            /// <param name="annConfig"></param>
            public void AddLayer(ANNConfig annConfig) => layers.Add(new LayerInfo(CNN.LayerType.FullyConnected, annConfig));

            /// <summary>
            /// Get the next layer of the config
            /// </summary>
            /// <returns></returns>
            public bool GetLayerInfo(out LayerInfo layer) {
                bool hasReachedEnd = false;
                if (index >= layers.Count) {
                    index = 0;
                    hasReachedEnd = true;
                }
                layer = layers[index];
                if (!hasReachedEnd) index++;
                return hasReachedEnd;
            }

            protected override string SerializeCNN() {
                string output = "#CNNConfig:" + id + "\n";
                foreach (LayerInfo l in layers) {
                    output += l.Serialize() + "\n";
                }
                return output;
            }

            protected override string SerializeANN() => throw new AccessViolationException("You shouldn't reach this with a CNNConfig!");

            protected override void Deserialize(StreamReader sr, string layer) {
                CNN.LayerType layerType = Parse(layer);
                ActivationFunctionHandler.ActivationFunction af = ActivationFunctionHandler.ActivationFunction.None;
                switch (layerType) {
                    case CNN.LayerType.AveragePooling:
                    case CNN.LayerType.MaxPooling:
                        AddLayer(af.Parse(sr.ReadLine().Split(':')[1].Trim()),
                                 int.Parse(sr.ReadLine().Split(':')[1].Trim()),
                                 int.Parse(sr.ReadLine().Split(':')[1].Trim()));
                        break;
                    case CNN.LayerType.Convolution:
                        AddLayer(af.Parse(sr.ReadLine().Split(':')[1].Trim()),
                                 int.Parse(sr.ReadLine().Split(':')[1].Trim()));
                        break;
                    case CNN.LayerType.FullyConnected:
                        AddLayer(DeserializeANN("", sr));
                        break;
                    case CNN.LayerType.Padding:
                        AddLayer();
                        break;
                }
            }

            private CNN.LayerType Parse(string value) {
                foreach (CNN.LayerType lt in Enum.GetValues(typeof(CNN.LayerType))) {
                    if (lt.ToString().Equals(value)) return lt;
                }
                throw new TypeLoadException("Couldn't find the requested value (" + value + ") in the LayerType enum");
            }

            public class LayerInfo {
                public CNN.LayerType layerType { get; }
                public ActivationFunctionHandler.ActivationFunction af { get; }
                public int stride { get; }
                public int kernelDimension { get; }
                public ANNConfig annConfig { get; }

                public LayerInfo(CNN.LayerType layerType, ActivationFunctionHandler.ActivationFunction af, int stride) {
                    this.layerType = layerType;
                    this.af = af;
                    this.stride = stride;
                }

                public LayerInfo(CNN.LayerType layerType, ActivationFunctionHandler.ActivationFunction af, int stride, int kernelDimension) {
                    this.layerType = layerType;
                    this.af = af;
                    this.stride = stride;
                    this.kernelDimension = kernelDimension;
                }

                public LayerInfo(CNN.LayerType layerType, ANNConfig annConfig) {
                    this.layerType = layerType;
                    this.annConfig = annConfig;
                }

                public LayerInfo(CNN.LayerType layerType) => this.layerType = layerType;

                public string Serialize() {
                    string output = "Layer: " + layerType.ToString() + "\n";
                    switch (layerType) {
                        case CNN.LayerType.AveragePooling:
                        case CNN.LayerType.MaxPooling:
                            output += "ActivationFunction: " + af + "\n";
                            output += "Stride: " + stride + "\n";
                            output += "KernelDimension: " + kernelDimension + "\n";
                            break;
                        case CNN.LayerType.Convolution:
                            output += "ActivationFunction: " + af + "\n";
                            output += "Stride: " + stride + "\n";
                            break;
                        case CNN.LayerType.FullyConnected:
                            output += "ANNConfig:\n" + annConfig.SerializeANN();
                            break;
                        case CNN.LayerType.GenerateANNInputs:
                        case CNN.LayerType.Padding:
                        case CNN.LayerType.None:
                            break;
                    }
                    return output;
                }
            }
        }

        public class ANNConfig : Configuration {
            public double alpha { get; private set; }
            public int epochs { get; private set; }
            public int nHiddenNeuronsPerLayer { get; private set; }
            public int nHiddenLayers { get; private set; }
            public int nOutputNeurons { get; private set; }
            public ActivationFunctionHandler.ActivationFunction hiddenAF { get; private set; }
            public ActivationFunctionHandler.ActivationFunction outputAF { get; private set; }

            public ANNConfig(AITypes aiType, string id) : base(aiType, id) { }

            public ANNConfig(AITypes aiType, string id, int nHiddenNeuronsPerLayer, int nHiddenLayers, int nOutputNeurons,
                ActivationFunctionHandler.ActivationFunction hiddenAF, ActivationFunctionHandler.ActivationFunction outputAF,
                int epochs, double alpha) : base(aiType, id) {
                this.alpha = alpha;
                this.epochs = epochs;
                this.nHiddenNeuronsPerLayer = nHiddenNeuronsPerLayer;
                this.nHiddenLayers = nHiddenLayers;
                this.nOutputNeurons = nOutputNeurons;
                this.hiddenAF = hiddenAF;
                this.outputAF = outputAF;
            }

            public void SetAlpha(double alpha) => this.alpha = alpha;

            public void SetEpochs(int epochs) => this.epochs = epochs;

            public void SetNHiddenNeuronsPerLayer(int nHiddenNeuronsPerLayer) => this.nHiddenNeuronsPerLayer = nHiddenNeuronsPerLayer;

            public void SetNHiddenLayers(int nHiddenLayers) => this.nHiddenLayers = nHiddenLayers;

            public void SetNOuputNeurons(int nOutputNeurons) => this.nOutputNeurons = nOutputNeurons;

            public void SetHiddenActivationFunction(ActivationFunctionHandler.ActivationFunction af) => hiddenAF = af;

            public void SetOutputActivationFunction(ActivationFunctionHandler.ActivationFunction af) => outputAF = af;

            protected override string SerializeANN() {
                string output = "#ANNConfig: " + id + "\n";
                output += "Alpha: " + alpha + "\n";
                output += "Epochs: " + epochs + "\n";
                output += "NumberOfHiddenLayers: " + nHiddenLayers + "\n";
                output += "NumberOfHiddenNeuronsPerLayer: " + nHiddenNeuronsPerLayer + "\n";
                output += "NumberOfOutputNeurons: " + nOutputNeurons + "\n";
                output += "HiddenActivationFunction: " + hiddenAF + "\n";
                output += "OutputActivationFunction: " + outputAF + "\n";
                return output;
            }

            protected override string SerializeCNN() => throw new AccessViolationException("You shouldn't reach this with an ANNConfig!");

            protected override void Deserialize(StreamReader sr, string additionalInfo = "") {
                string input;
                string[] inputParts;

                while ((input = sr.ReadLine()) != "") {
                    inputParts = input.Split(':');
                    switch (inputParts[0]) {
                        case "Alpha":
                            alpha = double.Parse(inputParts[1].Trim());
                            break;
                        case "Epochs":
                            epochs = int.Parse(inputParts[1].Trim());
                            break;
                        case "NumberOfHiddenLayers":
                            nHiddenLayers = int.Parse(inputParts[1].Trim());
                            break;
                        case "NumberOfHiddenNeuronsPerLayer":
                            nHiddenNeuronsPerLayer = int.Parse(inputParts[1].Trim());
                            break;
                        case "NumberOfOutputNeurons":
                            nOutputNeurons = int.Parse(inputParts[1].Trim());
                            break;
                        case "HiddenActivationFunction":
                            hiddenAF.Parse(inputParts[1].Trim());
                            break;
                        case "OutputActivationFunction":
                            outputAF.Parse(inputParts[1].Trim());
                            break;
                        default:
                            return;
                    }
                }
            }
        }
    }
}