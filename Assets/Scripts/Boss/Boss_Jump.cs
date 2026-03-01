using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;

public class Boss_Jump : StateMachineBehaviour
{
    Rigidbody2D rb;

    float distanceX;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        rb = animator.GetComponentInParent<Rigidbody2D>();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //get range between player and boss minus half of attack range
        //so that boss can get closer to player
        distanceX = Mathf.Abs(Mathf.Abs(PlayerController.Instance.transform.position.x -
            rb.position.x) - BossScript.instance.attackRange / 2);

        TargetPlayerPosition(animator);
        
        if (BossScript.instance.attackCountDown <= 0)
        {
            BossScript.instance.AttackHandler();
            BossScript.instance.attackCountDown = BossScript.instance.attackTimer;
        }
    }
    
    void TargetPlayerPosition(Animator animator)
    {
        if (PlayerController.Instance.IsOnGround())
        {
            rb.linearVelocity = Vector2.zero;
            float gravity = rb.gravityScale;                        
            float maxHeight = BossScript.instance.jumpForce;        
            float timeToPeak = Mathf.Sqrt(2 * maxHeight / gravity); 
            float totalTime = timeToPeak * 2;                       
            int direction = BossScript.instance.facingLeft ? -1 : 1;
            float velocityX = distanceX * direction / totalTime;
            float velocityY = Mathf.Sqrt(2 * gravity * maxHeight);  
            rb.linearVelocity = new Vector2(velocityX, velocityY);        //-> Important!!

            rb.AddForce(rb.linearVelocity, ForceMode2D.Impulse);
        }
        if (distanceX <= BossScript.instance.attackRange)
        {
            animator.SetBool("Jump", false);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }
}
