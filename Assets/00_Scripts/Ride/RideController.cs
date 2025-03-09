
using System.Collections.Generic;
using UnityEngine;

public class RideController : MonoBehaviour
{
    [SerializeField] RideEngine engine;
    [SerializeField] RideRollHandler rollHandler;


    void Start()
    {
        engine.SetStart();
        engine.OnSteerChanged += rollHandler.SetTargetRoll;
        engine.HoverModule.OnTargetPitch += rollHandler.SetTargetPitch;
        engine.OnAirStatusChanged += rollHandler.OnAirStatusChanged;
    }
    void Update()
    {
        engine.SetInputVector(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")));
    }

    public List<Transform> GetCheckTransforms() => rollHandler.groundCheckTransforms;
}
