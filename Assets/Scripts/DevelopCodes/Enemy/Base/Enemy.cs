using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Enemy : MonoBehaviour
{
   


    public float movementDirection;
    
    public Rigidbody rb;
    public string enemyGroupTag;
    public Transform target;
    public Transform myCenter;
    public Vector3 targetDir;


    //-------**-*-*-*-
    [System.Serializable]
    public class MyWeapon
    {
        public Transform myBarrelT;
        //public BasicPool myPool;
        public EnemyGunnerType enemy_Gnnr_Type;
        
        public float GizmosRange;
        public float attackRange;
        

        public GameObject ammoPrefab;
        public float explosionRadius;
        public float damage;
    }

    public MyWeapon myWeapon;

    [System.Serializable]
    public class RotationClass
    {
        public Transform angleX;    // X ekseninde dönen parça
        public Transform angleY;    // Y ekseninde dönen parça
        public float rotationSpeed = 5f; // Dönme hızı

        public float notDeep;
        public float notDeepT;
        public float BarrelHeightAllowance;
    }

    public RotationClass rotationClass;


    public float moveSpeed;


    [Header("Unity Stuff")]
    public Image healthBar;

    private Animator animator;

    [System.Serializable]
    public class AnimatoinClass
    {
        //Animation States
        public string ENEMY_IDLE = "IdleCube";
        public string ENEMY_WALK_FRONT = "WalkCube";
        public string ENEMY_WALK_BACK = "WalkBackCube";
        public string ENEMY_SHOOT_AUTO = "AttackCube";
        public string ENEMY_DIE = "EnemyDie";

    }
    public AnimatoinClass animatoinClass;

    private string currentState;

    #region State Machine Variables
    public EnemyStateMachine StateMachine { get; set; }
    public EnemyIdleState IdleState { get; set; }
    public EnemyChaseState ChaseState { get; set;}
    public EnemyAttackState AttackState { get; set; }
    public EnemyDieState DieState { get; set; }

    #endregion

    #region Idle Veriables
    public float RandomMovementrange = 5f;
    public float randomMovementSpeed = 1f;
    

    #endregion

    [field: SerializeField] public float MaxHealth { get; set; }
    [field: SerializeField] public float CurrentHealth { get; set; }
    //Rigidbody IMoveable.rb { get; set; }
    public bool IsMovingForward { get; set; } = true;
    public bool IsAggroed { get; set; }
    public bool IsWithinStrikeingDistance { get; set; }

    private void Awake()
    {
        StateMachine = new EnemyStateMachine();

        IdleState = new EnemyIdleState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
        DieState = new EnemyDieState(this, StateMachine);
    }
    private void Start()
    {
        EnemyStartMeth();
    }

    public void EnemyStartMeth()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        CurrentHealth = MaxHealth;
        StateMachine.Initialize(IdleState);
        //TryModelController.Instance.enemies.Add(gameObject);

        InvokeRepeating("UpdateTargetWithGizmos", 0f, 0.5f);

        Debug.Log("EnemyStarted");
        healthBar.fillAmount = CurrentHealth / MaxHealth;
    }

    private void Update()
    {
       


        StateMachine.CurrentEnemyState.FrameUpdate();
        //// Mevcut duruma bağlı olarak her kare güncellenir
        // StateMachine.CurrentEnemyState.FrameUpdate();
        if (target == null)
        {
            // Eğer target yoksa saldırı veya kovalama durumuna geçme
            Debug.LogWarning("Target is null, not switching to Chase or Attack State.");
            StateMachine.ChangeState(IdleState);
            return;
        }
        else
        {
            //// Eğer düşman hedefe yakınsa ve saldırı menzilindeyse, saldırı durumuna geç
            if (Vector3.Distance(transform.position, target.position) < myWeapon.attackRange)
            {
                StateMachine.ChangeState(AttackState);
                // ChangeAnimationState(animatoinClass.ENEMY_SHOOT_AUTO);

            }
            // Eğer düşman hedefi görüyorsa ve menzile girdiyse kovala
            else if (Vector3.Distance(transform.position, target.position) < myWeapon.GizmosRange)
            {
                StateMachine.ChangeState(ChaseState);

            }
            else
            {
                return;
            }
        }


        
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentEnemyState.PhysicsUpdate();
    }


    public void ChangeAnimationState(string newState)
    {
        if (currentState == newState) return;
        animator.Play(newState);
        currentState = newState;
    }


    #region SelectionTarget

    void UpdateTargetWithGizmos()
    {
        // Gizmos menzili içinde bulunan düşmanları tarar
        Collider[] colliders = Physics.OverlapSphere(transform.position, myWeapon.GizmosRange);
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag(enemyGroupTag))
            {
                float distanceToEnemy = Vector3.Distance(transform.position, collider.transform.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    rotationClass.notDeepT = distanceToEnemy;

                    // average value notDeep 30f
                    if (rotationClass.notDeepT > rotationClass.notDeep)
                    {
                        nearestEnemy = collider.gameObject;
                        target = collider.transform;
                    }
                }
            }
        }

        if (nearestEnemy != null && shortestDistance <= myWeapon.GizmosRange)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }


    #endregion
    public void Damage(float damageAmount)
    {
        CurrentHealth -= damageAmount;
        healthBar.fillAmount = CurrentHealth / MaxHealth;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        StateMachine.ChangeState(DieState);
    }

    public void CheckForForwardOrBackFacing(Vector3 velocity)
    {
        if (IsMovingForward && moveSpeed < 0)
        {
            IsMovingForward = !IsMovingForward;
        }     
        else if (!IsMovingForward && moveSpeed>0)
        {
            IsMovingForward = !IsMovingForward;
        }
    }

    #region Animation Triggers

    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        StateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public enum AnimationTriggerType
    {
        EnemyDamaged,
        PlayFootstepSound
    }
    #endregion

    #region Distance Checks
    public void SetAggroStatus(bool aggroStatus)
    {
        IsAggroed = aggroStatus;
    }

    public void SetStrikingDistanceBool(bool isWithinStrikeingDistance)
    {
        IsWithinStrikeingDistance = isWithinStrikeingDistance;
    }

    public void MoveEnemyTowardsTarget(GameObject enemyObject, float MoveSpeed)
    {
        if (target == null)
        {
            Debug.LogWarning("Hedef pozisyon null!");
            transform.Translate(gameObject.transform.forward * moveSpeed * Time.deltaTime);
            return;
        }
        Vector3 direction = (target.position - transform.position).normalized;

        transform.Translate(direction * moveSpeed * Time.deltaTime);

    }

    public void ITriggerCheck(Collision contactObj)
    {
        //empty for now
    }
    #endregion
}

public enum EnemyGunnerType
{
    Bullet,
    Laser,
    Bomb
}
