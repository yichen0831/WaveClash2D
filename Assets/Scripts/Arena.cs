using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arena : MonoBehaviour
{

    private Animator animator;
    private float animationCountDown;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animationCountDown > 0)
        {
            animationCountDown -= Time.deltaTime;
            animator.SetBool("Active", true);
        }
        else
        {
            animator.SetBool("Active", false);
        }
    }

    public void ActivateAnimation()
    {
        animationCountDown += 1f;
    }
}
