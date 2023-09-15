using System;

namespace Lokad.BrainScript.Example
{
    class Program
    {
        static void Main()
        {
            var script = @"
BrainScriptNetworkBuilder = {   # (we are inside the train section of the CNTK config file)

    SDim = 28*28 # feature dimension
    HDim = 256   # hidden dimension
    LDim = 10    # number of classes

    # define the model function. We choose to name it 'model()'.
    model (features) = {
        # model parameters
        W0 = ParameterTensor {(HDim:SDim)} ; b0 = ParameterTensor {HDim}
        W1 = ParameterTensor {(LDim:HDim)} ; b1 = ParameterTensor {LDim}

        # model formula
        r = RectifiedLinear (W0 * features + b0) # hidden layer
        z = W1 * r + b1                          # unnormalized softmax
    }.z

    # define inputs
    features = Input {SDim}
    labels   = Input {LDim} 

    # apply model to features
    z = model (features)

    # define criteria and output(s)
    ce   = CrossEntropyWithSoftmax (labels, z)  # criterion (loss)
    errs = ErrorPrediction         (labels, z)  # additional metric
    P    = Softmax (z)     # actual model usage uses this

    # connect to the system. These five variables must be named exactly like this.
    featureNodes    = (features)
    inputNodes      = (labels)
    criterionNodes  = (ce)
    evaluationNodes = (errs)
    outputNodes     = (P)
}

BrainScriptNetworkBuilder = {
    # STEP 1: load 1-hidden-layer model
    inModel = BS.Network.Load (""model.1.dnn"")
    # get its h1 variable --and also recover its dimension
    h1 = inModel.h1
    H = h1.dim
    # also recover the number of output classes
    M = inModel.z.dim

    # STEP 2: create the rest of the extended network as usual
            W2 = Parameter(H, H); b2 = Parameter(H)
    Wout = Parameter(M, H); bout = Parameter(M)
    h2 = Sigmoid(W2 * h1 + b2)
    z = Wout * h2 + bout
    ce = CrossEntropyWithSoftmax(labels, z, tag ='criterion')
}";

            Console.WriteLine(Parser.Parse(script).Print(false));
        }
    }
}
