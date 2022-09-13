using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class AnimationAndMovementController2 : MonoBehaviour
{

    PlayerInput playerInput;
    PlayerControls playerControls;
    CharacterController characterController;
    Animator animator;

    Vector2 movement;
    Vector2 aim;
    public Vector3 playerVelocity;
    [SerializeField] float gamepadRotationSmoothing = 1000f;
    [SerializeField] float gravity = -2.8f;
    [SerializeField] private float controllerDeadzone = 0.1f;
    [SerializeField] private bool isGamepad;
    [SerializeField] float dashTime = 0.06f;
    [SerializeField] float dashSpeed = 30f;
    [SerializeField] VisualEffect speedline;
    [SerializeField] GameObject volumeObject;
    Volume volume;
    ChromaticAberration chromaticAberration;


    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void Awake()
    {
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        speedline.Stop();
    }
    private void Start()
    {
        volume = volumeObject.GetComponent<Volume>();
        volume.profile.TryGet<ChromaticAberration>(out chromaticAberration);
    }

    private void Update()
    {

        HandleInput();
        HandleGravity();
        HandleAnimation();
        HandleDash();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    void HandleInput()
    {
        movement = playerControls.CharacterControls.Move.ReadValue<Vector2>();
        aim = playerControls.CharacterControls.Aim.ReadValue<Vector2>();
    }

    void HandleRotation()
    {
        if (isGamepad)
        {
            if (Mathf.Abs(aim.x) > controllerDeadzone || Mathf.Abs(aim.y) > controllerDeadzone)
            {
                Vector3 playerDirection = Vector3.right * aim.x + Vector3.forward * aim.y;
                if (playerDirection.sqrMagnitude > 0f)
                {
                    Quaternion newRotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, gamepadRotationSmoothing * Time.deltaTime);
                }
            }
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(aim);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float rayDistance;
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                Debug.DrawRay(ray.origin, point, Color.red);
                LookAt(point);
            }
        }
    }

    void LookAt(Vector3 lookPoint)
    {
        Vector3 heightCorrectedPoint = new Vector3(lookPoint.x, transform.position.y, lookPoint.z);
        transform.LookAt(heightCorrectedPoint);
    }

    void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            playerVelocity.y = 0;
        }
        else
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
    {
        return Mathf.Atan2(
            Vector3.Dot(n, Vector3.Cross(v1, v2)),
            Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    }

    void HandleAnimation()
    {
        animator.SetFloat("InputMagnitude", movement.magnitude);
        Vector3 movementp = new Vector3(movement.x, 0, movement.y);
        Vector3 test = transform.InverseTransformDirection(movementp) * 2;

        // animator.SetFloat("Horizontal", movement.x, 0.2f, Time.deltaTime);
        // animator.SetFloat("Vertical", movement.y, 0.2f, Time.deltaTime);

        Ray ray = Camera.main.ScreenPointToRay(aim);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);

            if (movement.magnitude < 0.1)
            {
                // 动画旋转player指向point
                Vector3 relative = transform.InverseTransformPoint(point);
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                animator.SetFloat("HorAimAngle", angle, 0.01f, Time.deltaTime);
            }

            if (movement.magnitude > 0.1)
            {
                transform.DOLookAt(new Vector3(point.x, transform.position.y, point.z), 0.5f);
                animator.SetFloat("Horizontal", test.x, 0.2f, Time.deltaTime);
                animator.SetFloat("Vertical", test.z, 0.2f, Time.deltaTime);
            }
        }

    }
    void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartCoroutine(Dash());
            StartCoroutine(StopDash());

        }
    }

    IEnumerator Dash()
    {
        for (float i = 0; i < dashTime; i += Time.deltaTime)
        {
            characterController.Move(new Vector3(movement.x, 0, movement.y) * dashSpeed * Time.deltaTime);
            speedline.Play();
            chromaticAberration.active = true;
            chromaticAberration.intensity.SetValue(new FloatParameter(0.5f, true));
            yield return null;
        }
    }

    IEnumerator StopDash()
    {
        yield return new WaitForSeconds(0.2f);
        speedline.Stop();
        DOTween.To(() => chromaticAberration.intensity.value, x => chromaticAberration.intensity.value = x, 0f, 0.5f);
        // chromaticAberration.active = false;
    }

    public void DeviceChange(PlayerInput pi)
    {
        isGamepad = pi.currentControlScheme.Equals("Gamepad") ? true : false;
    }
}
