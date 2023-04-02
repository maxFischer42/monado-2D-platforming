using Monado;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParabolicMotion : MonoBehaviour
{

    Vector2 backflip_velocity;
    public Vector2 backflipStrength = new Vector2(2, 3);
    private bool hasStartedFlip = false;

    float v_x0;
    float v_y0;
    float x0;
    float y0;
    float flip_time = 0f;
    public float gravityAcceleration;

    // Update is called once per frame
    void Update()
    {
        if (!hasStartedFlip)
        {
            hasStartedFlip = true;

            v_x0 = backflipStrength.x * -1;
            v_y0 = backflipStrength.y;
            x0 = transform.position.x;
            y0 = transform.position.y;
            flip_time = 0;
        }

        float _x = x0 + (v_x0 * flip_time);
        float _vy = v_y0 - (gravityAcceleration * flip_time);
        float _y = y0 + (v_y0 * flip_time) - (0.5f * gravityAcceleration * Mathf.Pow(flip_time, 2));

        //rb2d.position = new Vector2(_x, _y);
        backflip_velocity = new Vector2(_x, _y);

        flip_time += Time.deltaTime;
        transform.position = backflip_velocity;

        // Vx = Vx0
        // x = x0 +Vx0*t
        //
        // Vy = Vy0 -gt
        // y = y0 +Vy0*t - 1/2*g*t^2
        // V^2y = V^2y0 - 2g(y-y0)
    }
}
