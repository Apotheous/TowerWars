using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Zenject;

public class EnemyAttackState : EnemyState
{
    private Transform _playerTransform;
    private GameObject myBullet;
    private Rigidbody myBulletRb;
    private float _timer;
    private float _timeBetweenShots = 2f;

    private float _exitTimer;
    private float _timeTillExit;
    private float _distancetoCountExit = 3f;
    private float bulletSpeed = 10f;

    //private PoolStorage poolStorage;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {

    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("AttackStateEnemy");
        //enemy.targetDir = (enemy.target.position - enemy.transform.position).normalized;
        //// Hedef ile pozisyon aras�ndaki fark� hesaplay�n, Y eksenini s�f�rlay�n
        //Vector3 targetDir = enemy.target.position - enemy.transform.position;

        //// Y eksenini s�f�rlayarak sadece XZ d�zleminde hesaplama yap�yoruz
        //targetDir.y = 0;

        //// Y�n vekt�r�n� normalize et (birim vekt�r yap)
        //targetDir = targetDir.normalized;

        //// D��man� hedefe do�ru d�nd�r (sadece yatay eksende)
        //enemy.transform.rotation = Quaternion.LookRotation(targetDir);

        enemy.ChangeAnimationState(enemy.animatoinClass.ENEMY_SHOOT_AUTO);

    }

    public override void ExitState()
    {
        base.ExitState();

    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        Debug.Log("AttackStateEnemy");

        _timer += Time.deltaTime;
        if (_timer > _timeBetweenShots)
        {
            _timer = 0;

            // Havuzdan mermi al

            myBulletRb = myBullet.GetComponent<Rigidbody>();
            myBullet.transform.position = enemy.myWeapon.myBarrelT.position;

            // Hedefe y�nlendir
            Vector3 targetDir = (enemy.target.position - enemy.myWeapon.myBarrelT.position).normalized;
            myBullet.transform.forward = targetDir;

            // Kuvvet uygula
            myBulletRb.AddForce(targetDir * bulletSpeed, ForceMode.Impulse);

        }

        if (Vector2.Distance(enemy.target.position, enemy.transform.position) > _distancetoCountExit)
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer > 0)
            {
                enemy.StateMachine.ChangeState(enemy.ChaseState);
            }
        }
        else
        {
            _exitTimer = 0;
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    private void BulletFiring()
    {

    }
    private void BombExplosive()
    {

    }  




    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, myWeapon.explosionRadius);
    }
}
