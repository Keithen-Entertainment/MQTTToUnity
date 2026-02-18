using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAnimatorScript : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            animator.SetBool("movecamera", true);// Zet de bool "movecamera" op true om de animatie te starten
        }
    }
}
