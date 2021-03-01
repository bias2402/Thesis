using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace MachineLearning {
#if UNITY_EDITOR
    [Serializable]
#endif
    public class CNN {
#if UNITY_EDITOR
        [SerializeField] public List<double> outputs = new List<double>();
        [SerializeField] private List<CNNFilter> CNNFilters = new List<CNNFilter>();
        [SerializeReference] public List<float[,]> generatedMaps = new List<float[,]>();
        [SerializeReference] public List<float[,]> pooledMaps = new List<float[,]>();
        [SerializeField] public ANN ann = null;
#else
        private List<CNNFilter> CNNFilters = new List<CNNFilter>();
        public List<float[,]> generatedMaps { get; private set; } = new List<float[,]>();
        public List<float[,]> pooledMaps { get; private set; } = new List<float[,]>();
        public ANN ann { get; private set; } = null;
        public List<double> outputs = new List<double>();
#endif

        /// <summary>
        /// Clear the CNN completely. Set the parameters to only clear some of the data.
        /// </summary>
        /// <param name="clearOutputs"></param>
        /// <param name="clearGeneratedMaps"></param>
        /// <param name="clearFilters"></param>
        /// <param name="clearANN"></param>
        public void Clear(bool clearOutputs = true, bool clearGeneratedMaps = true, bool clearFilters = true, bool clearANN = true) {
            if (clearOutputs) outputs.Clear();
            if (clearGeneratedMaps) generatedMaps.Clear();
            if (clearFilters) CNNFilters.Clear();
            if (clearANN) ann = null;
        }

        /// <summary>
        /// Create a new <paramref name="filter"/> with dimensions equal to the given array's length. Filter must have the same length in both dimensions!
        /// </summary>
        /// <param name="filter"></param>
        public void AddNewFilter(float[,] filter, string filterName = "") {
            if (filter.GetLength(0) != filter.GetLength(1)) throw new ArgumentException("Filter dimensions aren't equal size!");
            CNNFilters.Add(new CNNFilter(filter, filterName));
        }

        /// <summary>
        /// Run the CNN on the given 2D float array with the default settings and return the network's decision.
        /// Default settings: padding = 0, convolution stride = 1, pooling kernel = 2, pooling stride = 2, outputs = 3
        /// Flow: Convolution, Pooling, Fully Connected (Decision Making)
        /// </summary>
        /// <param name="input"></param>
        public int Run(float[,] input) {
            Convolution(input);
            foreach (float[,] map in generatedMaps) Pooling(map);
            outputs = FullyConnected(3, pooledMaps);
            return (int)outputs.Max();
        }

        public List<double> Train(List<double> desiredOutputs, ANN ann = null) {
            //Generate a list of inputs
            List<double> inputs = GenerateInputs(generatedMaps);

            //If an ANN hasn't been created nor is one given, create a new ANN and save it for later use
            if (ann == null && this.ann == null) this.ann = new ANN(inputs.Count, 0, 0, desiredOutputs.Count, ANN.ActivationFunction.ReLU, ANN.ActivationFunction.ReLU);
            return this.ann.Train(inputs, desiredOutputs);
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
            float dimx, dimy, value;
            Coord newMapCoord, mapCoord;
            float[,] newMap;

            foreach (CNNFilter filter in CNNFilters) {
                //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
                //It is described and reference in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
                dimx = (1 + (map.GetLength(0) - filter.dimensions) / stride);
                dimy = (1 + (map.GetLength(1) - filter.dimensions) / stride);

                //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
                //new map could have the wrong dimensions, which would break the convolution!
                if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                    throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or filter(s) so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

                newMap = new float[(int)dimx, (int)dimy];                                                       //The map is created using the dimensions calculated above
                newMapCoord = new Coord(0, 0);                                                                  //Coordinates on the new map, where a the calculated value will be placed
                mapCoord = new Coord(0, 0);                                                                     //Coordinates on the old map, from which the filter is applied

                while (true) {
                    value = 0;                                                                                      //Value that is calculated during convolution and applied to the new map at newMapCoord
                    //Apply filter from current mapCoord
                    for (int y = mapCoord.y, fy = 0; y < mapCoord.y + filter.dimensions; y++, fy++) {
                        for (int x = mapCoord.x, fx = 0; x < mapCoord.x + filter.dimensions; x++, fx++) {
                            value += map[x, y] * filter.filter[fx, fy];
                        }
                    }
                    //Increment mapCoord
                    mapCoord.x += stride;
                    if (mapCoord.x > map.GetLength(0) - filter.dimensions) {
                        mapCoord.x = 0;
                        mapCoord.y += stride;
                    }
                    //Apply the value to newMap and increment newMapCoord
                    newMap[newMapCoord.x, newMapCoord.y] = value;
                    newMapCoord.x++;
                    if (newMapCoord.x == newMap.GetLength(0)) {
                        newMapCoord.x = 0;
                        newMapCoord.y++;
                        if (newMapCoord.y == newMap.GetLength(1)) {
                            break;
                        }
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
            //The dimensions for the output map is calculated using this formula: OutputDimension = 1 + (inputDimension - filterDimension) / Stride   <=>   0 = 1 + (N - F) / S
            //The formula is described and referenced in the article: Albawi, Al-Zawi, & Mohammed. 2017. Understanding of a Convolutional Neural Network
            float dimx = (1 + (map.GetLength(0) - kernelDimension) / stride);
            float dimy = (1 + (map.GetLength(1) - kernelDimension) / stride);

            //A check is performed to ensure the new dimensions are integers since the new map can't be made using floating points! If floating points were used and rounded, the
            //new map could have the wrong dimensions, which would break the pooling!
            if (Math.Abs(Math.Round(dimx) - dimx) > 0.000001f || Math.Abs(Math.Round(dimy) - dimy) > 0.000001)
                throw new ArgumentException("Output calculation didn't result in an integer! Adjust your stride or kernel dimension so: outputMapSize = 1 + (inputMapSize - filterSize) / stride = integer");

            float[,] newMap = new float[(int)dimx, (int)dimy];                                              //The map will shrink during pooling, so the new map is smaller in both dimensions
            Coord newMapCoord = new Coord(0, 0);                                                            //Coordinates on the new map, where a the calculated value will be placed
            Coord mapCoord = new Coord(0, 0);                                                               //Coordinates on the old map, from which the filter is applied
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
                mapCoord.x += stride;
                if (mapCoord.x > map.GetLength(0) - kernelDimension) {
                    mapCoord.x = 0;
                    mapCoord.y += stride;
                }
                //Apply the value to newMap and increment newMapCoord
                newMap[newMapCoord.x, newMapCoord.y] = kernelValues.Max();
                newMapCoord.x++;
                if (newMapCoord.x == newMap.GetLength(0)) {
                    newMapCoord.x = 0;
                    newMapCoord.y++;
                    if (newMapCoord.y == newMap.GetLength(1)) {
                        break;
                    }
                }
            }
            pooledMaps.Add(newMap);

            return newMap;
        }

        /// <summary>
        /// Run the generated maps through an ANN (if an ANN doesn't exist nor is given, it creates a new using the <paramref name="nOutputs"/> paramter)
        /// and return the calculated outputs from the ANN. The <paramref name="ann"/> parameter allows for custom ANN's to be used.
        /// If an ANN is given, it will be used and it will override the currently saved ANN (if any). 
        /// </summary>
        /// <param name="nOutputs"></param>
        /// <param name="ann"></param>
        /// <returns></returns>
        public List<double> FullyConnected(int nOutputs, List<float[,]> maps, ANN ann = null) {
            //Generate a list of inputs
            List<double> inputs = GenerateInputs(maps);

            //If an ANN hasn't been created nor is one given, create a new ANN and save it for later use
            if (ann == null && this.ann == null) this.ann = new ANN(inputs.Count, 0, 0, nOutputs, ANN.ActivationFunction.ReLU, ANN.ActivationFunction.ReLU);
            return this.ann.Run(inputs);
        }

        /// <summary>
        /// Input a list of float[,] <paramref name="maps"/> and get a list of double values
        /// </summary>
        /// <param name="maps"></param>
        /// <returns></returns>
        List<double> GenerateInputs(List<float[,]> maps) {
            List<double> inputs = new List<double>();
            for (int i = 0; i < generatedMaps.Count; i++) {
                for (int x = 0; x < generatedMaps[i].GetLength(0); x++) {
                    for (int y = 0; y < generatedMaps[i].GetLength(1); y++) {
                        inputs.Add(generatedMaps[i][x, y]);
                    }
                }
            }
            return inputs;
        }

#if UNITY_EDITOR
        [Serializable]
#endif
        class CNNFilter {
            [SerializeReference] public string filterName = "";
            [SerializeReference] public float dimensions = 3;
            [SerializeReference] public float[,] filter;

            public CNNFilter(float[,] filter, string filterName) {
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
                    this.filterName = s;
                }
            }
        }

        struct Coord {
            public int x;
            public int y;

            public Coord(int x, int y) {
                this.x = x;
                this.y = y;
            }
        }
    }

#if UNITY_EDITOR
    [Serializable]
#endif
    public class ANN {
#if UNITY_EDITOR
        [SerializeField] public enum ActivationFunction { ReLU, Sigmoid, TanH }
        [SerializeReference] private int epochs = 1000;
        [SerializeReference] private double alpha = 0.05;
        [SerializeField] private List<Layer> layers = new List<Layer>();
        [SerializeReference] private static System.Random random = new System.Random();
#else
        public enum ActivationFunction { ReLU, Sigmoid, TanH }
        private int epochs = 1000;
        private double alpha = 0.05;
        private List<Layer> layers = new List<Layer>();
        private static System.Random random = new System.Random();
#endif

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
        public ANN(int nInputsN, int nHiddenL, int nhiddenN, int nOutputN, ActivationFunction hiddenLAF, ActivationFunction outputLAF, int epochs = 1000, double alpha = 0.05) {
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
        /// Run the network with the given inputs.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public List<double> Run(List<double> inputs) {
            PassInputs(inputs);
            CalculateOutput();

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
            for (int i = 0; i < epochs; i++) {
                PassInputs(inputs);
                CalculateOutput();
                Backpropagation(desiredOutputs);
            }

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
            for (int i = 0; i < inputs.Count; i++) {
                for (int j = 0; j < epochs; j++) {
                    PassInputs(inputs[i]);
                    CalculateOutput();
                    Backpropagation(desiredOutputs[i]);
                }
            }

            return GetOutputs();
        }

        public List<double> GetOutputs() {
            List<double> outputs = new List<double>();
            foreach (Neuron n in layers[layers.Count - 1].neurons) outputs.Add(n.outputValue);
            return outputs;
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

#if UNITY_EDITOR
        [Serializable]
#endif
        class Layer {
#if UNITY_EDITOR
            [SerializeField] public List<Neuron> neurons = new List<Neuron>();
#else
            public List<Neuron> neurons = new List<Neuron>();
#endif

            /// <summary>
            /// Create a new layer with <paramref name="numberOfNeuronsForLayer"/> number for neurons.
            /// If <paramref name="prevLayer"/> is null, the layer will be made as an input layer.
            /// If <paramref name="activationFunction"/> isn't set, the layer will default to ReLU.
            /// </summary>
            /// <param name="numberOfNeuronsForLayer"></param>
            /// <param name="prevLayer"></param>
            /// <param name="activationFunction"></param>
            public Layer(int numberOfNeuronsForLayer, Layer prevLayer = null, ActivationFunction activationFunction = ActivationFunction.ReLU) {
                for (int i = 0; i < numberOfNeuronsForLayer; i++) {
                    if (prevLayer != null) 
                        neurons.Add(new Neuron(prevLayer.neurons.Count));
                    else neurons.Add(new Neuron());
                }
                if (prevLayer != null) foreach (Neuron n in neurons) n.activationFunction = activationFunction;
            }

            /// <summary>
            /// Set the activation function in each neuron of this layer to <paramref name="activationFunction"/>.
            /// </summary>
            /// <param name="activationFunction"></param>
            public void SetActivationFunctionForLayer(ActivationFunction activationFunction) { foreach (Neuron n in neurons) n.activationFunction = activationFunction; }
        }

#if UNITY_EDITOR
        [Serializable]
#endif
        class Neuron {
#if UNITY_EDITOR
            [SerializeReference] public ActivationFunction activationFunction = ActivationFunction.ReLU;
            [SerializeReference] public bool isInputNeuron = false;
            [SerializeReference] public double inputValue = 0;
            [SerializeReference] public double bias = 0;
            [SerializeField] public List<double> weights = new List<double>();
            [SerializeField] public List<double> inputs = new List<double>();
            [SerializeReference] public double outputValue = 0;
            [SerializeReference] public double errorGradient = 0;
#else
            public ActivationFunction activationFunction = ActivationFunction.ReLU;
            public bool isInputNeuron = false;
            public double inputValue = 0;
            public double bias = 0;
            public List<double> weights = new List<double>();
            public List<double> inputs = new List<double>();
            public double outputValue = 0;
            public double errorGradient = 0;
#endif

            /// <summary>
            /// Create a new input neuron.
            /// </summary>
            public Neuron() => isInputNeuron = true;

            /// <summary>
            /// Create a new hidden or output neuron. <paramref name="nInputsToNeuron"/> defines the number of weights generated for neuron.
            /// </summary>
            /// <param name="nInputsToNeuron"></param>
            public Neuron(int nInputsToNeuron) {
                if (nInputsToNeuron <= 0) return;

                bias = random.NextDouble() * (1 - -1) + -1;
                for (int i = 0; i < nInputsToNeuron; i++) {
                    weights.Add(random.NextDouble() * (1 - -1) + -1);
                }
            }
        }

        static class ActivationFunctionHandler {
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

            static double ReLU(double value) { return value > 0 ? value : 0; }

            static double Sigmoid(double value) {
                double k = Math.Exp(value);
                return k / (1.0f + k);
            }

            static double TanH(double value) {
                double k = Math.Exp(-2 * value);
                return 2 / (1.0f + k) - 1;
            }

            public static double TriggerDerativeFunction(ActivationFunction activationFunction, double value) {
                switch (activationFunction) {
                    case ActivationFunction.ReLU:
                        return ReLUDerivative(value);
                    case ActivationFunction.Sigmoid:
                        return SigmoidDerivative(value);
                    case ActivationFunction.TanH:
                        return TanHDerivative(value);
                    default:
                        throw new System.NullReferenceException("The activation function wasn't set properly!");
                }
            }

            static double SigmoidDerivative(double value) { return value * (1 - value); }

            static double ReLUDerivative(double value) { return value > 0 ? value : 0; }

            static double TanHDerivative(double value) { return 1 - value * value; }
        }
    }
}