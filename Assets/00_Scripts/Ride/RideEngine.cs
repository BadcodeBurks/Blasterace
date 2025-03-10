using System;
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
    [SerializeField] RideController controller;
    private EngineState state = EngineState.Idle;
    [Header("Hover")]
    [SerializeField] RideEngineHoverModule hoverModule;
    public RideEngineHoverModule HoverModule => hoverModule;
    [Header("Acceleration")]
    [SerializeField] float maxSpeed;
    [SerializeField] float maxAcceleration;
    [SerializeField] float maxDeceleration;
    [SerializeField] float idleMaxDeceleration;
    [SerializeField] AnimationCurve speedAccelerationFactor;

    [Header("Steering")]
    [SerializeField] float maxTurnSpeed;
    [SerializeField] float inputSteerSpeed;
    [SerializeField] AnimationCurve steerCancellingForceCurve;
    [SerializeField][Range(1, 5)] float steerCancelForce;
    [SerializeField] AnimationCurve cancelForceForwardingCurve;
    [SerializeField][Range(0, 5)] float cancelForceForwardAmount;
    float _turnSpeed;
    private Vector2 _inputVector = Vector2.zero;
    private float _maxSpeedForwardEnergy = 0;
    public Action<float, float> OnSteerChanged;
    private bool _onAir;
    public Action<bool> OnAirStatusChanged;


    void Awake()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        _inputVector = Vector2.zero;
        _maxSpeedForwardEnergy = maxSpeed * maxSpeed * rb.mass / 2;
        hoverModule.Init(rb, controller.GetCheckTransforms()[1]);
    }

    void FixedUpdate()
    {
        if (state == EngineState.Moving)
        {
            Move();
            hoverModule.ApplyHover();
        }
        else if (state == EngineState.Idle) Idle();
        HandleAirStatus();
    }

    void HandleAirStatus()
    {
        bool t = _onAir;
        _onAir = hoverModule.groundChecker.AvgDist > hoverModule.TotalOnGroundDistance;
        if (t != _onAir) OnAirStatusChanged?.Invoke(_onAir);
    }

    void Move()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float turnSpeedClamped = Mathf.Clamp(maxTurnSpeed, 1f, maxTurnSpeed * Mathf.Clamp01(vel.magnitude / .2f + .05f));
        _turnSpeed = Mathf.Lerp(Mathf.Clamp(_turnSpeed + _inputVector.x * inputSteerSpeed, -turnSpeedClamped, turnSpeedClamped), 0, 0.1f);
        rideTransform.RotateAround(rideTransform.position, Vector3.up, _turnSpeed * Time.fixedDeltaTime);
        Vector3 forward = rideTransform.forward;

        float cancellingForce = -Vector3.Dot(vel, rideTransform.right);
        cancellingForce = Mathf.Sign(cancellingForce) * Mathf.Pow(cancellingForce, 2) * rb.mass / 2;

        Vector3 cancelVector = rideTransform.right;
        Vector3 velocityForwardComponent = Vector3.Project(vel, forward);
        float currentForwardEnergy = Mathf.Pow(velocityForwardComponent.magnitude, 2) * rb.mass / 2;
        float accBasedOnInput = _inputVector.y < 0 ? -Mathf.Lerp(0, maxDeceleration, -_inputVector.y) : Mathf.Lerp(0, maxAcceleration * speedAccelerationFactor.Evaluate(velocityForwardComponent.magnitude / maxSpeed), _inputVector.y);
        float realForwardForce = Mathf.Clamp(accBasedOnInput, -maxDeceleration, _maxSpeedForwardEnergy - currentForwardEnergy);
        debugForwardForce = forward * realForwardForce;
        debugCancelForce = cancelVector * cancellingForce * steerCancelForce * steerCancellingForceCurve.Evaluate(Mathf.Abs(_turnSpeed) / maxTurnSpeed);
        debugDragForce = GetDragForce();

        float cancelForceMultiplier = steerCancelForce * steerCancellingForceCurve.Evaluate(Mathf.Abs(_turnSpeed) / maxTurnSpeed);
        cancellingForce = cancellingForce * cancelForceMultiplier;
        BroadcastSteerVelocity(cancellingForce);
        float forwardedCancelForce = _onAir ? 0 : Mathf.Abs(cancellingForce) * cancelForceForwardAmount * cancelForceForwardingCurve.Evaluate(Mathf.Abs(_turnSpeed) / maxTurnSpeed);

        Vector3 forceVector = forward * (realForwardForce + forwardedCancelForce) + cancelVector * cancellingForce;
        rb.AddForce(forceVector + GetDragForce(), ForceMode.Force);
    }

    void Idle()
    {
        rb.AddForce(GetDragForce(), ForceMode.Force);
    }

    public Vector3 GetDragForce()
    {
        Vector3 vel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 forceVector = -vel.normalized;
        float k = Mathf.Pow(vel.magnitude, 2) * rb.mass / 2;
        float dragForce = Mathf.Clamp(k, 0, idleMaxDeceleration * vel.magnitude / 2);

        return forceVector * dragForce;
    }

    public void SetInputVector(Vector2 inputVector)
    {
        _inputVector = inputVector;
    }

    public void SetStart()
    {
        state = EngineState.Moving;
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Default") && state == EngineState.Moving)
        {
            float contactForce = collision.GetContact(0).impulse.magnitude;
            float contactAngle = Vector3.Angle(collision.GetContact(0).normal, Vector3.up);
            bool isCollisionVerticalOrWeak = contactForce < 6 * rb.mass;
            if (isCollisionVerticalOrWeak) return;
            isCollisionVerticalOrWeak = contactAngle < 45f;
            if (isCollisionVerticalOrWeak) return;
            //TODO: Invoke OnHit
            state = EngineState.Idle;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddForceAtPosition(collision.GetContact(0).normal * rb.velocity.magnitude * rb.mass * .4f, collision.GetContact(0).point, ForceMode.Impulse);

        }
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
            hoverModule.OnDrawGizmos();
        }
    }

    private void BroadcastSteerVelocity(float cancellingForce)
    {
        OnSteerChanged?.Invoke(_turnSpeed, cancellingForce / rb.mass);
    }
}
