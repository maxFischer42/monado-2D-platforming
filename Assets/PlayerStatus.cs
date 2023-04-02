using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    public float max_stamina;
    private float current_stamina;
    public float stamina_recovery_amount;
    public Sprite green_stamina;
    public Sprite red_stamina;
    public float stamina { 
        get { return current_stamina; }
        set { current_stamina = value; }
    }
    public bool stamina_cooldown = false;
    private bool halt_stamina;
    private float halt_stamina_counter;
    private float halt_stamina_counter_max = 2.5f;

    public Image stamina_wheel;

    private void Start()
    {
        stamina = max_stamina;
    }

    public void Update()
    {
        UpdateStamina();   
    }

    public void UpdateStamina()
    {
        float ratio = stamina / max_stamina;

        
        if(halt_stamina)
        {
            stamina_wheel.color = new Color(1, 0, 0, Mathf.Sin(Time.deltaTime));
            halt_stamina_counter += Time.deltaTime;
            if(halt_stamina_counter >= halt_stamina_counter_max)
            {
                halt_stamina_counter = 0;
                halt_stamina = false;
            }
        }
        else if (stamina_cooldown)
        {
            stamina_wheel.color = Color.white;
            if (stamina >= max_stamina) stamina_cooldown = false;
            else
            {
                stamina += stamina_recovery_amount;
                stamina_wheel.fillAmount = ratio;
            }
        }
        else
        {
            if (stamina <= 0) { stamina_cooldown = true; halt_stamina = true; stamina_wheel.sprite = red_stamina; stamina_wheel.fillAmount = 0; return; }
            if (ratio <= 0.25) stamina_wheel.sprite = red_stamina;
            else stamina_wheel.sprite = green_stamina;
            bool isActive = true;
            if (ratio >= 1) isActive = false;
            stamina_wheel.transform.parent.gameObject.SetActive(isActive);
            stamina_wheel.fillAmount = ratio;
            if(ratio < 1)
            {
                stamina += stamina_recovery_amount;
            }
        }
    }
}
