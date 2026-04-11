using UnityEngine;

public class AnimationStateControl : MonoBehaviour
{
    Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("w")){
            animator.SetBool("IsWalking",true);
        }else
        {
            animator.SetBool("IsWalking", false);
        }
    }
}
