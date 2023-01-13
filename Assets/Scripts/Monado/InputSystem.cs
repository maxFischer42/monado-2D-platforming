using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monado
{
    public class InputSystem : MonoBehaviour
    {
        public float xMovement;
        public float yMovement;
        public float analogXMovement;
        public float analogYMovement;
        public bool jumpStart;
        public bool jumpHold;
        public bool jumpRelease;
        public bool isRunning;

        public void Check()
        {
            xMovement = Input.GetAxis("Horizontal");
            analogXMovement = Input.GetAxisRaw("Horizontal");
            jumpStart = Input.GetButtonDown("Jump");
            jumpHold = Input.GetButton("Jump");
            jumpRelease = Input.GetButtonUp("Jump");
            isRunning = Input.GetButton("Run");
        }
    }

}