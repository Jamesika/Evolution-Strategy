using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class NeuralNetwork
{
    public List<DenseMatrix> layers;
    public List<DenseMatrix> weights;

    public NeuralNetwork(List<DenseMatrix> layers, List<DenseMatrix> weights)
    {
        this.layers = new List<DenseMatrix>();
        this.weights = new List<DenseMatrix>();

        for (int i = 0; i < layers.Count; i++)
        {
            this.layers.Add(DenseMatrix.OfMatrix(layers[i]));
        }
        for (int i = 0; i < weights.Count; i++)
        {
            this.weights.Add(DenseMatrix.OfMatrix(weights[i]));
        }
        // 初始化时, 权重随机
        for (int i = 0; i < weights.Count; i++)
        {
            weights[i] = DenseMatrix.CreateRandom(weights[i].RowCount, weights[i].ColumnCount, new MathNet.Numerics.Distributions.Normal(0, 0.5));
        }
    }

    /// <summary>
    /// 输入数据, 正向传播(需要非线性的激活函数!!), 输出数据
    /// </summary>
    public float[] Forward(float[] inputs)
    {
        double[] dinputs = new double[inputs.Length + 1];
        dinputs[0] = 1;
        for (int i = 0; i < inputs.Length; i++)
            dinputs[i + 1] = inputs[i];
        layers[0].SetRow(0, dinputs);

        // 正向传播
        for (int i = 0; i < weights.Count - 1; i++)
        {
            var m = (DenseMatrix)DenseMatrix.CreateIdentity(1).Append(layers[i] * weights[i]);
            // 使用 ReLu 激活函数
            m = (DenseMatrix)m.Map(x => System.Math.Max(0, x));
            layers[i + 1] = m;
        }
        // 最后一层, output
        layers[weights.Count] = layers[weights.Count - 1] * weights[weights.Count - 1];

        double[] outputs = layers[weights.Count].ToRowWiseArray();
        return System.Array.ConvertAll(outputs, d => (float)d);
    }
}
