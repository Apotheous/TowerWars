using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] float movementSpeedBase = 5;

    private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier;
    private Vector2 currentMoveDirection;
    public int playerScore;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
        Attack();
    }

    private void Move()
    {
        //if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal"); // A, D veya Sol/Sað ok
        float v = Input.GetAxis("Vertical");   // W, S veya Yukarý/Aþaðý ok

        Vector3 move = new Vector3(h, 0f, v) * movementSpeedBase * Time.deltaTime;
        transform.Translate(move, Space.World);
    }


    private bool canAttack = true;
    private void Attack()
    {
        if (Input.GetMouseButton(0))
        {
            movementSpeedMultiplier = 0.5f;
            animator.SetFloat("Attack", 1);

            if (canAttack)
            {
                RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 1f, currentMoveDirection, 0, 1 << 6);

                if (hits.Length > 0)
                {
                    hits[0].transform.GetComponent<HealthSystem>().OnDamageDealt(50);
                    if (hits[0].transform.GetComponent<HealthSystem>().health < 0)
                    {
                        playerScore++;
                    }
                }

                StartCoroutine(AttackCooldown());
            }
        }
        else
        {
            animator.SetFloat("Attack", 0);
            movementSpeedMultiplier = 1f;
        }
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1);
        canAttack = true;
    }
}
