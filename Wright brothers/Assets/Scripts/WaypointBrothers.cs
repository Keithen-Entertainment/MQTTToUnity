using UnityEngine;

public class WaypointBrothers : MonoBehaviour
{
    public Animator animator;
    public Transform waypoint;
    public float moveSpeed = 2f;
    public float arriveDistance = 0.1f;
    public Transform lookAtTarget;

    float startY;

    void Awake()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Walking"))
        {
            if (waypoint == null)
            {
                return;
            }

            Vector3 targetPosition = waypoint.position;
            targetPosition.y = startY;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if ((transform.position - targetPosition).sqrMagnitude <= arriveDistance * arriveDistance)
            {
                transform.position = targetPosition;
            }

            Transform target = lookAtTarget != null ? lookAtTarget : (Camera.main != null ? Camera.main.transform : null);
            if (target != null)
            {
                Vector3 lookPosition = target.position;
                lookPosition.y = transform.position.y;
                transform.LookAt(lookPosition);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Botsing met: " + collision.gameObject.name);
        animator.SetTrigger("Talking");
    }
}


//animator.SetBool("NextAnimation", true);
//zodat de volgende animatie wordt getriggerd wanneer broer bij waypoint is. rigidbody? Collider?