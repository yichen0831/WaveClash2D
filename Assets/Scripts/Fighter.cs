using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Fighter : MonoBehaviour
{
    public static readonly float gravity = -20f;

    public Transform shadow;

    public bool Grounded { get { return body != null ? body.localPosition.y <= 0 : true; } }

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform body;

    private float verticalSpeed = 0f;

    private bool falling;
    private bool canAttack;
    private float attackingCountDown;
    public bool Attacking { get { return attackingCountDown > 0f; } }
    private float pushedCountDown;
    public bool Pushed { get { return pushedCountDown > 0f; } }
    public bool CanAct { get { return !falling && !Pushed && !Attacking; } }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
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
                animator.SetBool("Jump", false);

                if (canAttack)
                {
                    canAttack = false;
                    attackingCountDown = 0.36f;
                    var wave = Instantiate(GameController.Instance.wavePrefab) as Wave;
                    wave.caster = this;
                    wave.transform.localPosition = transform.localPosition;
                }
            }

            shadow.localScale = Vector3.one * ((5f - localPosition.y) / 5f);

            body.localPosition = localPosition;

            // Check facing direction.
            if (Grounded)
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

    public void Setup(GameObject bodyPrefab)
    {
        var bodyGO = Instantiate(bodyPrefab, transform);
        body = bodyGO.transform;
        body.localPosition = new Vector3(0f, 0f, 0f);

        animator = body.GetComponent<Animator>();
        spriteRenderer = body.GetComponent<SpriteRenderer>();
    }

    public void Reset()
    {
        falling = false;
        canAttack = false;
        attackingCountDown = 0f;
        pushedCountDown = 0f;
        myCollider.isTrigger = false;
    }

    public void SetVelocity(float x, float y)
    {
        if (CanAct && Grounded)
        {
            rb.velocity = new Vector2(x, y);
        }
    }

    public void Jump()
    {
        if (CanAct)
        {
            verticalSpeed = 7.2f;
            animator.SetBool("Jump", true);
            canAttack = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collider2D)
    {
        if (collider2D.CompareTag("Arena"))
        {
            // Out of ring.
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
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
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

        }
    }

    private void OnTriggerStay2D(Collider2D collider2D)
    {
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
