using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ActivationFunctionHandler {
    public static double TriggerActivationFunction(ActivationFunctions activationFunction, double value) {
        switch (activationFunction) {
            case ActivationFunctions.ReLU:
                return ReLU(value);
            case ActivationFunctions.Sigmoid:
                return Sigmoid(value);
            case ActivationFunctions.TanH:
                return TanH(value);
        }
        Debug.LogError("The activation function wasn't set properly!");
        return value;
    }

    static double ReLU(double value) { return value > 0 ? value : 0;  }

    static double Sigmoid(double value) {
        double k = System.Math.Exp(value);
        return k / (1.0f + k);
    }

    static double TanH(double value) {
        double k = System.Math.Exp(-2 * value);
        return 2 / (1.0f + k) - 1;
    }

    public static double TriggerDerativeFunction(ActivationFunctions activationFunction, double value) {
        switch (activationFunction) {
            case ActivationFunctions.ReLU:
                return ReLUDerivative(value);
            case ActivationFunctions.Sigmoid:
                return SigmoidDerivative(value);
            case ActivationFunctions.TanH:
                return TanHDerivative(value);
        }
        Debug.LogError("The activation function wasn't set properly!");
        return value;
    }

    static double SigmoidDerivative(double value) { return value * (1 - value); }

    static double ReLUDerivative(double value) { return value > 0 ? value : 0; }

    static double TanHDerivative(double value) { return 1 - value * value; }
}