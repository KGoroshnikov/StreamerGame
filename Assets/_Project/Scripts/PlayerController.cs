using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerContoller : Character
{
    [Header("Player Setttings")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpForce;

    private Vector3 jointOriginalPos;
    private float timer = 0;
    [SerializeField] private Transform joint;
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private Vector3 bobAmount = new Vector3(.15f, .05f, 0f);
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;
    public enum state{
        Running, Idle, NoUse
    }
    public state m_state;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    [SerializeField] private Vector2 mouseSensitivity;
    [SerializeField] private float maxLookAngle = 85f;

    [SerializeField] private InputActionReference wasd, look;

    private float forceDamping = 0.95f;
    private float minForceThreshold = 0.1f;

    void Start(){
        base.Start();
        jointOriginalPos = joint.localPosition;
        m_state = state.Idle;
        //animator.SetTrigger("ToIdle");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate(){
        base.FixedUpdate();
        CalculateExternalForce();
    }

    void Update()
    {
        Vector3 dir = Vector3.zero;
        if (m_state != state.NoUse){
            Rotating();
            //dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
            dir = new Vector3(wasd.action.ReadValue<Vector2>().x, 0, wasd.action.ReadValue<Vector2>().y);
            //if (isGrounded && Input.GetKeyDown(KeyCode.Space)) Jump();
            UpdateAnim(dir);
            HeadBob();
        }
        MoveCharacter(dir * walkSpeed);
    }

    private void Jump()
    {
        ApplyExplosionForce(Vector3.up * jumpForce, 0.55f);
        r.linearVelocity = new Vector3(r.linearVelocity.x, 0, r.linearVelocity.z);
    }

    void CalculateExternalForce(){
        if (externalForce.magnitude > minForceThreshold)
        {
            externalForce = new Vector3(externalForce.x * forceDamping,
                                        externalForce.y * (forceDamping),
                                        externalForce.z * forceDamping);
        }
        else
        {
            externalForce = Vector3.zero;
        }
    }

    public void ApplyExplosionForce(Vector3 force, float _forceDamping, bool OverrideEverything = false)
    {
        float weight = Mathf.Clamp01(force.magnitude / (externalForce.magnitude + force.magnitude));
        forceDamping = Mathf.Lerp(forceDamping, _forceDamping, weight);
        externalForce += force;

        if (OverrideEverything){ // explosion, 
            externalForce = force;
            forceDamping = _forceDamping;
            r.linearVelocity = Vector3.zero;
        }
    }

    void UpdateAnim(Vector3 dir){
        if (dir.magnitude >= 0.05f && m_state != state.Running)
        {
            m_state = state.Running;
            //animator.SetTrigger("ToWalking");
        }
        else if (dir.magnitude < 0.05f && m_state != state.Idle)
        {
            m_state = state.Idle;
            //animator.SetTrigger("ToIdle");
        }
    }

    private void HeadBob()
    {
        if(m_state == state.Running)
        {
            timer += Time.deltaTime * bobSpeed;
            joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            timer = 0;
            joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }

    void Rotating(){
        //yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
        //pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
        yaw = transform.localEulerAngles.y + look.action.ReadValue<Vector2>().x * mouseSensitivity.x;
        pitch -= mouseSensitivity.y * look.action.ReadValue<Vector2>().y;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        //transform.localEulerAngles = new Vector3(0, yaw, 0);
        r.rotation = Quaternion.Euler(new Vector3(0, yaw, 0));
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    public void FreezePlayer(){
        m_state = state.NoUse;
        //animator.SetTrigger("ToIdle");
        MakeMeStatic();
    }
    public void UnfreezePlayer(){
        m_state = state.Idle;
        MakeMeNONStatic();
    }

    public void EnterShip(Transform newparent){
        transform.SetParent(newparent);
        m_state = state.NoUse;
        //animator.SetTrigger("ToIdle");
        playerCamera.gameObject.SetActive(false);
        MakeMeStatic();
    }
    public void ExitShip(){
        transform.SetParent(null);
        m_state = state.Idle;
        playerCamera.gameObject.SetActive(true);
        MakeMeNONStatic();
    }

    void OnGUI()
    {
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.normal.textColor = Color.red;
        guiStyle.fontSize = 20;
        float yOffset = 50;
        GUI.Label(new Rect(10, yOffset + 10, 300, 200), "state: " + m_state.ToString(), guiStyle);
        GUI.Label(new Rect(10, yOffset + 30, 300, 200), "moveAmount: " + moveAmount.ToString(), guiStyle);
        GUI.Label(new Rect(10, yOffset + 50, 300, 200), "isGrounded: " + isGrounded.ToString(), guiStyle);
        GUI.Label(new Rect(10, yOffset + 70, 300, 200), "velocity: " + r.linearVelocity.ToString(), guiStyle);
        GUI.Label(new Rect(10, yOffset + 90, 300, 200), "externalForce: " + externalForce.ToString(), guiStyle);
        GUI.Label(new Rect(10, yOffset + 110, 300, 200), "platformVelocity: " + platformVelocity.ToString(), guiStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y + originOffset, transform.position.z),
                                     bounds.extents.x);
    }
}
