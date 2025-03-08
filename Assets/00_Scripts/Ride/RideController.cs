
using UnityEngine;

public class RideController : MonoBehaviour
{
    [SerializeField] RideEngine engine;
    [SerializeField] RideRollHandler rollHandler;


    void Start()
    {
        engine.SetStart();
        engine.OnSteerChanged += rollHandler.SetTargetRoll;
    }
    void Update()
    {
        engine.SetInputVector(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")));
    }
}
