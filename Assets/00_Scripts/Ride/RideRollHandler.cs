using System.Collections.Generic;
using UnityEngine;

public class RideRollHandler : MonoBehaviour
{
    [SerializeField] Transform rollingTransform;
    [SerializeField] Transform pitchTransform;
    [SerializeField] float maxRollAngle = 60f;
    [SerializeField] float forceRollMultiplier = 5f;

    [SerializeField] Vector2 pitchAngleLimits = new Vector2(-30f, 45f);
    [SerializeField] float pitchMaxDelta = 5f;
    public List<Transform> groundCheckTransforms;
    private float pitchC = 1f;
    private float pitchAirC = 0.2f;
    private float pitchGroundC = 1f;

    private float _targetRoll = 0f;

    float _targetPitch = 0f;
    public void SetTargetRoll(float _turnSpeed, float cancelForceOverMass)
    {
        _targetRoll = Mathf.Clamp(-cancelForceOverMass * forceRollMultiplier, -maxRollAngle, maxRollAngle);
    }

    public void SetTargetPitch(float pitch)
    {
        _targetPitch = pitch;
    }

    public void OnAirStatusChanged(bool isAirborne)
    {
        if (isAirborne) pitchC = pitchAirC;
        else pitchC = pitchGroundC;
    }

    // Update is called once per frame
    void Update()
    {
        Pitch();
        Roll();
    }
    void Roll()
    {
        float currentRoll = rollingTransform.localEulerAngles.z;
        if (currentRoll > 180) currentRoll -= 360f;
        float newRoll = Mathf.Lerp(currentRoll, _targetRoll, 0.1f);
        if (newRoll < 0) newRoll += 360f;
        rollingTransform.localEulerAngles = new Vector3(rollingTransform.localEulerAngles.x, rollingTransform.localEulerAngles.y, newRoll);
    }

    void Pitch()
    {
        float currentPitch = pitchTransform.localEulerAngles.x;
        if (currentPitch > 180) currentPitch -= 360f;
        float targetPitchAngle = Mathf.Clamp(-Mathf.Atan2(_targetPitch, 1f) * Mathf.Rad2Deg, pitchAngleLimits.x, pitchAngleLimits.y);
        float pitchLerpC = targetPitchAngle > currentPitch ? 0.04f : 0.12f;
        float newPitch = Mathf.Lerp(currentPitch, targetPitchAngle, pitchC * pitchLerpC);
        //rollingTransform.localEulerAngles = new Vector3(rollingTransform.localEulerAngles.x + pitchDelta, rollingTransform.localEulerAngles.y, rollingTransform.localEulerAngles.z);
        pitchTransform.localEulerAngles = new Vector3(newPitch, pitchTransform.localEulerAngles.y, pitchTransform.localEulerAngles.z);
    }


    void OnDrawGizmos()
    {
        foreach (Transform item in groundCheckTransforms)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(item.position, item.position + Vector3.down * .2f);
        }
    }

}
