using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RideEngineHoverModule
{
    public GroundChecker groundChecker;
    [SerializeField] float hoverHeight;
    [SerializeField] float hoverResponseDistence = 0.1f;
    [SerializeField] float hoverResponceForce = 5f;
    [SerializeField] float hoverDiminishDistance = 0.2f;
    [SerializeField][Range(0, 1)] float idleHoverForceRate = 0.3f;
    [SerializeField][Range(1, 5)] float hoverDragRate = 0.3f;

    public Action<float> OnTargetPitch;

    private Rigidbody _rb;
    private Transform _checkTransform;

    public float TotalOnGroundDistance => hoverHeight + hoverDiminishDistance;

    public void Init(Rigidbody rb, Transform checkTransform)
    {
        _rb = rb;
        _checkTransform = checkTransform;
        groundChecker.Init();
    }

    private void DoRayChecks()
    {
        groundChecker.Check(_rb.position, _checkTransform.forward);
        OnTargetPitch?.Invoke(groundChecker.Angles.y);
    }

    public void ApplyHover()
    {
        DoRayChecks();
        Vector3 defaultHoverForce = -Physics.gravity.y * _rb.mass * idleHoverForceRate * Vector3.up;
        Vector3 hoverForce = defaultHoverForce;
        if (groundChecker.AvgDist < hoverHeight + hoverDiminishDistance && groundChecker.Angles.magnitude < .5f)
        {
            hoverForce += CalculateHoverForce(groundChecker.AvgDist);
        }
        _rb.AddForce(hoverForce, ForceMode.Force);
    }

    private Vector3 CalculateHoverForce(float floorDistance)
    {
        float g = -Physics.gravity.y;
        float hoverResponseAmount = Mathf.Pow(Mathf.Clamp01((hoverHeight - floorDistance) / hoverResponseDistence), 2);
        float hoverDiminishAmount = Mathf.Pow(Mathf.Clamp01(1 - (floorDistance - hoverHeight) / hoverDiminishDistance), 2);
        float hoverResponse = hoverResponseAmount * hoverResponceForce;
        float hoverDiminish = hoverDiminishAmount * g * (1 - idleHoverForceRate);

        return Vector3.up * (hoverResponse + hoverDiminish) * _rb.mass + GetSpeedDampForce(Mathf.Clamp(Mathf.Abs(floorDistance - hoverHeight), 0, hoverDiminishDistance));
    }

    private Vector3 GetSpeedDampForce(float desiredDistance)
    {
        float verticalSpeed = _rb.velocity.y;
        float speedEnergy = verticalSpeed * Mathf.Abs(verticalSpeed) * _rb.mass / 2;
        return new Vector3(0, -speedEnergy, 0) * hoverDragRate / Mathf.Max(Mathf.Clamp01(desiredDistance / hoverDiminishDistance), 0.1f);
    }

    public void OnDrawGizmos()
    {
        groundChecker.OnDrawGizmos();
    }
}
