using Monado;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Monado
{

    [RequireComponent(typeof(InputSystem))]

    public class ControllerCore : MonoBehaviour
    {
        [HideInInspector] public InputSystem input;        
        [HideInInspector] public BoxCollider2D collider_MAIN;
        private Rigidbody2D rb2d;
        public Animator anim;

        public JumpState jumpState = JumpState.Falling;
        [Header("Physics")]
        public Vector2 velocity;
        public float groundMoveSpeed = 1.0f;
        public float groundRunSpeed = 1.0f;
        public float airMoveSpeed = 0.5f;
        public float slopeCheckDistance = 0.4f;
        public bool isOnSlope = false;
        public float angle;
        public float maxAngle = 1.9f;
        public float playerSkinWidth = 0.45f;
        public float pushUpDetectionSize = 0.9f;
        public bool isGrounded = false;


        [Header("Jumping and Gravity")]
        public float groundCheckDistance = 0.1f;
        public LayerMask groundLayerMask;
        public float gravityAcceleration = 0.09f;
        public Vector2 verticalMaxVelocity = new Vector2(-25, 25);
        public float pushFromDistance = 0.001f;
        public float initialJumpVelocity;
        public float additionalJumpVelocity;
        public float timeToFullJump = 0.35f;
        public float wallSlideSpeed = 0.1f;
        public Vector2 initialWallJumpVelocity = Vector2.up;
        public Vector2 initialSprintJumpVelocity = Vector2.up;
        public float jumpCooldown = 0.6f;
        public float effectRotationMultiplier = 2.5f;
        private bool wasSprintJump;

        public int previousDirection = 1;
        public int direction = 1;

        private Vector3 previous_position;

        private float wallSlideInitialDirection = 0f;

        public bool canJump = true;

        private bool wasJumping = false;
        private bool jumpDown = false;
        private bool lockJump = false;

        public float jumpFrames = 5;
        private float currentFrames = 0;

        [HideInInspector] public float previousVelocityX = 0f;

        // Called when the GameObject is enabled
        public void Awake()
        {
            
        }

        // Called when the scene initiates
        public void Start()
        {
            input = GetComponent<InputSystem>();
            collider_MAIN = GetComponent<BoxCollider2D>();
            rb2d= GetComponent<Rigidbody2D>();
        }

        // Called first, all physics calculations are handled here
        public void FixedUpdate()
        {
            input.Check();
            if (input.jumpHold && !lockJump && !jumpDown)
            {
                currentFrames = 0;
                jumpDown = true;
            }
            if (jumpDown && input.jumpHold)
            {
                currentFrames++;
                if (jumpFrames <= currentFrames)
                {
                    currentFrames = 0;
                    lockJump = true;
                    jumpDown = false;
                }
            }
            if (lockJump && !input.jumpHold)
            {
                lockJump = false;
            }
            if(!input.jumpHold)
            {
                jumpDown = false;
            }


            if (isGrounded && jumpState == JumpState.Falling)
            {
                //velocity = Vector2.zero;
                WallStuckCheck();

            }
            HandleJumpState();
                //isGrounded = doGrounded();
                Move();
        }
        
        // Called second, handle all non-time-dependant calculations here
        public void Update()
        {
            isGrounded = doGrounded();
        }

        // Called third, handle any checking functions here
        public void LateUpdate()
        {
            //input.Check();
            previous_position = transform.position;
            SyncLateUpdate();
        }

        // Physics-Related
        // Check if a collider is found at a given raycast
        public bool DetectCollisionAtDistance(Vector2 origin, Vector2 direction, float distance, LayerMask layers, out RaycastHit2D ray)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layers);
            ray = hit;
            return hit.collider;
        }

        // Return the world position of the center-bottom of the controller's collider
        Vector2 getFeetPos()
        {
            float h = collider_MAIN.size.y / 2;
            return (Vector2)transform.position - (Vector2.up * h);
        }

        // Check if the controller is close enough to collide with the ground
        void CheckGroundedState()
        {
            RaycastHit2D hitC = new RaycastHit2D(), hitL = new RaycastHit2D(), hitR = new RaycastHit2D();
            bool isG = DetectCollisionAtDistance(getFeetPos() + (Vector2.up / 2), Vector2.down, groundCheckDistance, groundLayerMask, out hitC) ||
                DetectCollisionAtDistance(getFeetPos() + (Vector2.up / 2) + Vector2.right * collider_MAIN.size.x/2, Vector2.down, groundCheckDistance, groundLayerMask, out hitL) ||
                DetectCollisionAtDistance(getFeetPos() + (Vector2.up / 2) - Vector2.right * collider_MAIN.size.x/2, Vector2.down, groundCheckDistance, groundLayerMask, out hitR);

            Debug.DrawLine(getFeetPos(), getFeetPos() + Vector2.down * groundCheckDistance);
            Debug.DrawLine(getFeetPos() + Vector2.right * collider_MAIN.size.x / 2, getFeetPos() + Vector2.down + (Vector2.right * collider_MAIN.size.x / 2) * groundCheckDistance);
            Debug.DrawLine(getFeetPos() - Vector2.right * collider_MAIN.size.x / 2, getFeetPos() + Vector2.down - (Vector2.right * collider_MAIN.size.x / 2) * groundCheckDistance);

            // Check if collision point is within our selected angle range
            if(isG)
            {
                RaycastHit2D selectedRay = new RaycastHit2D();
                if (!hitC.collider)
                {
                    if (hitL.collider) { selectedRay = hitL; }
                    else { selectedRay = hitR; }

                    float angle = Vector2.SignedAngle(Vector2.down, selectedRay.normal);
                    angle = angle * Mathf.Deg2Rad;
                    if (Mathf.Abs(angle) <= 1.65f) isG = false;
                } else
                {
                    selectedRay = hitC;
                }
            }

            anim.SetBool("_isGrounded", isG);
            isGrounded = isG;
            if (isGrounded && (jumpState == JumpState.Falling || jumpState == JumpState.Sliding))
            {
                if (jumpState == JumpState.Sliding) {
                    anim.SetBool("_isSliding", false);
                    OnWallDragEnd();
                }
                velocity = Vector2.zero;
                jumpState = JumpState.Landing;
            } else if(!isGrounded)
            {
                if (jumpState == JumpState.Sliding && input.analogXMovement == wallSlideInitialDirection) return;
                if (jumpState == JumpState.Sliding) {
                    anim.SetBool("_isSliding", false);
                    OnWallDragEnd();
                }
                jumpState = JumpState.Falling;
            }
        }
        void PushOutFromGeometry()
        {
            print("foo");
            if (isOnSlope) return;
            // Check if the collider is overlapping with the ground
            // - Create an overlap circle between the center and feet positions of the collider
            // - Check if it overlaps the ground collision
            // - If so, push the controller up until it no longer is colliding

            //bool isOverlapping = true;
            //while(isOverlapping)
            //{
                Collider2D p = Physics2D.OverlapBox(transform.position, collider_MAIN.size * pushUpDetectionSize, 0f, groundLayerMask);
                
                if (p != null)
                {

                    transform.position += (Vector3.up * pushFromDistance);
                }
            //    else if(p == null) { isOverlapping = false; }
            //}
        }
        public Vector3 sidePushSize;
        void WallStuckCheck()
        {
            print("tttt");
            Collider2D L = Physics2D.OverlapBox(transform.position - (Vector3.right * collider_MAIN.size.x * pushUpDetectionSize),  sidePushSize, 0f, groundLayerMask);
            Collider2D R = Physics2D.OverlapBox(transform.position + (Vector3.right * collider_MAIN.size.x * pushUpDetectionSize),  sidePushSize, 0f, groundLayerMask);
            Debug.Log("L" + L);
            Debug.Log("R" + R);
            bool T = false;
            float dir = 0;
            Collider2D cc = new Collider2D();

            if (L != null && R == null) { 
                T = true;
                dir = 1;
                cc = L;
            }
            else if (L == null && R != null) { 
                T = true;
                dir = -1;
                cc = R;
            } else if (L != null && R != null)
            {
                if(L.tag == "Passable")
                {
                    T = true;
                    dir = -1;
                    cc = R;
                } else if(R.tag == "Passable")
                {
                    T = true;
                    dir = 1;
                    cc = L;
                }
            }

            if(T)
            {
                if(cc.tag != "Passable") transform.position += Vector3.right * pushFromDistance * dir;
            }
        }

        void PerformGroundMovement()
        {
            RaycastHit2D hit;
            bool isGrounded = DetectCollisionAtDistance(transform.position + (Vector3.up * 0.01f), Vector2.down, slopeCheckDistance, groundLayerMask, out hit) ||
                DetectCollisionAtDistance(getFeetPos() + Vector2.right * collider_MAIN.size.x / 2, Vector2.down, groundCheckDistance, groundLayerMask, out hit) ||
                DetectCollisionAtDistance(getFeetPos() - Vector2.right * collider_MAIN.size.x / 2, Vector2.down, groundCheckDistance, groundLayerMask, out hit);
            // Get the angle of the collision
            angle = Vector2.SignedAngle(Vector2.down, hit.normal);
            angle = angle * Mathf.Deg2Rad;
            print(Mathf.Abs(angle));
            if (!isTurning)
            {
                if (Mathf.Abs(angle) > 1.65f)
                {
                    bool isRunning = input.isRunning;
                    anim.SetBool("_isRunning", isRunning);
                    float speed = (isRunning ? groundRunSpeed : groundMoveSpeed);
                    isOnSlope = (angle != Mathf.PI) && (angle != 0);
                    float _x = Mathf.Cos(angle) * speed * input.xMovement * -1;
                    float _y = Mathf.Sin(angle) * speed * input.xMovement * -1;                    
                    
                    if(!isTurning) velocity = new Vector2(_x, _y);


                    CheckDirection();

                    PushOutFromGeometry();
                    if (jumpDown) jumpState = JumpState.PrepareJump;
                }
            } else
            {
                HandleTurn();
                velocity = Vector2.zero;
            }

            
        }

        void PerformAirMovement()
        {
            float _x = airMoveSpeed * input.xMovement;
            if (wasSprintJump) _x = _x / 2;
            velocity.x += _x;
            PushOutFromGeometry();
        }

        void PerformWallJump()
        {
            StartCoroutine(doJumpCooldown());
            wasSprintJump = false;
            anim.SetBool("_isSliding", false);
            OnWallDragEnd();
            velocity.y = initialWallJumpVelocity.y;
            velocity.x = initialWallJumpVelocity.x * -1 * wallSlideInitialDirection;
            jumpTimer = 1f;
            anim.SetTrigger("_jumpStart");
            direction = (int)wallSlideInitialDirection * -1;
            previousDirection = direction;
            jumpState = JumpState.Jumping;
            Flip();
            OnWallJumpLaunch(transform.position, transform.position + new Vector3(initialWallJumpVelocity.x * -1 * wallSlideInitialDirection * effectRotationMultiplier, initialWallJumpVelocity.y * effectRotationMultiplier));
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void HandleJumpGrounded()
        {
            CheckGroundedState();
            if (jumpState != JumpState.Grounded) return;
            PerformGroundMovement();
        }
        void HandleJumpPrepare()
        {
            if (!canJump) return;
            isOnSlope = false;
            isGrounded = false;
            StartCoroutine(doJumpCooldown());
            velocity.y = velocity.y + initialJumpVelocity;
            if (Mathf.Abs(velocity.x) > 0f && input.isRunning) {
                wasSprintJump = true;
                velocity.x *= initialSprintJumpVelocity.x;
            }
            jumpState = JumpState.Jumping;
            jumpTimer = 1f;
            anim.SetTrigger("_jumpStart");
            OnJumpLaunch();
        }

        private float jumpTimer = 1f;

        void HandleJumpJumping()
        {

            //CheckGroundedState();
            if (input.jumpHold && jumpTimer > timeToFullJump)
            {
                velocity.y += additionalJumpVelocity / jumpTimer;
                jumpTimer -= Time.deltaTime;
            }
            PerformAirMovement();
            if (!wasSprintJump)
            {
                velocity.x *= 0.89f;
            }
            velocity.y -= gravityAcceleration;
            if (velocity.y < 0) jumpState = JumpState.Falling;

        }
        void HandleJumpFalling()
        {
            CheckGroundedState();
            //WallStuckCheck(
            if (isGrounded) return;
            velocity.y -= gravityAcceleration;
            if (velocity.y < verticalMaxVelocity.x) velocity.y = verticalMaxVelocity.x;
        }
        void HandleJumpHanging()
        {

        }
        void HandleJumpSliding()
        {            
            CheckGroundedState();
            velocity.x = 0f;
            velocity.y = -wallSlideSpeed;
            if (jumpDown && canJump) PerformWallJump();
        }
        void HandleJumpLand()
        {
            wasSprintJump = false;
            velocity = Vector2.zero;
            jumpState = JumpState.Grounded;
        }

        void HandleJumpState()
        {
            Debug.Log(jumpState);
            switch(jumpState)
            {
                case JumpState.Grounded:
                    HandleJumpGrounded();
                    break;
                case JumpState.PrepareJump:
                    HandleJumpPrepare();
                    break;
                case JumpState.Jumping:
                    HandleJumpJumping();
                    break;
                case JumpState.Falling:
                    HandleJumpFalling();
                    break;
                case JumpState.Hanging:
                    HandleJumpHanging();
                    break;
                case JumpState.Sliding:
                    HandleJumpSliding();
                    break;
                case JumpState.Landing:
                    HandleJumpLand();
                    break;
                default:
                    break;
            }
        }

        public void CheckDirection()
        {
            previousDirection = direction;
            direction = (int)input.analogXMovement;
            if (direction == 0) direction = previousDirection;
            if (previousDirection != direction)
            {
                Flip();
                tTurnTime = turnTime;
                isTurning = true;
                anim.SetBool("_turn", true);
                OnTurnStart();
                HandleTurn();
            }
        }

        public bool doGrounded()
        {
            RaycastHit2D hit = new RaycastHit2D();
            RaycastHit2D hit2 = new RaycastHit2D();
            RaycastHit2D hit3 = new RaycastHit2D();

            bool check = DetectCollisionAtDistance(transform.position + (Vector3.up * 0.01f), Vector2.down, slopeCheckDistance, groundLayerMask, out hit) ||
                DetectCollisionAtDistance(getFeetPos() + Vector2.right * collider_MAIN.size.x / 2, Vector2.down, groundCheckDistance, groundLayerMask, out hit2) ||
                DetectCollisionAtDistance(getFeetPos() - Vector2.right * collider_MAIN.size.x / 2, Vector2.down, groundCheckDistance, groundLayerMask, out hit3);
            return hit.collider || hit2.collider || hit3.collider;
        } 
        
        

        private bool isTurning = false;
        public float turnTime = 0.1f;
        private float tTurnTime;

        void HandleTurn()
        {
            tTurnTime -= Time.deltaTime;
            if(tTurnTime <= 0 || !isGrounded)
            {
                anim.SetBool("_turn", false);
                isTurning = false;
                
                previousDirection = direction;
                if (!isGrounded) anim.SetBool("_isGrounded", false);
                OnTurnEnd();
            }
        }

        public void Flip()
        {
            bool dir = false;
            if (direction == -1) dir = true;
            anim.GetComponent<SpriteRenderer>().flipX = dir;
        }

        public bool CeilingCheck()
        {
            RaycastHit2D hitC = new RaycastHit2D();
            RaycastHit2D hitL = new RaycastHit2D();
            RaycastHit2D hitR = new RaycastHit2D();
            Vector3 xOffset = Vector2.right * (collider_MAIN.size.x / 2);
            Vector3 yOffset = Vector3.up * (collider_MAIN.size.y / 2);
            hitC = Physics2D.Raycast(transform.position + yOffset, Vector2.up, playerSkinWidth, groundLayerMask);
            hitL = Physics2D.Raycast(transform.position + yOffset - xOffset, Vector2.up, playerSkinWidth, groundLayerMask);
            hitR = Physics2D.Raycast(transform.position + yOffset + xOffset, Vector2.up, playerSkinWidth, groundLayerMask);

            RaycastHit2D hit = new RaycastHit2D();
            if (hitL.collider) hit = hitL; else if (hitR.collider) hit = hitR; else hit = hitC;
            if (hit && jumpState != JumpState.Falling) { if (hit.collider.tag != "Passable") { jumpState = JumpState.Falling; velocity.y = 0f; OnWallDragEnd(); } }
            return hit.collider;
        }

        public void Move()
        {
            // Check if theres a wall next to us
            int sign = (int)Mathf.Sign(velocity.x);
            if (velocity.x == 0) { sign = (int)Mathf.Sign(previousVelocityX); } else
            {
                previousVelocityX = velocity.x;
            }

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * sign, playerSkinWidth, groundLayerMask);
            Debug.DrawLine(transform.position, transform.position + Vector3.right * sign * playerSkinWidth);
            // Apply the translation
            if (hit && hit.collider.tag != "Passable")
            {
                velocity.x = 0f;
                if (jumpState == JumpState.Falling && input.analogXMovement == sign)
                {
                    wallSlideInitialDirection = sign;
                    jumpState = JumpState.Sliding;
                    canJump = true;
                    anim.SetBool("_isSliding", true);
                    OnWallDragStart();
                }
            }
            if (jumpState == JumpState.Sliding)
            {
                if (input.analogXMovement != wallSlideInitialDirection || !hit)
                {
                    jumpState = JumpState.Falling;
                    OnWallDragEnd();
                }
            }
            bool c = CeilingCheck();
            transform.position += (Vector3)velocity;
            anim.SetFloat("_xVelocity", Mathf.Abs(velocity.x));
            anim.SetFloat("_yVelocity", velocity.y);
            anim.SetBool("_isGrounded", isGrounded);
        }

        ///////////////////////////////////////////

        public virtual void OnWallDragStart() { }
        public virtual void OnWallDragEnd() { }
        public virtual void OnJumpLaunch() { }
        public virtual void OnWallJumpLaunch(Vector2 pos, Vector2 dir) { }
        public virtual void OnTurnStart() { }
        public virtual void OnTurnEnd() { }
        public virtual void SyncLateUpdate() { }

        ///////////
        
        public IEnumerator doJumpCooldown()
        {
            canJump = false;
            yield return new WaitForSeconds(jumpCooldown);
            canJump = true;
        }

    }

    public enum JumpState
    {
        Grounded,
        PrepareJump,
        Jumping,
        Falling,
        Hanging,
        Sliding,
        Landing
    }

    [System.Serializable]
    public class Particles
    {
        public ParticleSystem wallSlideParticles;
        public GameObject jumpEffectPrefab;
        public ParticleSystem runParticles;
        public ParticleSystem skidParticles;
        public GameObject wallJumpEffect;
    }
}