using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody2D))]
public class Fighter : NetworkBehaviour
{
    public static readonly float gravity = -20f;

    [SyncVar]
    private int selectedFighter;

    public PlayerController playerController;
    public Transform shadow;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform body;

    private float verticalSpeed = 0f;

    private bool grounded;
    private bool jumping;

    private bool falling;

    private bool canAttack;
    private float attackingCountDown;
    public bool Attacking { get { return attackingCountDown > 0f; } }

    [SyncVar]
    private float pushedCountDown;
    public bool Pushed { get { return pushedCountDown > 0f; } }

    public bool CanAct { get { return !falling && !Pushed && !Attacking; } }

    private float playerControllerCheckCountDown;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CapsuleCollider2D>();
        CreateBody();

        grounded = true;
        jumping = false;
    }

    void Update()
    {
        if (isServer)
        {
            if (playerControllerCheckCountDown > 0f)
            {
                playerControllerCheckCountDown -= Time.deltaTime;
            }
            else
            {
                playerControllerCheckCountDown = 1f;
                if (playerController == null)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }

        if (Attacking)
        {
            attackingCountDown -= Time.deltaTime;
        }

        if (Pushed)
        {
            pushedCountDown -= Time.deltaTime;
        }

        if (!falling)
        {
            spriteRenderer.sortingOrder = (int)(transform.localPosition.y * -100);

            verticalSpeed += gravity * Time.deltaTime;

            var localPosition = body.localPosition;

            localPosition.y += verticalSpeed * Time.deltaTime;

            if (localPosition.y <= 0)
            {
                localPosition.y = 0;
                grounded = true;

                if (jumping)
                {
                    jumping = false;
                    animator.SetBool("Jump", false);
                }

                if (canAttack)
                {
                    canAttack = false;
                    attackingCountDown = 0.36f;
                    var wave = Instantiate(ResourceManager.Instance.wavePrefab) as Wave;
                    wave.caster = this;
                    wave.transform.localPosition = transform.localPosition;
                }
            }

            shadow.localScale = Vector3.one * ((5f - localPosition.y) / 5f);

            body.localPosition = localPosition;

            // Check facing direction.
            if (grounded)
            {
                rb.drag = 10f;

                if (!Pushed)
                {
                    if ((spriteRenderer.flipX && rb.velocity.x > 0) || (!spriteRenderer.flipX && rb.velocity.x < 0))
                    {
                        spriteRenderer.flipX = !spriteRenderer.flipX;
                    }

                    if (rb.velocity.sqrMagnitude > 1f)
                    {
                        animator.SetBool("Run", true);
                    }
                    else if (rb.velocity.sqrMagnitude < 0.3f)
                    {
                        animator.SetBool("Run", false);
                    }
                }
            }
            else
            {
                rb.drag = 1.6f;
            }

            if (Pushed)
            {
                animator.SetBool("Run", false);
            }
        }
    }

    [Server]
    public void Setup(int selectedFighter, PlayerController playerController)
    {
        this.selectedFighter = selectedFighter;
        this.playerController = playerController;
    }

    private void CreateBody()
    {
        var bodyPrefab = ResourceManager.Instance.bodyPrefabs[selectedFighter];
        var bodyGO = Instantiate(bodyPrefab, transform);
        body = bodyGO.transform;
        body.localPosition = new Vector3(0f, 0f, 0f);

        animator = body.GetComponent<Animator>();
        spriteRenderer = body.GetComponent<SpriteRenderer>();
    }

    [Server]
    public void SetVelocity(float x, float y)
    {
        if (CanAct && grounded)
        {
            rb.velocity = new Vector2(x, y);
        }
    }

    [Server]
    public void Jump()
    {
        if (CanAct && grounded)
        {
            RpcJump();
        }
    }

    [ClientRpc]
    private void RpcJump()
    {
        canAttack = true;

        grounded = false;
        jumping = true;
        verticalSpeed = 7.2f;
        animator.SetBool("Jump", true);
    }

    private void OnTriggerExit2D(Collider2D collider2D)
    {
        if (!isServer)
        {
            return;
        }

        if (collider2D.CompareTag("Arena"))
        {
            if (falling)
            {
                return;
            }

            // Out of ring.
            RpcFall();
        }
    }

    [ClientRpc]
    private void RpcFall()
    {
        falling = true;
        myCollider.isTrigger = true;

        animator.SetBool("Jump", false);
        animator.SetBool("Run", false);
        animator.SetBool("Fall", true);

        if (transform.localPosition.y > -1f)
        {
            spriteRenderer.sortingLayerName = "Arena";
            spriteRenderer.sortingOrder = -1;
        }
        else
        {
            spriteRenderer.sortingOrder = 2;
        }

        // Turn of the shadow.
        shadow.gameObject.SetActive(false);

        // Set gravity for falling.
        rb.gravityScale = 2f;
        rb.drag = 0f;
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (!isServer)
        {
            return;
        }

        if (collider2D.CompareTag("Wave"))
        {
            if (body.localPosition.y < 0.8f)
            {
                var wave = collider2D.GetComponent<Wave>();
                if (wave.caster != this)
                {
                    pushedCountDown = 0.1f;
                }
            }
        }
        else if (collider2D.CompareTag("DeathZone"))
        {
            rb.gravityScale = 0;
            rb.velocity = Vector2.zero;
            transform.localPosition = new Vector3(0f, -1f, 0f);
            RpcReset();
        }
    }

    [ClientRpc]
    private void RpcReset()
    {
        Reset();
    }

    public void Reset()
    {
        grounded = true;
        jumping = false;
        falling = false;
        canAttack = false;

        attackingCountDown = 0f;
        pushedCountDown = 0f;

        myCollider.isTrigger = false;

        spriteRenderer.sortingLayerName = "Foreground";

        verticalSpeed = 0;

        body.localPosition = Vector3.zero;

        animator.SetBool("Run", false);
        animator.SetBool("Jump", false);
        animator.SetBool("Fall", false);

        // Turn on the shadow.
        shadow.gameObject.SetActive(true);

        ResourceManager.Instance.arena.ActivateAnimation();
    }

    private void OnTriggerStay2D(Collider2D collider2D)
    {
        if (!isServer)
        {
            return;
        }

        if (collider2D.CompareTag("Wave"))
        {
            var wave = collider2D.GetComponent<Wave>();
            if (Pushed && wave.caster != this)
            {
                pushedCountDown = 0.1f;

                // Add force.
                var relativeVec = transform.position - collider2D.transform.position;
                var distance = relativeVec.magnitude;
                var dir = relativeVec / distance;
                rb.AddForce(dir * (Mathf.Max(2.5f - distance, 0.5f)), ForceMode2D.Impulse);
            }
        }
    }
}
