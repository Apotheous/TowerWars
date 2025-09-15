using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyModel
{
    public float movementDirection;
    public MyWeapon myWeapon;
    public RotationClass rotationClass;
    public float moveSpeed;
    public float damage;
    public Image healthBar;
    public AnimatoinClass animatoinClass;
    public Rigidbody bulletPrefab;
    public float CurrentHealth { get; set; }

    public EnemyType enemyType; // Enemy type
    public LevelProp level;     // Enemy level

    public enum EnemyType
    {
        Bomber,
        Gunner,
        Laser
    }

    public enum LevelProp
    {
        LEVEL_ONE,
        LEVEL_TWO,
        LEVEL_THREE
    }

    public class MyWeapon
    {
        public float GizmosRange;
        public float attackRange;

        public MyWeapon(float gizmosRange, float attackRange)
        {
            GizmosRange = gizmosRange;
            attackRange = attackRange;
        }
    }

    public class RotationClass
    {
        public float rotationSpeed;
        public float notDeep;
        public float notDeepT;
        public float BarrelHeightAllowance;

        public RotationClass(float rotationSpeed, float notDeep, float notDeepT, float barrelHeightAllowance)
        {
            this.rotationSpeed = rotationSpeed;
            this.notDeep = notDeep;
            this.notDeepT = notDeepT;
            this.BarrelHeightAllowance = barrelHeightAllowance;
        }
    }

    public class AnimatoinClass
    {
        public string ENEMY_IDLE;
        public string ENEMY_WALK_FRONT;
        public string ENEMY_WALK_BACK;
        public string ENEMY_SHOOT_AUTO;
        public string ENEMY_DIE;

        public AnimatoinClass(string idle, string walkFront, string walkBack, string shootAuto, string die)
        {
            ENEMY_IDLE = idle;
            ENEMY_WALK_FRONT = walkFront;
            ENEMY_WALK_BACK = walkBack;
            ENEMY_SHOOT_AUTO = shootAuto;
            ENEMY_DIE = die;
        }
    }

    // Constructor
    public EnemyModel(
        float movementDirection,
        MyWeapon myWeapon,
        RotationClass rotationClass,
        float moveSpeed,
        float damage,
        Image healthBar,
        AnimatoinClass animatoinClass,
        Rigidbody bulletPrefab,
        float currentHealth,
        EnemyType enemyType,
        LevelProp level
    )
    {
        this.movementDirection = movementDirection;
        this.myWeapon = myWeapon;
        this.rotationClass = rotationClass;
        this.moveSpeed = moveSpeed;
        this.damage = damage;
        this.healthBar = healthBar;
        this.animatoinClass = animatoinClass;
        this.bulletPrefab = bulletPrefab;
        this.CurrentHealth = currentHealth;
        this.enemyType = enemyType;
        this.level = level;

        // Set properties based on enemy type and level
        SetLevelProperties(enemyType, level);
    }

    // Set properties based on enemy type and level
    private void SetLevelProperties(EnemyType type, LevelProp level)
    {
        switch (type)
        {
            case EnemyType.Bomber:
                SetBomberProperties(level);
                break;
            case EnemyType.Gunner:
                SetGunnerProperties(level);
                break;
            case EnemyType.Laser:
                SetLaserProperties(level);
                break;
        }
    }

    private void SetBomberProperties(LevelProp level)
    {
        switch (level)
        {
            case LevelProp.LEVEL_ONE:
                damage = 50.0f;
                moveSpeed = 3.0f;
                CurrentHealth = 100.0f;
                break;
            case LevelProp.LEVEL_TWO:
                damage = 70.0f;
                moveSpeed = 3.5f;
                CurrentHealth = 150.0f;
                break;
            case LevelProp.LEVEL_THREE:
                damage = 100.0f;
                moveSpeed = 4.0f;
                CurrentHealth = 200.0f;
                break;
        }
    }

    private void SetGunnerProperties(LevelProp level)
    {
        switch (level)
        {
            case LevelProp.LEVEL_ONE:
                damage = 30.0f;
                moveSpeed = 2.5f;
                CurrentHealth = 80.0f;
                break;
            case LevelProp.LEVEL_TWO:
                damage = 50.0f;
                moveSpeed = 3.0f;
                CurrentHealth = 120.0f;
                break;
            case LevelProp.LEVEL_THREE:
                damage = 80.0f;
                moveSpeed = 3.5f;
                CurrentHealth = 160.0f;
                break;
        }
    }

    private void SetLaserProperties(LevelProp level)
    {
        switch (level)
        {
            case LevelProp.LEVEL_ONE:
                damage = 40.0f;
                moveSpeed = 2.0f;
                CurrentHealth = 90.0f;
                break;
            case LevelProp.LEVEL_TWO:
                damage = 60.0f;
                moveSpeed = 2.5f;
                CurrentHealth = 130.0f;
                break;
            case LevelProp.LEVEL_THREE:
                damage = 90.0f;
                moveSpeed = 3.0f;
                CurrentHealth = 180.0f;
                break;
        }
    }
}
