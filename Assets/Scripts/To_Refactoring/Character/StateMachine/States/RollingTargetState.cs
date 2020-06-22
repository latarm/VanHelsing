﻿using UnityEngine;
using System;


namespace BeastHunter
{
    public class RollingTargetState : CharacterBaseState
    {
        #region Constants

        private const float ROLL_LOW_SIDE_ANGLE = 45f;
        private const float ROLL_HALF_SIDE_ANGLE = 90f;
        private const float ROLL_BACK_SIDE_ANGLE = 225f;
        private const float ROLL_BACK_ANGLE = 180f;
        private const float ANGLE_SPEED_INCREASE = 1.225f;

        #endregion


        #region Fields

        private Vector3 MoveDirection;
        private Collider ClosestEnemy;
        private float _angleSpeedIncrease;
        private float _currentHorizontalInput;
        private float _currentVerticalInput;

        #endregion


        #region Properties

        public Action OnRollEnd { get; set; }
        private float RollTime { get; set; }

        #endregion


        #region ClassLifeCycle

        public RollingTargetState(CharacterModel characterModel, InputModel inputModel, CharacterAnimationController animationController,
            CharacterStateMachine stateMachine) : base(characterModel, inputModel, animationController, stateMachine)
        {
            CanExit = false;
            CanBeOverriden = false;
        }

        #endregion


        #region Methods

        public override void Initialize()
        {
            if (CanRoll())
            {
                _currentHorizontalInput = _inputModel.inputStruct._inputTotalAxisX;
                _currentVerticalInput = _inputModel.inputStruct._inputTotalAxisY;
                RollTime = _characterModel.CharacterCommonSettings.RollTime;
                _characterModel.AnimationSpeed = _characterModel.CharacterCommonSettings.RollAnimationSpeed;
                PrepareRoll(_currentHorizontalInput, _currentVerticalInput);
                CanExit = false;
                CanBeOverriden = false;
            }
            else
            {
                CanExit = true;
                CanBeOverriden = true;
                _stateMachine.ReturnState();
            }
        }

        public override void Execute()
        {
            ExitCheck();
            GetClosestEnemy();
            Roll();
            StayInBattle();
        }

        public override void OnExit()
        {
            _characterModel.AnimationSpeed = _characterModel.CharacterCommonSettings.AnimatorBaseSpeed;
        }

        private void ExitCheck()
        {
            RollTime -= Time.deltaTime;

            if (RollTime <= 0)
            {
                CanExit = true;
                CanBeOverriden = true;
                _stateMachine.ReturnState();
            }
        }

        private bool CanRoll()
        {
           return _inputModel.inputStruct._inputAxisX != 0 || _inputModel.inputStruct._inputAxisY != 0 ? true : false;           
        }

        private void Roll()
        {
            if (RollTime > 0)
            {
                if (ClosestEnemy != null)
                {
                    Vector3 lookPos = ClosestEnemy.transform.position - _characterModel.CharacterTransform.position;
                    lookPos.y = 0;
                    Quaternion rotation = Quaternion.LookRotation(lookPos);
                    _characterModel.CharacterTransform.rotation = rotation;
                }

                _characterModel.CharacterData.Move(_characterModel.CharacterTransform,
                    _characterModel.CharacterCommonSettings.RollFrameDistance, MoveDirection);
            }
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

        private void PrepareRoll(float rollingX, float rollingY)
        {
            _angleSpeedIncrease = rollingX != 0 && rollingY != 0 ? ANGLE_SPEED_INCREASE : 1;

            MoveDirection = (Vector3.forward * rollingY + Vector3.right * rollingX) / (Mathf.Abs(rollingY) + 
                Mathf.Abs(rollingX)) * _angleSpeedIncrease;

            _animationController.PlayRollAnimation(rollingX, rollingY);
        }

        private void StayInBattle()
        {
            _characterModel.IsInBattleMode = true;
        }

        #endregion
    }
}
