using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Fighter : NetworkBehaviour
{
    enum Status
    {
        Idle,
        Run,
        Jump,
        Attack,
        GetPushed,
        Fall
    }

    public static readonly float gravity = -20f;

    [SyncVar]
    public string nickname;

    [SyncVar]
    private int selectedFighter;

    public PlayerController playerController;
    public Transform shadow;
    public Text nameText;
    public Text aliveText;

    [SyncVar]
    private Status currentStatus;

    public float AliveTime { get; private set; }     // Alive time calculated on the server.
    private float aliveTimeUpdatCountDown;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform body;

    private float verticalSpeed = 0f;

    [SyncVar]
    private bool facingRight;

    private bool grounded;

    private bool falling;

    private bool canAttack;
    private float attackingCountDown;
    public bool Attacking { get { return attackingCountDown > 0f; } }

    private float pushedCountDown;
    public bool Pushed { get { return pushedCountDown > 0f; } }

    public bool CanAct { get { return !falling && !Pushed && !Attacking; } }

    private float playerControllerCheckCountDown;

    public override void OnStartServer()
    {
        GameController.Instance.RegisterFighter(this);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CapsuleCollider2D>();
        CreateBody();

        grounded = true;

        facingRight = true;

        nameText.text = nickname;

        if (isClient && !isServer)
        {
            myCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        CheckInAir(deltaTime);

        // Server is in charge of logic.
        if (isServer)
        {
            AliveTime += deltaTime;

            aliveTimeUpdatCountDown -= deltaTime;
            if (aliveTimeUpdatCountDown <= 0)
            {
                aliveTimeUpdatCountDown = 0.1f;
                RpcUpdateAliveTime(AliveTime);
            }

            // Check if the player who controls is disconnected.
            if (playerControllerCheckCountDown > 0f)
            {
                playerControllerCheckCountDown -= deltaTime;
            }
            else
            {
                playerControllerCheckCountDown = 1f;
                // If the player in charge is disconnected, destroy self.
                if (playerController == null)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }

            switch (currentStatus)
            {
                case Status.Idle:
                    rb.drag = 10f;
                    break;
                case Status.Run:
                    rb.drag = 10f;

                    if (rb.velocity.x > 0)
                    {
                        facingRight = true;
                    }
                    else if (rb.velocity.x < 0)
                    {
                        facingRight = false;
                    }

                    if (rb.velocity.sqrMagnitude < 1f)
                    {
                        currentStatus = Status.Idle;
                    }
                    break;
                case Status.Jump:
                    rb.drag = 1.8f;

                    if (body.localPosition.y <= 0)
                    {
                        grounded = true;

                        if (canAttack)
                        {
                            canAttack = false;
                            attackingCountDown = 0.36f;
                            var wave = Instantiate(ResourceManager.Instance.wavePrefab) as Wave;
                            wave.caster = this;
                            wave.transform.localPosition = transform.localPosition;
                            RpcCreateWave(transform.localPosition);
                            currentStatus = Status.Attack;
                        }
                    }
                    break;
                case Status.Attack:
                    rb.drag = 10f;
                    if (Attacking)
                    {
                        attackingCountDown -= deltaTime;
                    }
                    else
                    {
                        currentStatus = Status.Idle;
                    }
                    break;
                case Status.GetPushed:
                    rb.drag = 10f;
                    if (Pushed)
                    {
                        pushedCountDown -= deltaTime;
                    }
                    else
                    {
                        currentStatus = Status.Idle;
                    }
                    break;
                case Status.Fall:
                    rb.drag = 0f;
                    break;
                default:
                    break;
            }
        }

        // Animations.
        if (currentStatus != Status.Fall)
        {
            spriteRenderer.sortingOrder = (int)(transform.localPosition.y * -100);
        }

        spriteRenderer.flipX = !facingRight;

        switch (currentStatus)
        {
            case Status.Idle:
                animator.SetBool("Run", false);
                animator.SetBool("Jump", false);
                animator.SetBool("Fall", false);
                break;
            case Status.Run:
                animator.SetBool("Run", true);
                break;
            case Status.Jump:
                animator.SetBool("Jump", true);
                break;
            case Status.Attack:
                animator.SetBool("Run", false);
                animator.SetBool("Jump", false);
                animator.SetBool("Fall", false);
                break;
            case Status.GetPushed:
                animator.SetBool("Run", false);
                animator.SetBool("Jump", false);
                animator.SetBool("Fall", false);
                break;
            case Status.Fall:
                animator.SetBool("Fall", true);
                break;
            default:
                break;
        }
    }

    private void CheckInAir(float deltaTime)
    {
        if (body.localPosition.y > 0f || verticalSpeed > 0f)
        {
            verticalSpeed += gravity * deltaTime;
            var tmpBodyLocalPosition = body.localPosition;
            tmpBodyLocalPosition.y += verticalSpeed * deltaTime;

            if (tmpBodyLocalPosition.y <= 0f)
            {
                tmpBodyLocalPosition.y = 0f;
                verticalSpeed = 0f;
            }

            body.localPosition = tmpBodyLocalPosition;
            shadow.localScale = Vector3.one * ((5f - body.localPosition.y) / 5f);
        }
    }

    [ClientRpc]
    private void RpcCreateWave(Vector3 localPosition)
    {
        if (isServer)
        {
            return;
        }

        var wave = Instantiate(ResourceManager.Instance.wavePrefab) as Wave;
        wave.caster = this;
        wave.transform.localPosition = transform.localPosition;
    }

    [Server]
    public void Setup(int selectedFighter, PlayerController playerController, string nickname)
    {
        this.selectedFighter = selectedFighter;
        this.playerController = playerController;
        this.nickname = nickname;
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
            currentStatus = Status.Run;
            rb.velocity = new Vector2(x, y);
        }
    }

    [Server]
    public void Jump()
    {
        if (CanAct && grounded)
        {
            canAttack = true;
            grounded = false;
            verticalSpeed = 7.2f;

            currentStatus = Status.Jump;
            RpcJump();
        }
    }

    [ClientRpc]
    private void RpcJump()
    {
        verticalSpeed = 7.2f;
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

            falling = true;
            myCollider.isTrigger = true;

            currentStatus = Status.Fall;

            // Set gravity for falling.
            rb.gravityScale = 2f;

            // Out of ring.
            RpcFall();
        }
    }

    [ClientRpc]
    private void RpcFall()
    {
        myCollider.isTrigger = true;

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

    }

    [ClientRpc]
    private void RpcUpdateAliveTime(float aliveTime)
    {
        // Update alive time on the GUI.
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendFormat("{0:0.0}", aliveTime);
        aliveText.text = stringBuilder.ToString();
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

            myCollider.isTrigger = false;

            Reset();
            RpcReset();
        }
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
            if (currentStatus != Status.Fall && Pushed && wave.caster != this)
            {
                pushedCountDown = 0.1f;
                currentStatus = Status.GetPushed;

                // Add force.
                var relativeVec = transform.position - collider2D.transform.position;
                var distance = relativeVec.magnitude;
                var dir = relativeVec / distance;
                rb.AddForce(dir * (Mathf.Max(2.5f - distance, 0.5f)), ForceMode2D.Impulse);
            }
        }
    }

    [ClientRpc]
    private void RpcReset()
    {
        if (isServer)
        {
            return;
        }

        Reset();
    }

    public void Reset()
    {
        grounded = true;
        falling = false;
        canAttack = false;

        AliveTime = 0f;

        attackingCountDown = 0f;
        pushedCountDown = 0f;

        spriteRenderer.sortingLayerName = "Foreground";

        verticalSpeed = 0;

        body.localPosition = Vector3.zero;

        currentStatus = Status.Idle;
        animator.SetBool("Run", false);
        animator.SetBool("Jump", false);
        animator.SetBool("Fall", false);

        // Turn on the shadow.
        shadow.gameObject.SetActive(true);

        ResourceManager.Instance.arena.ActivateAnimation();
    }

    private void OnDestroy()
    {
        if (isServer)
        {
            GameController.Instance.RemoveFighter(this);
        }
    }
}
