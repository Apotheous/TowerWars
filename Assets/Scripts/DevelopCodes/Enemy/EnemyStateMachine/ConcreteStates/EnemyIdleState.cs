using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyIdleState : EnemyState
{

    private Vector3 _targetPos;
    private Vector3 _direction;
    public EnemyIdleState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
        
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("IdleStateEnemy");
        enemy.moveSpeed = 0f;
        _targetPos = GetRandomPointInCircle();
        enemy.ChangeAnimationState(enemy.animatoinClass.ENEMY_WALK_FRONT);
        
    }

    private Vector3 GetRandomPointInCircle()
    {
        return enemy.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * enemy.RandomMovementrange;
        
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        Debug.Log("IdleStateEnemy");
        if (enemy.moveSpeed > 0)
        {
            enemy.MoveEnemyTowardsTarget(enemy.gameObject, enemy.moveSpeed);
        }
        else if ((enemy.transform.position - _targetPos).sqrMagnitude < 0.01f)
        {
            _targetPos = GetRandomPointInCircle();
        }
        else
        {
            enemy.ChangeAnimationState(enemy.animatoinClass.ENEMY_WALK_FRONT);
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
