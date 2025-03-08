using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RideEngine : MonoBehaviour
{
    public enum EngineState
    {
        Idle,
        Moving,
    }
    [Header("Dependencies")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform rideTransform;
    private EngineState state = EngineState.Idle;
    [Header("Settings")]
    [SerializeField] float hoverHeight;
    [SerializeField] float hoverResponseDistence = 0.1f;
    [SerializeField] float hoverResponceForce = 5f;
    [SerializeField] float hoverDiminishDistance = 0.2f;
    [SerializeField][Range(0, 1)] float idleHoverForceRate = 0.3f;
    [SerializeField][Range(0, 1)] float hoverDragRate = 0.3f;
    [SerializeField] float maxSpeed;
    [Header("Acceleration")]
    [SerializeField] float maxAcceleration;
    [SerializeField] float maxDeceleration;
    [SerializeField] float idleMaxDeceleration;
    [SerializeField] float maxTurnSpeed;
    [SerializeField] float inputSteerSpeed;
    float _turnSpeed;
    private Vector2 _inputVector = Vector2.zero;
    private float _maxSpeedForwardEnergy = 0;
    public Action<float, float> OnSteerChanged;

    private int _groundLayerMask;

    void Awake()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        _inputVector = Vector2.zero;
        _maxSpeedForwardEnergy = maxSpeed * maxSpeed * rb.mass / 2;
        _groundLayerMask = LayerMask.GetMask("Default");
    }

    void FixedUpdate()
    {

        if (state == EngineState.Moving) Move();
        else if (state == EngineState.Idle) Idle();
        ApplyHover();
    }

    void Move()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        _turnSpeed = Mathf.Lerp(Mathf.Clamp(_turnSpeed + _inputVector.x * inputSteerSpeed, -maxTurnSpeed, maxTurnSpeed), 0, 0.1f);
        rideTransform.RotateAround(rideTransform.position, Vector3.up, _turnSpeed * Time.fixedDeltaTime);
        Vector3 forward = rideTransform.forward;

        float cancellingForce = -Vector3.Dot(vel, rideTransform.right);
        cancellingForce = Mathf.Sign(cancellingForce) * Mathf.Pow(cancellingForce, 2) * rb.mass / 2;
        Vector3 cancelVector = rideTransform.right;
        Vector3 velocityForwardComponent = Vector3.Project(vel, forward);
        float currentForwardEnergy = Mathf.Pow(velocityForwardComponent.magnitude, 2) * rb.mass / 2;
        float accBasedOnInput = _inputVector.y < 0 ? -Mathf.Lerp(0, maxDeceleration, -_inputVector.y) : Mathf.Lerp(0, maxAcceleration, _inputVector.y);
        float realForwardForce = Mathf.Clamp(accBasedOnInput, -maxDeceleration, _maxSpeedForwardEnergy - currentForwardEnergy);
        debugForwardForce = forward * realForwardForce;
        debugCancelForce = cancelVector * cancellingForce;
        debugDragForce = GetDragForce();

        BroadcastSteerVelocity(cancellingForce);

        Vector3 forceVector = forward * realForwardForce + cancelVector * cancellingForce;

        rb.AddForce(forceVector + GetDragForce(), ForceMode.Force);
    }

    void Idle()
    {
        rb.AddForce(GetDragForce(), ForceMode.Force);
    }
    void ApplyHover()
    {
        float verticalDrag = 0;
        Vector3 hoverForce = Vector3.zero;
        RaycastHit groundRay = new RaycastHit();
        if (Physics.Raycast(transform.position, Vector3.down, out groundRay, hoverHeight * 2, _groundLayerMask))
        {
            float verticalSpeed = rb.velocity.y;
            if (verticalSpeed > .04f)
                verticalDrag = -verticalSpeed * Mathf.Abs(verticalSpeed) * rb.mass / 2 * hoverDragRate;
            hoverForce = CalculateHoverForce(groundRay.distance);
        }
        Vector3 drag = verticalDrag * Vector3.up;
        Vector3 defaultHoverForce = -Physics.gravity.y * rb.mass * idleHoverForceRate * Vector3.up;
        debugHoverForce = hoverForce + defaultHoverForce;
        rb.AddForce(hoverForce + defaultHoverForce + drag, ForceMode.Force);
    }

    private Vector3 CalculateHoverForce(float floorDistance)
    {
        Debug.Log("Floor Distance: " + floorDistance);
        float g = -Physics.gravity.y;
        float hoverResponseAmount = Mathf.Pow(Mathf.Clamp01((hoverHeight - floorDistance) / hoverResponseDistence), 2);
        float hoverDiminishAmount = Mathf.Pow(Mathf.Clamp01(1 - (floorDistance - hoverHeight) / hoverDiminishDistance), 2);
        float hoverResponse = hoverResponseAmount * hoverResponceForce;
        float hoverDiminish = hoverDiminishAmount * g * (1 - idleHoverForceRate);

        return Vector3.up * (hoverResponse + hoverDiminish) * rb.mass;
    }

    public Vector3 GetDragForce()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 forceVector = -vel.normalized;
        float k = Mathf.Pow(vel.magnitude, 2) * rb.mass / 2;
        float force = Mathf.Clamp(k, 0, idleMaxDeceleration);
        return forceVector * force;
    }

    public void SetInputVector(Vector2 inputVector)
    {
        _inputVector = inputVector;
    }

    public void SetStart()
    {
        state = EngineState.Moving;
    }

    private Vector3 debugForwardForce;
    private Vector3 debugCancelForce;
    private Vector3 debugDragForce;
    private Vector3 debugHoverForce;

    public void OnDrawGizmos()
    {
        if (state == EngineState.Moving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + rb.velocity);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + debugForwardForce);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + debugCancelForce);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + debugDragForce);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + debugHoverForce / rb.mass);
        }
    }

    private void BroadcastSteerVelocity(float cancellingForce)
    {
        OnSteerChanged?.Invoke(_turnSpeed, cancellingForce / rb.mass);
    }
}
