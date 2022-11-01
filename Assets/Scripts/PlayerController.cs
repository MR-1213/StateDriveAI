using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public Transform point_Living;
    public Transform point_Bed;
    public Transform point_Table;
    public Transform point_Toilet;
    public Text text;

    NavMeshAgent navmeshAgent;
    Animator animator;

    enum State //取りうる全ての行動
    {
        MoveToDestination,
        Eating,
        Sleeping,
        SitOnToilet,
        DoNothing,
    }

    State currentState = State.MoveToDestination; //初めの行動は目的地に移動するステート
    State targetState = State.DoNothing;
    bool stateEnter = false; //新たなステートに入った時に最初の1フレームだけtrueになる
    float stateTime = 0;//ステート実行中の経過時間

    enum Anim_State{ //アニメーションを遷移させるためのステート
        Stand = 0,
        Eating = 1,
        Toilet = 2,
        Sleep = 3,
    }

    enum Desire{ //欲求を管理するステート(1.0になると対応する行動を実行)
        Toilet,
        Eat,
        Sleep,
    }

    private void ChangeState(State newState) //新たなステートに遷移
    {
        currentState = newState;
        stateEnter = true;
        stateTime = 0;
        Debug.Log(currentState.ToString());
    }

    private void ChangeAnimState(Anim_State state) //新たなアニメーションに遷移
    {
        animator.SetInteger("ID", (int)state);
    }

    /* DesireステートをKey,float型の値をValueとしたディクショナリーを作成 */
    Dictionary<Desire, float> desireDictionary = new Dictionary<Desire, float>(); //この時点ではまだ空のディクショナリー

    private void Start()
    {
        navmeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;

        /* in以降でDesireステートの逐次取得。ディクショナリーのValue(float)を変数disireに入れる */
        foreach(Desire desire in Enum.GetValues(typeof(Desire)))
        {
            desireDictionary.Add(desire, 0f); //各々の欲求に初期値として0を代入 & ディクショナリーに追加
        }

        ChangeState(State.MoveToDestination); //一度だけ実行しておく
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        float speed = navmeshAgent.velocity.magnitude;

        animator.SetFloat("PlayerSpeed",speed);

        if(currentState != State.Eating)
        {
            desireDictionary[Desire.Eat] += Time.deltaTime / 30.0f; //30秒に一回欲求がMaxに
        }

        if(currentState != State.Sleeping)
        {
            desireDictionary[Desire.Sleep] += Time.deltaTime / 60.0f;
        }
        
        if(currentState != State.SitOnToilet)
        {
             desireDictionary[Desire.Toilet] += Time.deltaTime / 40.0f;
        }

        IOrderedEnumerable<KeyValuePair<Desire, float>> sortedDesire = desireDictionary.OrderByDescending(i => i.Value); //最も欲求値の高いものが先頭に来るようにソート

        text.text ="";
        foreach(KeyValuePair<Desire, float> sortedDesireElement in sortedDesire)
        {
            text.text += sortedDesireElement.Key.ToString() + ":" + sortedDesireElement.Value + "\n";
        }

        switch (currentState)
        {
            case State.MoveToDestination: { //目的地に向かうステート
                if(stateEnter) //1フレーム目で目的地、スピード、次の行動を決定
                {
                    var topDesireElement = sortedDesire.ElementAt(0); //最も欲求の大きいものを取得

                    if(topDesireElement.Value >= 1.0f)
                    {
                        switch(topDesireElement.Key)
                        {
                            case Desire.Eat:
                                navmeshAgent.SetDestination(point_Table.position);
                                navmeshAgent.speed = 2.0f;
                                targetState = State.Eating;
                                break;
                            case Desire.Sleep:
                                navmeshAgent.SetDestination(point_Bed.position);
                                navmeshAgent.speed = 1.7f;
                                targetState = State.Sleeping;
                                break;
                            case Desire.Toilet:
                                navmeshAgent.SetDestination(point_Toilet.position);
                                navmeshAgent.speed = 3.0f;
                                targetState = State.SitOnToilet;
                                break;
                        }
                    }
                    else
                    {
                        navmeshAgent.SetDestination(point_Living.position);
                        navmeshAgent.speed = 1.7f;
                        targetState = State.DoNothing;
                    }
                }

                /* ここは毎フレーム実行する */
                ChangeAnimState(Anim_State.Stand);

                if(navmeshAgent.remainingDistance <= 0.01f && !navmeshAgent.pathPending) //目的地に到着したら
                {
                    ChangeState(targetState); //targetStateの行動を次の行動とする
                    return;
                }

                return;
            }

            case State.DoNothing: {
                
                if(sortedDesire.ElementAt(0).Value >= 1.0f) //いずれかの欲求値が1.0になるまで何もせず待機(Stopアニメーション実行)
                {
                    ChangeState(State.MoveToDestination);
                    return;
                }

                return;
            }

            case State.Eating: {

                if(stateEnter)
                {
                    navmeshAgent.enabled = false;
                    ChangeAnimState(Anim_State.Eating);
                    transform.position = point_Table.position;
                    transform.rotation = point_Table.rotation;
                }

                if(stateTime >= 3.0f)
                {
                    navmeshAgent.enabled = true;
                    desireDictionary[Desire.Eat] = 0;
                    ChangeState(State.MoveToDestination);
                    return;
                }

                return;
            }

            case State.Sleeping: {

                if(stateEnter)
                {
                    navmeshAgent.enabled = false;
                    ChangeAnimState(Anim_State.Sleep);
                    transform.position = point_Bed.position;
                    transform.rotation = point_Bed.rotation;
                }

                if(stateTime >= 5.0f)
                {
                    navmeshAgent.enabled = true;
                    desireDictionary[Desire.Sleep] = 0;
                    ChangeState(State.MoveToDestination);
                    return;
                }

                return;
            }

            case State.SitOnToilet: {

                if(stateEnter)
                {
                    navmeshAgent.enabled = false;
                    ChangeAnimState(Anim_State.Toilet);
                    transform.position = point_Toilet.position;
                    transform.rotation = point_Toilet.rotation;
                }

                if(stateTime >= 4.0f)
                {
                    navmeshAgent.enabled = true;
                    desireDictionary[Desire.Toilet] = 0;
                    ChangeState(State.MoveToDestination);
                    return;
                }

                return;
            }
        }
    }

    private void LateUpdate()
    {
        if(stateTime != 0)
        {
            stateEnter = false; //最初の1フレーム後はfalseに
        }
        
        
    }
}
