using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    public float grade = 0f; // 分数
    public bool isAlive = false;// 是否还活着
    public int deathCount = 0;

    public NeuralNetwork nn;

    public float[] output;

    public Rigidbody2D rb2d;
    public Transform headTrans;// 头部
    public Transform leftProp;
    public Transform rightProp;
    public float flyMaxForce = 100f;
    public float flyForceRate = 10f;
    public float maxAngularVelocity = 100f;
    public float windForce = 100f;

    public void Reset(Vector2 pos, bool resetGrade = true)
    {
        headTrans.position = pos;
        headTrans.rotation = Quaternion.Euler(0,0,0);

        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0;

        if (resetGrade)
        {
            grade = 0f;
        }
        isAlive = true;
        gameObject.SetActive(true);
    }

    public void ForwardUpdate()
    {
        output = nn.Forward(GetInput());
        ActByOutput(output);
    }

    /// <summary>
    /// 将环境及自身的状态作为输入, 注意取值范围需要映射到(-1~1)左右
    /// </summary>
    float[] GetInput()
    {
        // head, top, btm
        float[] input = new float[6];
        input[0] = headTrans.rotation.z/180f;
        input[1] = headTrans.position.x/5f;
        input[2] = headTrans.position.y/5f;

        input[3] = rb2d.velocity.x/4f;
        input[4] = rb2d.velocity.y/4f;
        input[5] = rb2d.angularVelocity/maxAngularVelocity/2f;

        return input;
    }

    void ActByOutput(float[] output)
    {
        Debug.Assert(isAlive);

                            
        float lForce = Mathf.Clamp(output[0] * flyForceRate, 0, flyMaxForce) * Time.fixedDeltaTime;
        float rForce = Mathf.Clamp(output[1] * flyForceRate, 0, flyMaxForce) * Time.fixedDeltaTime;
 
        rb2d.AddForceAtPosition(leftProp.up * lForce, leftProp.position);
        rb2d.AddForceAtPosition(rightProp.up * rForce, rightProp.position);
        // 旋转
        leftProp.Rotate(0, lForce*20f, 0);
        rightProp.Rotate(0, rForce*20f, 0);

        // 随机吹风
        rb2d.AddForce(windForce * Random.insideUnitCircle * Time.fixedDeltaTime);
         
        rb2d.angularVelocity = Mathf.Clamp(rb2d.angularVelocity, -maxAngularVelocity, maxAngularVelocity);
        //var v = rb2d.velocity;
        //v.x = Mathf.Clamp(v.x, )

        grade += Time.fixedDeltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isAlive = false;
        grade -= 5f;
        gameObject.SetActive(false);
        return;
    }
}
