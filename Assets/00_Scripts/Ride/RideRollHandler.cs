using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RideRollHandler : MonoBehaviour
{
    [SerializeField] Transform rollingTransform;
    [SerializeField] float maxRollAngle = 60f;
    [SerializeField] float forceRollMultiplier = 5f;

    private float _targetRoll = 0f;
    public void SetTargetRoll(float _turnSpeed, float cancelForceOverMass)
    {
        _targetRoll = Mathf.Clamp(-cancelForceOverMass * forceRollMultiplier, -maxRollAngle, maxRollAngle);
    }

    // Update is called once per frame
    void Update()
    {
        float currentRoll = rollingTransform.localEulerAngles.z;
        if (currentRoll > 180) currentRoll -= 360f;
        float newRoll = Mathf.Lerp(currentRoll, _targetRoll, 0.1f);
        if (newRoll < 0) newRoll += 360f;
        rollingTransform.localEulerAngles = new Vector3(0, 0, newRoll);
    }
}
