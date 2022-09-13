using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    [SerializeField] int targetFrameRate = 60;
    private void Start()
    {
        Application.targetFrameRate = targetFrameRate;
    }
}
