using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyDieState : EnemyState
{
    public EnemyDieState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {

    }
    public override void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("DieStateEnemy");
        enemy.GetComponent<BoxCollider>().enabled = false;
        enemy.GetComponent<Rigidbody>().isKinematic = true;
        enemy.ChangeAnimationState(enemy.animatoinClass.ENEMY_DIE);
        //Destroy(enemy.gameObject, 0.5f);
        enemy.gameObject.SetActive(false);
        //enemy.transform.SetParent(EnemyMainBase.instance.obj_Pool.transform);


        enemy.moveSpeed = 0;
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.GetComponent<BoxCollider>().enabled = true;
        enemy.GetComponent<Rigidbody>().isKinematic = false;
        enemy.gameObject.SetActive(true);
        enemy.CurrentHealth = enemy.MaxHealth;
        enemy.moveSpeed = 2;
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
