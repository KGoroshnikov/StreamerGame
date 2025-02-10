using UnityEngine;

public class ItemShaker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform holder;

    [Header("Settings")]
    [SerializeField] private float maxOffset = 10f;
    [SerializeField] private float influence = 0.5f;
    [SerializeField] private float returnSpeed = 5f;

    private Vector3 previousRotation;
    private Vector3 currentOffset;
    private Transform camTransform;

    private void Start()
    {
        camTransform = Camera.main.transform;
        previousRotation = Camera.main.transform.eulerAngles;
        currentOffset = Vector3.zero;
    }

    private void Update()
    {
        Vector3 currentCamRotation = camTransform.eulerAngles;

        Vector3 deltaRotation = new Vector3(
            Mathf.DeltaAngle(previousRotation.x, currentCamRotation.x),
            Mathf.DeltaAngle(previousRotation.y, currentCamRotation.y),
            Mathf.DeltaAngle(previousRotation.z, currentCamRotation.z)
        );
        
        Vector3 angularVelocity = deltaRotation / Time.deltaTime;
        currentOffset += angularVelocity * influence * Time.deltaTime;
        currentOffset = Vector3.ClampMagnitude(currentOffset, maxOffset);
        currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, returnSpeed * Time.deltaTime);
        holder.localRotation = Quaternion.Euler(-currentOffset);
        previousRotation = currentCamRotation;
    }
}
