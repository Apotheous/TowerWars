using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : EnemyState
{

    private Transform _towerTransform;
    private float _MovementSpeed = 1.75f;
    public EnemyChaseState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
        _towerTransform = enemy.target;
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("ChaseStateEnemy");
        //enemy.ChangeAnimationState(enemy.animatoinClass.ENEMY_DIE);
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        Debug.Log("ChaseStateEnemy");
        // Hedefe doðru hareket et
        //if (enemy.target == null)
        //{
        //    StateMachine.ChangeState(enemy.IdleState);
        //    return;
        //}
        //else
        //{
        if (enemy.target)
        {
            enemy.MoveEnemyTowardsTarget(enemy.target.gameObject, enemy.moveSpeed);
        }
        //}


    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
