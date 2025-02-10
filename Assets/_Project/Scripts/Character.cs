using System.Linq;
using System.Threading;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("Character Setttings")]
    [SerializeField] private float RideSpringStrength = 100f;
    [SerializeField] private float RideSpringDamper = 5f;
    [SerializeField] private float height = 2f;    
    [SerializeField] private float rideHeight = 1f;    
    [SerializeField] protected float originOffset = 1f;
    [SerializeField] protected Rigidbody r;
    [SerializeField] private Collider collider;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private string[] movingPlatformsTags;
    private RaycastHit _rayHit;
    protected bool isGrounded;

    private RaycastHit raycastHit;
    int maxBounces = 5;
    float skinWidth = 0.005f;
    float maxSlopeAngle = 55;

    protected Bounds bounds;
    private Vector3 targetVelocity;
    protected Vector3 moveAmount;

    protected Vector3 externalForce = Vector3.zero;
    protected Vector3 platformVelocity = Vector3.zero;

    private Vector3 lastPlatformPosition;
    private Quaternion lastPlatformRotation;

    private bool onPlatform;

    private bool im_static;


    protected void Start()
    {
        bounds = collider.bounds;
        bounds.Expand(-2 * skinWidth);
    }

    void GroundCheck(){
        isGrounded = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + originOffset, transform.position.z),
                                 Vector3.down, out _rayHit, height, layerMask);
        if (isGrounded && _rayHit.rigidbody != null && !onPlatform){
            lastPlatformPosition = _rayHit.rigidbody.position;
            lastPlatformRotation = _rayHit.rigidbody.rotation;
            //onPlatform = true;
        }
        else if (!isGrounded && onPlatform){
            onPlatform = false;
        }
    }

    Vector3 PlatformMovement()
    {
        if (!onPlatform) return Vector3.zero;
        Vector3 platformMovement = (_rayHit.rigidbody.position - lastPlatformPosition) / Time.fixedDeltaTime;

        Quaternion platformRotationDelta = _rayHit.rigidbody.rotation * Quaternion.Inverse(lastPlatformRotation);
        Vector3 offset = r.position - _rayHit.rigidbody.position;
        Vector3 rotatedOffset = platformRotationDelta * offset;

        Vector3 rotationalMovement = (rotatedOffset - offset) / Time.fixedDeltaTime;

        Vector3 totalVelocity = platformMovement + rotationalMovement;

        lastPlatformPosition = _rayHit.rigidbody.position;

        totalVelocity.y = 0;

        return totalVelocity;
    }

    Quaternion PlatformRotation(){
        if (!onPlatform) return r.rotation;

        Quaternion platformRotationDelta = _rayHit.rigidbody.rotation * Quaternion.Inverse(lastPlatformRotation);
        lastPlatformRotation = _rayHit.rigidbody.rotation;
        return platformRotationDelta * r.rotation;
    }

    protected void MakeMeStatic(){
        r.interpolation = RigidbodyInterpolation.None;
        r.isKinematic = true;
        im_static = true;
    }
    protected void MakeMeNONStatic(){
        r.interpolation = RigidbodyInterpolation.Interpolate;
        r.isKinematic = false;
        im_static = false;
    }

    protected void FixedUpdate()
    {
        if (im_static){
            GroundCheck();
            PlatformMovement();
            PlatformRotation();
            return;
        }
        GroundCheck();

        Vector3 worldVel = transform.TransformDirection(targetVelocity);
        ApplySpringForce();

        if (!isGrounded) 
            worldVel = Move(worldVel + externalForce + platformVelocity);
        else
            worldVel = Move(worldVel + externalForce);
        worldVel += PlatformMovement();
        worldVel.y += r.linearVelocity.y;
        r.linearVelocity = worldVel;
        r.rotation = PlatformRotation();
    }

    public void MoveCharacter(Vector3 moveDir){
        targetVelocity = moveDir;
    }

    private Vector3 Move(Vector3 _moveAmount)
    {
        moveAmount = CollideAndSlide(_moveAmount, transform.position + Vector3.up * originOffset, 0, false, _moveAmount);
        return moveAmount;
    }

    private void ApplySpringForce()
    {
        if (isGrounded)
        {
            Vector3 velocity = r.linearVelocity;
            platformVelocity = _rayHit.rigidbody ? _rayHit.rigidbody.linearVelocity : Vector3.zero;
            //platformAngularVelocity = _rayHit.rigidbody ? _rayHit.rigidbody.angularVelocity : Vector3.zero;

            float rayDirVel = Vector3.Dot(Vector3.down, velocity);
            //float otherDirVel = Vector3.Dot(Vector3.down, otherVelocity);
            float relativeVelocity = rayDirVel; // rayDirVel - otherDirVel;

            float distanceFromGround = _rayHit.distance - rideHeight;
            float springForce = (distanceFromGround * RideSpringStrength) - (relativeVelocity * RideSpringDamper);

            Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + originOffset, transform.position.z),
                                 Vector3.down * height, Color.red);
            
            //if (externalForce.y > 0)
                //externalForce -= Vector3.down * springForce;
            if (Mathf.Abs(externalForce.y) < 0.1f)
                r.AddForce(Vector3.down * springForce);
        }
        else{
            platformVelocity *= .999f;
            if (platformVelocity.magnitude <= 0.1f) platformVelocity = Vector3.zero;
            //platformAngularVelocity = Vector3.zero;
        }
        //r.AddForce(externalForce);
    }

    Vector3 ProjectAndScale(Vector3 vec, Vector3 normal) {
        float mag = vec.magnitude;
        vec = Vector3.ProjectOnPlane(vec, normal).normalized;
        vec *= mag;
        return vec;
    }

    private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit) {
        if(depth >= maxBounces) 
            return Vector3.zero;

        float dist = vel.magnitude + skinWidth;
        dist = Mathf.Clamp(dist, 0, 0.2f);

        RaycastHit hit;
        if(
            Physics.SphereCast(pos, bounds.extents.x, vel.normalized, out hit, dist, layerMask)
        ) {
            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if(snapToSurface.magnitude <= skinWidth) {
                snapToSurface = Vector3.zero;
            }

            // normal ground / slope
            if(angle <= maxSlopeAngle) {
                if(gravityPass) {
                    return snapToSurface;
                }
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            // wall or steep slope
            else {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                );

                if(isGrounded && !gravityPass) {
                    leftover = ProjectAndScale(
                        new Vector3(leftover.x, 0, leftover.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z).normalized
                    ) * scale;
                } else {
                    leftover = ProjectAndScale(leftover, hit.normal) * scale;
                }
            }

            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth+1, gravityPass, velInit);
        }

        return vel;
    }
}
