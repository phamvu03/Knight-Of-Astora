using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss_Lunge : StateMachineBehaviour
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
        rb.gravityScale = 0;
        int dir = BossScript.instance.facingLeft ? 1 : -1;
        rb.linearVelocity = new Vector2( dir * BossScript.instance.speed * 5, 0f);
        //rb.transform.x += 
        if (Vector2.Distance(rb.position, PlayerController.Instance.transform.position) <= BossScript.instance.attackRange
            && !BossScript.instance.dmgPlayer)
        {
            PlayerController.Instance.TakeDamage(BossScript.instance.damage, rb.transform.position);
        }
            
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }
}
