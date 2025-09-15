using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Zenject;

public class EnemyStateMachine : MonoBehaviour
{
    public EnemyState CurrentEnemyState {  get; set; }
    // �rne�in, EnemyStateMachine i�inde bir de�i�ken
    //[Inject] private EnemyAttackStateFactory _enemyAttackStateFactory;
    public void Initialize(EnemyState startingState)
    {
        CurrentEnemyState = startingState;
        CurrentEnemyState.EnterState();


        //// EnemyAttackState �rne�i olu�turulurken
        //EnemyAttackState enemyAttackState = _enemyAttackStateFactory.Create(enemy, this);
    }

    public void ChangeState(EnemyState newState)
    {
        CurrentEnemyState.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterState();
    }
}
