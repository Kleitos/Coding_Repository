using UnityEngine;

public class CustomPositionConstraint : MonoBehaviour
{
    public Transform target;
    [Range(0f, 1f)] public float weight = 1f;

    public Vector3 maxOffset = new Vector3(1f, 1f, 1f); // Max allowed offset per axis
    public bool freezeX = false;
    public bool freezeY = false;
    public bool freezeZ = false;

    private Vector3 initialLocalPosition;
    private Vector3 targetOffset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CustomPositionConstraint: Target not assigned.");
            enabled = false;
            return;
        }

        initialLocalPosition = transform.localPosition;

        if (transform.parent != null)
        {
            Vector3 localTargetPos = transform.parent.InverseTransformPoint(target.position);
            targetOffset = initialLocalPosition - localTargetPos;
        }
        else
        {
            targetOffset = transform.localPosition - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredWorldPos = target.position + targetOffset;
        Vector3 desiredLocalPos = transform.parent != null
            ? transform.parent.InverseTransformPoint(desiredWorldPos)
            : desiredWorldPos;

        Vector3 offsetDelta = desiredLocalPos - initialLocalPosition;

        // Clamp and freeze axes
        if (freezeX) offsetDelta.x = 0f;
        else offsetDelta.x = Mathf.Clamp(offsetDelta.x, -maxOffset.x, maxOffset.x);

        if (freezeY) offsetDelta.y = 0f;
        else offsetDelta.y = Mathf.Clamp(offsetDelta.y, -maxOffset.y, maxOffset.y);

        if (freezeZ) offsetDelta.z = 0f;
        else offsetDelta.z = Mathf.Clamp(offsetDelta.z, -maxOffset.z, maxOffset.z);

        Vector3 constrainedPos = initialLocalPosition + offsetDelta;

        transform.localPosition = Vector3.Lerp(transform.localPosition, constrainedPos, weight);
    }
}
