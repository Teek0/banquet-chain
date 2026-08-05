using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class CameraFollow2D : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Suavizado")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    private Vector3 smoothingVelocity;

    private void Start()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null || Time.timeScale <= 0f)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothingVelocity,
            smoothTime
        );
    }
}