using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Monado
{
    public class PlayerController : ControllerCore
    {
        public Particles myParticles;
        [Header("Stat Information")]
        public PlayerStatus ps;
        public float run_cost;
        public float wall_jump_cost;

        public override void OnWallDragStart()
        {
            myParticles.wallSlideParticles.Play();
        }

        public override void OnWallDragEnd()
        {
            myParticles.wallSlideParticles.Stop();
        }

        public override void OnJumpLaunch()
        {
            Vector3 pos = transform.position;// + (Vector3.up * collider_MAIN.size.y / 2);
            GameObject effect = (GameObject)Instantiate(myParticles.jumpEffectPrefab, pos, Quaternion.identity);
            Destroy(effect, 0.6f);
        }

        public override void OnWallJumpLaunch(Vector2 pos, Vector2 dir)
        {
            GameObject effect = (GameObject)Instantiate(myParticles.wallJumpEffect, pos, Quaternion.FromToRotation(pos, pos + dir));
            Destroy(effect, 0.5f);
            ps.didChangeLastFrame = true;
            ps.CancelResetStamina();
            ps.ResetStaminaCooldown();
        }

        public override void OnTurnStart()
        {
            Debug.Log(previousVelocityX);
            if (doSprint && isGrounded && Mathf.Abs(previousVelocityX) > 0) myParticles.skidParticles.Play();
        }
        public override void OnTurnEnd()
        {
            myParticles.skidParticles.Stop();
        }

        public override void SyncLateUpdate()
        {
            if (isGrounded && input.isRunning && !myParticles.runParticles.isPlaying && Mathf.Abs(input.xMovement) > groundMoveSpeed && !isCrouch && doSprint) myParticles.runParticles.Play();
            else if ((!doSprint || !isGrounded || Mathf.Abs(input.xMovement) <= groundMoveSpeed) && myParticles.runParticles.isPlaying) { myParticles.runParticles.Stop(); myParticles.skidParticles.Stop();  }
            if (!isGrounded && myParticles.skidParticles.isPlaying) myParticles.skidParticles.Stop();
        }

        public override bool canWallJump()
        {
            if (ps.stamina_cooldown) return false;
            float t = ps.stamina - wall_jump_cost;
            ps.stamina = t;
            return true;
        }

        public override bool canSprint()
        {
            if (ps.stamina_cooldown) return false;
            float t = ps.stamina - run_cost;
            ps.stamina = t;
            ps.didChangeLastFrame = true;
            ps.CancelResetStamina();
            return true;
        }
    }
}