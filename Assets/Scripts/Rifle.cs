﻿using System.Collections.Generic;
using UnityEngine;

public class Rifle: WeaponManager.Gun
{

    public override WeaponManager.GunType GetGunType() => WeaponManager.GunType.Rifle;

    public AudioClip fireSound;
    public AudioClip boltUp;
    public AudioClip boltBack;
    public AudioClip boltForward;
    public AudioClip boltDown;

    public List<GameObject> parts;

    public Transform barrel;
    public Transform center;
    public Transform stock;

    private const float crouchPositionSpeed = 4;

    private float _upChange;
    private float _upSoften;
    private float _rightChange;
    private float _rightSoften;
    private float _forward;

    private Vector3 _prevVelocity;

    private float _crouchFactor;
    private float _crouchReloadMod;
    private Player player;
    private PlayerInput input;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        input = Game.OnStartResolve<PlayerInput>();
    }

    public bool UseSideGun
    {
        get
        {
            if (!player.jumpKitEnabled) return false;
            if (_layer0Info.IsName("Unequip")) return false;

            if (player.IsSliding) return true;
            return false;
        }
    }

    public void ReloadComplete()
    {
        animator.SetBool("Reload", false);
    }

    public void BoltUp()
    {
        player.AudioManager.PlayAudio(boltUp);
    }

    public void BoltBack()
    {
        player.AudioManager.PlayAudio(boltBack);
    }

    public void BoltForward()
    {
        player.AudioManager.PlayAudio(boltForward);
    }

    public void BoltDown()
    {
        player.AudioManager.PlayAudio(boltDown);
    }

    private AnimatorStateInfo _layer0Info;
    private AnimatorStateInfo _layer1Info;

    private void FixedUpdate()
    {
        if (animator == null) return;
        _layer0Info = animator.GetCurrentAnimatorStateInfo(0);
        _layer1Info = animator.GetCurrentAnimatorStateInfo(1);
    }

    protected float leftHandFactor;
    private bool fireInputConsumed;

    private bool shotAvailable;

    private void Update()
    {
        if ((_layer1Info.normalizedTime <= 1 || _layer1Info.IsTag("Hold")) && _layer1Info.speed > 0)
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 1, Time.deltaTime / 0.05f);
            if (_layer1Info.IsTag("Instant")) leftHandFactor = 1;
        }
        else
        {
            leftHandFactor = Mathf.Lerp(leftHandFactor, 0, Time.deltaTime / 0.25f);
        }
        animator.SetLayerWeight(1, leftHandFactor);

        if (player.IsOnGround)
        {
            shotAvailable = true;
        }

        shotAvailable = true;

        if (fireInputConsumed && !input.IsKeyPressed(PlayerInput.PrimaryInteract)) fireInputConsumed = false;
        if (input.IsKeyPressed(PlayerInput.PrimaryInteract) && Time.timeScale > 0 && !animator.GetBool("Unequip") 
            && !animator.GetBool("Reload") && !fireInputConsumed && shotAvailable)
            //&& (player.IsOnGround || player.IsOnWall || player.ApproachingGround || player.ApproachingWall || player.IsInCoyoteTime()))
        {
            shotAvailable = false;
            //Fire(QueryTriggerInteraction.Collide, player.CrosshairDirection);
            fireInputConsumed = true;

            player.AudioManager.PlayOneShot(fireSound);
            //player.weaponManager.EquipGun(WeaponManager.GunType.None);

            if (animator != null)
            {
                animator.Play("Fire", -1, 0f);
                animator.SetBool("Reload", true);
            }
        }
    }

    private float aimFactor;
    private Vector3 toTargetVector;

    private void LateUpdate()
    {
        var yawMovement = player.YawIncrease;

        var velocityChange = player.velocity - _prevVelocity;

        if (UseSideGun)
        {
            if (_crouchFactor < 1) _crouchFactor += Time.deltaTime * crouchPositionSpeed;
        }
        else if (_crouchFactor > 0) _crouchFactor -= Time.deltaTime * crouchPositionSpeed;
        _crouchFactor = Mathf.Max(0, Mathf.Min(1, _crouchFactor));

        var crouchAmt = -(Mathf.Cos(Mathf.PI * _crouchFactor) - 1) / 2;

        _upChange -= velocityChange.y / 15;
        if (!player.IsOnGround && !player.IsOnWall) _upChange += Time.deltaTime * Mathf.Lerp(2, 1, crouchAmt);
        else
        {
            _upChange -= velocityChange.y / Mathf.Lerp(25, 50, crouchAmt);
        }

        _rightChange -= yawMovement / 3;

        _rightChange = Mathf.Lerp(_rightChange, 0, Time.deltaTime * 20);
        _upChange = Mathf.Lerp(_upChange, 0, Time.deltaTime * 8);

        _rightSoften = Mathf.Lerp(_rightSoften, _rightChange, Time.deltaTime * 20);

        if (_upSoften > _upChange)
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 10);
        }
        else
        {
            _upSoften = Mathf.Lerp(_upSoften, _upChange, Time.deltaTime * 5);
        }
        _upSoften = Mathf.Clamp(_upSoften, -1.3f, 1.3f);

        _prevVelocity = player.velocity;

        //_forward += Time.deltaTime / 1.2f;
        _forward = Mathf.Lerp(_forward, 0, Time.deltaTime * 8);

        var localforward = _forward;
        var localright = -0.02f * crouchAmt;
        var localup = Mathf.Lerp(0, 0.02f, crouchAmt);
        var globalup = _upSoften / 15;

        var roll = Mathf.Lerp(_rightSoften, 60, crouchAmt);
        var swing = _rightSoften / Mathf.Lerp(10, 5, crouchAmt);
        var tilt = _upSoften < 0 ? _upSoften * 10 : _upSoften;

        var rollAxis = center.up;
        var swingAxis = center.right;
        var tiltAxis = center.forward;

        tilt += 5 * _crouchReloadMod;
        localup -= 0.02f * _crouchReloadMod;
        localright -= 0.005f * _crouchReloadMod;
        roll -= 5 * _crouchReloadMod;

        roll += player.CameraRoll / 2;

        var barrelPosition = barrel.position;
        var centerPosition = center.position;
        var stockPosition = stock.position;
        foreach (var model in parts)
        {
            model.transform.localPosition += new Vector3(localforward, localright, localup);

            model.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
            model.gameObject.transform.RotateAround(centerPosition, swingAxis, swing);
            model.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
            model.transform.position += Vector3.up * globalup;
        }

        rightHand.transform.localPosition += new Vector3(localforward, localright, localup);

        rightHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll);
        rightHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing);
        rightHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt);
        rightHand.transform.position += Vector3.up * globalup;

        var mod = 1 - leftHandFactor;

        leftHand.transform.localPosition += new Vector3(localforward, localright, localup * mod);

        leftHand.gameObject.transform.RotateAround(barrelPosition, rollAxis, roll * mod);
        leftHand.gameObject.transform.RotateAround(centerPosition, swingAxis, swing * mod);
        leftHand.gameObject.transform.RotateAround(stockPosition, tiltAxis, tilt * mod);
        leftHand.transform.position += Vector3.up * globalup;
    }

    private static Vector3 Flatten(Vector3 vec)
    {
        return new Vector3(vec.x, 0, vec.z);
    }
}
