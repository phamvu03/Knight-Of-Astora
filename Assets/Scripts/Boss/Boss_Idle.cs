using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Idle : StateMachineBehaviour
{
    Rigidbody2D rb;
    

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        rb = animator.GetComponentInParent<Rigidbody2D>();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        rb.linearVelocity = Vector2.zero;
        BossScript.instance.Flip();
        JumpToPlayer(animator);

        if (BossScript.instance.attackCountDown <= 0)
        {
            BossScript.instance.AttackHandler();
            //BossScript.instance.attackCountDown = BossScript.instance.attackTimer;
            BossScript.instance.attackCountDown = Random.Range(BossScript.instance.attackTimer - 1,
                BossScript.instance.attackTimer + 1);
        }
        Boss_Fall.Instance.Grounded(animator);
    }
    
    void JumpToPlayer(Animator animator)
    {
        float _distance = Mathf.Abs(PlayerController.Instance.transform.position.x - rb.position.x);   
        if (_distance > BossScript.instance.attackRange)
        {
            animator.SetBool("Jump", true);
        }
        return;
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        BossScript.instance.jumpDistance = Mathf.Abs(PlayerController.Instance.transform.position.x 
            - rb.position.x - BossScript.instance.attackRange);
    }

}
