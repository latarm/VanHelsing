﻿using UnityEngine;


namespace BeastHunter
{
    public class BattleTargetMovementState : CharacterBaseState
    {
        #region Constants

        private const float CAMERA_LOW_SIDE_ANGLE = 45f;
        private const float CAMERA_HALF_SIDE_ANGLE = 90f;
        private const float CAMERA_BACK_SIDE_ANGLE = 225f;
        private const float CAMERA_BACK_ANGLE = 180f;
        private const float ANGLE_SPEED_INCREASE = 1.225f;

        #endregion


        #region Fields

        private float _currentHorizontalInput;
        private float _currentVerticalInput;
        private float _currentAngleVelocity;
        private float _targetSpeed;
        private float _speedChangeLag;
        private float _currentVelocity;
        private float _angleSpeedIncrease;

        #endregion


        #region Properties

        public Vector3 MoveDirection { get; private set; }
        public Collider ClosestEnemy { get; private set; }

        public float MovementSpeed { get; private set; }
        public float SpeedIncreace { get; private set; }

        #endregion


        #region ClassLifeCycle

        public BattleTargetMovementState(CharacterModel characterModel, InputModel inputModel, CharacterAnimationController animationController,
            CharacterStateMachine stateMachine) : base(characterModel, inputModel, animationController, stateMachine)
        {
            CanExit = false;
            CanBeOverriden = true;
            SpeedIncreace = _characterModel.CharacterCommonSettings.InBattleRunSpeed /
                _characterModel.CharacterCommonSettings.InBattleWalkSpeed;
        }

        #endregion


        #region Methods

        public override void Initialize()
        {
            CanExit = false;
            _characterModel.CharacterSphereCollider.radius *= _characterModel.CharacterCommonSettings.
                SphereColliderRadiusIncrese;
            GetClosestEnemy();
            _animationController.PlayBattleTargetMovementAnimation();
            _characterModel.CameraCinemachineBrain.m_DefaultBlend.m_Time = 1f;
            _characterModel.CharacterTargetCamera.Priority = 15;
        }

        public override void Execute()
        {
            ExitCheck();
            GetClosestEnemy();
            CountSpeed();
            MovementControl();
            StayInBattle();
        }

        public override void OnExit()
        {
            _characterModel.AnimationSpeed = _characterModel.CharacterCommonSettings.AnimatorBaseSpeed;
        }

        private void ExitCheck()
        {
            if(ClosestEnemy == null)
            {
                CanExit = true;
            }
        }

        private void StayInBattle()
        {
            _characterModel.IsInBattleMode = true;
        }

        private void MovementControl()
        {
            if (_characterModel.IsGrounded)
            {
                _currentHorizontalInput = _inputModel.inputStruct._inputTotalAxisX;
                _currentVerticalInput = _inputModel.inputStruct._inputTotalAxisY;
                _angleSpeedIncrease = _characterModel.IsMoving ? ANGLE_SPEED_INCREASE : 1;

                MoveDirection = (Vector3.forward * _currentVerticalInput + Vector3.right * _currentHorizontalInput) /
                    (Mathf.Abs(_currentVerticalInput) + Mathf.Abs(_currentHorizontalInput)) * _angleSpeedIncrease;

                if (ClosestEnemy != null)
                {
                    Vector3 lookPos = ClosestEnemy.transform.position - _characterModel.CharacterTransform.position;
                    lookPos.y = 0;
                    Quaternion rotation = Quaternion.LookRotation(lookPos);
                    _characterModel.CharacterTransform.rotation = rotation;
                }
                if (_characterModel.IsMoving)
                {
                    _characterModel.CharacterData.Move(_characterModel.CharacterTransform, MovementSpeed, MoveDirection);
                }
            }
        }

        private void CountSpeed()
        {
            if (_characterModel.IsMoving)
            {
                if (_inputModel.inputStruct._isInputRun)
                {
                    _targetSpeed = _characterModel.CharacterCommonSettings.InBattleRunSpeed;
                    _characterModel.AnimationSpeed = SpeedIncreace * ANGLE_SPEED_INCREASE;
                }
                else
                {
                    _targetSpeed = _characterModel.CharacterCommonSettings.InBattleWalkSpeed;
                    _characterModel.AnimationSpeed = ANGLE_SPEED_INCREASE;
                }
            }
            else
            {
                _targetSpeed = 0;
            }

            if (_characterModel.CurrentSpeed < _targetSpeed)
            {
                _speedChangeLag = _characterModel.CharacterCommonSettings.InBattleAccelerationLag;
            }
            else
            {
                _speedChangeLag = _characterModel.CharacterCommonSettings.InBattleDecelerationLag;
            }

            MovementSpeed = Mathf.SmoothDamp(_characterModel.CurrentSpeed, _targetSpeed, ref _currentVelocity,
                _speedChangeLag);
        }

        private void GetClosestEnemy()
        {
            Collider enemy = null;

            float minimalDistance = _characterModel.CharacterCommonSettings.SphereColliderRadius;
            float countDistance = minimalDistance;

            foreach (var collider in _characterModel.EnemiesInTrigger)
            {
                countDistance = Mathf.Sqrt((_characterModel.CharacterTransform.position -
                    collider.transform.position).sqrMagnitude);

                if (countDistance < minimalDistance)
                {
                    minimalDistance = countDistance;
                    enemy = collider;
                }
            }

            ClosestEnemy = enemy;
        }

        #endregion
    }
}


