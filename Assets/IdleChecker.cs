using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleChecker : StateMachineBehaviour
{
    public Gem m_GemScript;
    public bool m_Idle = true;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_Idle = true;
    }
}
