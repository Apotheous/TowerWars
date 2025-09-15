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
        //// Hedef ile pozisyon arasýndaki farký hesaplayýn, Y eksenini sýfýrlayýn
        //Vector3 targetDir = enemy.target.position - enemy.transform.position;

        //// Y eksenini sýfýrlayarak sadece XZ düzleminde hesaplama yapýyoruz
        //targetDir.y = 0;

        //// Yön vektörünü normalize et (birim vektör yap)
        //targetDir = targetDir.normalized;

        //// Düþmaný hedefe doðru döndür (sadece yatay eksende)
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

            // Hedefe yönlendir
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
