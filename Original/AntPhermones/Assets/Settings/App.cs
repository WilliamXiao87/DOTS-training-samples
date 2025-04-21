using System;
using UnityEngine;

namespace Settings
{
    public class App : MonoBehaviour
    {
        private void Start()
        {
            UnityEngine.Application.targetFrameRate = 30;
        }
    }
}