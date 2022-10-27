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

    enum State
    {
        MoveToDestination,
        Eating,
        Sleeping,
        SitOnToilet,
        DoNothing,
    }

    State currentState = State.MoveToDestination;
    State targetState = State.DoNothing;
    bool stateEnter = false;
    float stateTime = 0;

    enum Anim_State{
        Stand = 0,
        Eating = 1,
    }

    enum Desire{
        Toilet,
        Eat,
        Sleep,
    }

    private void ChangeState(State newState)
    {
        currentState = newState;
        stateEnter = true;
        stateTime = 0;
        Debug.Log(currentState.ToString());
    }

    private void ChangeAnimState(Anim_State state)
    {
        animator.SetInteger("ID", (int)state);
    }

    Dictionary<Desire, float> desireDictionary = new Dictionary<Desire, float>();

    private void Start()
    {
        navmeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;

        foreach(Desire desire in Enum.GetValues(typeof(Desire)))
        {
            desireDictionary.Add(desire, 0f);
        }

        ChangeState(State.MoveToDestination);
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        float speed = navmeshAgent.velocity.magnitude;

        animator.SetFloat("PlayerSpeed",speed);

        if(currentState != State.Eating)
        {
            desireDictionary[Desire.Eat] += Time.deltaTime / 5.0f; //5秒に一回欲求がMaxに
        }

        if(currentState != State.Sleeping)
        {
            desireDictionary[Desire.Sleep] += Time.deltaTime / 10.0f;
        }
        
        if(currentState != State.SitOnToilet)
        {
             desireDictionary[Desire.Toilet] += Time.deltaTime / 7.0f;
        }

        IOrderedEnumerable<KeyValuePair<Desire, float>> sortedDesire = desireDictionary.OrderByDescending(i => i.Value);

        text.text ="";
        foreach(KeyValuePair<Desire, float> sortedDesireElement in sortedDesire)
        {
            text.text += sortedDesireElement.Key.ToString() + ":" + sortedDesireElement.Value + "\n";
        }

        switch (currentState)
        {
            case State.MoveToDestination: {
                if(stateEnter)
                {
                    var topDesireElement = sortedDesire.ElementAt(0);

                    if(topDesireElement.Value >= 1.0f)
                    {
                        switch(topDesireElement.Key)
                        {
                            case Desire.Eat:
                                navmeshAgent.SetDestination(point_Table.position);
                                targetState = State.Eating;
                                break;
                            case Desire.Sleep:
                                navmeshAgent.SetDestination(point_Bed.position);
                                targetState = State.Sleeping;
                                break;
                            case Desire.Toilet:
                                navmeshAgent.SetDestination(point_Toilet.position);
                                targetState = State.SitOnToilet;
                                break;
                        }
                    }
                    else
                    {
                        navmeshAgent.SetDestination(point_Living.position);
                        targetState = State.DoNothing;
                    }
                }

                ChangeAnimState(Anim_State.Stand);

                if(navmeshAgent.remainingDistance <= 0.01f && !navmeshAgent.pathPending)
                {
                    ChangeState(targetState);
                    return;
                }

                return;
            }

            case State.DoNothing: {
                
                if(sortedDesire.ElementAt(0).Value >= 1.0f)
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

                }

                if(stateTime >= 5.0f)
                {
                    desireDictionary[Desire.Sleep] = 0;
                    ChangeState(State.MoveToDestination);
                    return;
                }

                return;
            }

            case State.SitOnToilet: {

                if(stateEnter)
                {

                }

                if(stateTime >= 4.0f)
                {
                    desireDictionary[Desire.Eat] = 0;
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
            stateEnter = false;
        }
        
        
    }
}
