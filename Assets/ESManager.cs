using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

// 进化策略过程管理
public class ESManager : MonoBehaviour
{
    public string savePath = "data.json";
    public Creature prefab;
    public int generation = 0;          // 代数
    public int instanceCount = 10;           // 同时跑10个
    public int[] counts = { 3, 15, 15, 2 };// 各层的节点的数量

    public float roundTime = 7f;

    public double learnRate = 0.1;
    public double mutationRate = 0.3;      // 变异比例
    public double mutationStrength = 0.05;  // 变异程度

    public List<float> avgGrades = new List<float>();

    List<Creature> men;
    public List<DenseMatrix> weights;   // 权重

    public void SaveData(string path)
    {
        var fullPath = Application.dataPath + "/" + path;
        var data = new ESManagerData(this);
        var json = JsonUtility.ToJson(data,true);
        StreamWriter w = new StreamWriter(fullPath);
        w.Write(json);
        w.Close();
    }

    public void LoadData(string path)
    {
        var fullPath = Application.dataPath + "/" + path;
        StreamReader r = new StreamReader(fullPath);
        var json = r.ReadToEnd();
        r.Close();
        var data = JsonUtility.FromJson<ESManagerData>(json);
        data.LoadTo(this);
        StopAllCoroutines();
        generation -= 1;
        NextGeneration(true);
    }
        
    private void Start()
    {
        // ======================== 生成多个独立个体 ======================== 
        men = new List<Creature>();
        for (int i = 0; i < instanceCount; i++)
        {
            var go  = Instantiate<GameObject>(prefab.gameObject);
            go.name = "那个男人 " + i;
            var man = go.GetComponent<Creature>();
            go.transform.position += Vector3.forward * Random.value;
            men.Add(man);
        }
        prefab.gameObject.SetActive(false);

        weights = new List<DenseMatrix>();
        List<DenseMatrix> layers = new List<DenseMatrix>();

        // ======================== 初始化神经网络 ======================== 
        // 权重数量
        int weightsCount = 0;
        for (int i = 0; i < counts.Length - 1; i++)
        {
            weightsCount += (counts[i] + 1) * counts[i + 1];
        }
        UnityEngine.Debug.Log("权重数量:" + weightsCount);
        
        // 生成层节点的向量
        for (int i = 0; i < counts.Length - 1; i++)
        {
            var mat = new DenseMatrix(1, 1 + counts[i]);
            layers.Add(mat);
        }
        var outputLayer = new DenseMatrix(1, counts[counts.Length - 1]);
        layers.Add(outputLayer);
        // 生成全连接的权重矩阵
        for (int i = 0; i < counts.Length - 1; i++)
        {
            // 正态分布随机初始化
            DenseMatrix h = new DenseMatrix(counts[i] + 1, counts[i + 1]);
            weights.Add(h);
        }
        // 初始化各个network...
        for (int i = 0; i < men.Count; i++)
        {
            men[i].nn = new NeuralNetwork(layers, weights);
        }

        // ======================== 第一代 ======================== 
        NextGeneration(true);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < men.Count; i++)
        {
            if (men[i].isAlive)
            {
                men[i].ForwardUpdate();
            }
        }
    }

    // 控制模拟进程, 隔一段时间, 停止模拟
    IEnumerator SimulateForSeconds(float stepSecs)
    {
        List<Vector2> poses = new List<Vector2>() { new Vector2(0,0) };
        System.Func<bool> isAllDead = () => 
        {
            foreach (var m in men)
            {
                if (m.isAlive)
                    return false;
            }
            return true;
        };

        float startTime;

        for (int i = 0; i < poses.Count; i++)
        {
            for (int j = 0; j < men.Count; j++)
            {
                men[j].Reset(poses[i] * new Vector2(1, 1), false);
            }

            startTime = Time.time;
            while (Time.time - startTime < stepSecs && !isAllDead())
                yield return null;
        }

        NextGeneration(false);
    }

    void NextGeneration(bool isInit)
    {
        if (!isInit)
        {
            CountGenerationGrade();
            UpdateGenerationWeights();
            // 选择一组权值并用正态分布随机
            for (int i = 0; i < weights.Count; i++)
            {
                // 随机取一部分节点作变化
                int row = weights[i].RowCount;
                int column = weights[i].ColumnCount;
                var maskMat = new DenseMatrix(row, column);
                maskMat = (DenseMatrix)maskMat.Map(x => (Random.value < mutationRate ? 1d : 0d));

                // 这里有问题啊喂!
                foreach (var m in men)
                {
                    var ws = m.nn.weights;
                    ws[i] = weights[i] + (DenseMatrix)maskMat.PointwiseMultiply(DenseMatrix.CreateRandom(row, column, new MathNet.Numerics.Distributions.Normal(0, mutationStrength)));
                    //ws[i] += (DenseMatrix)maskMat.PointwiseMultiply(DenseMatrix.CreateRandom(row, column, new MathNet.Numerics.Distributions.Normal(0, mutationStrength)));
                }
            }
        }
        Debug.Log(weights[0]);
        // 下一代
        generation++;
        
        for (int i = 0; i < men.Count; i++)
        {
            men[i].Reset(Vector2.zero, true);
            //men[i].transform.position = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
        }

        StopAllCoroutines();
        StartCoroutine(SimulateForSeconds(roundTime));
    }
    // 计算平均分
    void CountGenerationGrade()
    {  
        // 输出平均分数
        float gradeSum = 0f;
        foreach (var m in men)
        {
            gradeSum += m.grade;
        }
        gradeSum /= instanceCount;
        avgGrades.Add(gradeSum);
    }
    // 根据得分更新权值
    void UpdateGenerationWeights()
    {
        List<float> grades = new List<float>();
        foreach (var m in men)
        {
            grades.Add(m.grade);
        }
        grades.Sort();

        int minIndex = (int)(grades.Count * 0.6f);
        float minG = grades[minIndex];
        float sumG = 0f;
        foreach (var m in men)
        {
            if(m.grade >=minG)
                sumG += m.grade;
        }
        
        for (int i = 0; i < weights.Count; i++)
        {
            DenseMatrix w = new DenseMatrix(weights[i].RowCount, weights[i].ColumnCount);
            foreach (var m in men)
            {
                if(m.grade>=minG)
                    w += m.nn.weights[i] * (m.grade/sumG);
            }
            weights[i] += (w - weights[i]) * learnRate;
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(220, 30, 200, 400));
        GUILayout.Label("单次训练时间 "+ roundTime );
        roundTime = GUILayout.HorizontalSlider(roundTime, 3f, 100f);
        GUILayout.Label("学习率 "+(float)learnRate );
        learnRate = GUILayout.HorizontalSlider((float)learnRate, 0f, 1f);
        GUILayout.Label("变异率 "+ (float)mutationRate);
        mutationRate = GUILayout.HorizontalSlider((float)mutationRate, 0f, 1f);
        GUILayout.Label("变异强度 "+ (float)mutationStrength);
        mutationStrength = GUILayout.HorizontalSlider((float)mutationStrength, 0f, 1f);
        
        GUILayout.Label("模拟速度" + Time.timeScale);
        Time.timeScale =  GUILayout.HorizontalSlider((float)Time.timeScale, 1f, 4f);
        
        GUILayout.Space(20);
        GUILayout.Label("储存路径");
        savePath = GUILayout.TextField(savePath);

        try
        {
            if (GUILayout.Button("保存数据"))
                SaveData(savePath);
            if (GUILayout.Button("读取数据"))
                LoadData(savePath);
        }
        catch
        {}

        GUILayout.Label("重新开始训练");
        if(GUILayout.Button("RESTART"))
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(10, 10, 150, 1060));
        string message = "第 " + generation + " 代\n";
        foreach (var m in men)
            if (m.isAlive)
                message +=  "存活 \t" + m.grade + "\n";
        
        GUILayout.TextArea(message);
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 160, 10, 150, 1060));
        string history = "最近的60代平均得分 : \n";
        float minIndex = Mathf.Clamp(avgGrades.Count - 60, 0, int.MaxValue);
        for (int i = avgGrades.Count-1; i>=minIndex ;i--)
        {
            history += (i+1).ToString() + " : \t" + avgGrades[i] + "\n";
        }
        GUILayout.TextArea(history);
        GUILayout.EndArea();
    }
}

[System.Serializable]
public class WeightsData
{
    public List<double> data;
    public int[] counts;

    public WeightsData(List<DenseMatrix> ws, int[] counts)
    {
        data = new List<double>();
        this.counts = counts;
        foreach (var w in ws)
        {
            double[] rowW = w.ToRowWiseArray();
            data.AddRange(rowW);
        }
    }

    public List<DenseMatrix> ToWeights()
    {
        var ws = new List<DenseMatrix>();
        int startIndex = 0;
        for (int i = 0; i < counts.Length - 1; i++)
        {
            int c1 = counts[i] + 1;
            int c2 = counts[i + 1];
            double[] w = new double[c1 * c2];
            data.CopyTo(startIndex, w, 0, c1 * c2);
            startIndex += c1 * c2;

            DenseMatrix mat = new DenseMatrix(c1, c2, w);
            ws.Add(mat);
        }
        return ws;
    }
}

[System.Serializable]
public class ESManagerData
{
    public int generation;          // 代数
    public double learnRate;
    public double mutationRate;      // 变异比例
    public double mutationStrength;  // 变异程度
    public float roundTime;

    public List<float> avgGrades;
    public WeightsData weightsData;

    public ESManagerData(ESManager m)
    {
        this.generation = m.generation;
        this.learnRate = m.learnRate;
        this.mutationRate = m.mutationRate;
        this.mutationStrength = m.mutationStrength;
        this.avgGrades = m.avgGrades;
        this.roundTime = m.roundTime;
        this.weightsData = new WeightsData(m.weights, m.counts);
    }

    public void LoadTo(ESManager m)
    {
        m.roundTime = this.roundTime;
        m.generation = this.generation;
        m.learnRate = this.learnRate;
        m.mutationRate = this.mutationRate;
        m.mutationStrength = this.mutationStrength;
        m.avgGrades = this.avgGrades;
        m.weights = this.weightsData.ToWeights();
    }
}