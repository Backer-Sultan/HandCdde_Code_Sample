﻿/*********************************************
 * Project: HANDCODE                         *
 * Author:  Backer Sultan                    *
 * Email:   backer.sultan@ri.se              *
 * *******************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace HandCode
{
    // the order of the tasks is set here. declare task IDs in the order you want them to execute.
    public enum TaskID
    {
        NONE,
        MOVE_CRADLE_RIGHT,
        OPEN_ARMS,
        RAISE_ARMS,
        MOVE_SPOOL,
        LOWER_ARMS,
        TELEPORT_POS_4,
        TELEPORT_POS_2,
        HANDLE_SPOOL,
        RAISE_ARMS_WITH_SPOOL,
        LOWER_ARMS_WITH_SPOOL,
        TELEPORT_POS_3,
        LOWER_PINSHER,
        DISCONNECT_BREAK,
        CUT_PAPER,
        MOUNT_MIDDLE_TAPE,
        MOUNT_RIGHT_TAPE,
        MOUNT_LEFT_TAPE,
        MOUNT_ADHESIVE_TAPE,
        RAISE_PINSHER,
        MOVE_CRADLE_MIDDLE,
        TEST,
    }

    public enum TaskState
    {
        PENDING,
        ACTIVE,
        INTERRUPTED,
        COMPLETE,
    }

    public class Task : MonoBehaviour
    {
        /* fields & properties */

        public TaskID ID;
        public TaskState state { get { CheckState(); return _state; } }
        public Func<bool> completionCondition; // is specified and set in GameFlowManager script.
        public List<Task> dependencies; // the list of tasks needed to conteniously be checked during performing this task.

        [Header("Evetns")]
        public UnityEvent onStarted;
        public UnityEvent onInterrupted;
        public UnityEvent onCompleted;
        public UnityEvent onReset;

        private GameFlowManager gameFlowManager;
        [SerializeField] // for test only! This field should not be modified from inspector.
        private TaskState _state = TaskState.PENDING;

        /* methods & coroutines */

        public void StartTask()
        {
            _state = TaskState.ACTIVE;
            onStarted.Invoke();
        }

        private void Interrupt()
        {
            _state = TaskState.INTERRUPTED;
            onInterrupted.Invoke();
        }

        private void CheckState()
        {
            TaskState oldState = _state;
            if (completionCondition.Invoke() == true)
            {
                // `state` shouldn't be set to `complete` when checked while it's not active, even if it initially fulfills the completion condition.
                if (_state == TaskState.ACTIVE)
                    _state = TaskState.COMPLETE;

                // invoking `onComplete` event only when the state changes to complete.
                if (oldState != _state)
                    onCompleted.Invoke();
            }
            // the completion condition is broken from another task (while this task is not active)
            // AND this task is in the current-task dependecies.
            else if (gameFlowManager.currentTask!= null &&
                     gameFlowManager.currentTask.dependencies.Contains(this) && 
                     oldState == TaskState.COMPLETE)
            {
                _state = TaskState.PENDING;
                onReset.Invoke();
            }
        }

        private void CheckDependencies()
        {
            // preformance note: "`for` is twice faster than `foreach` on generic lists.
            for (int i = 0; i < dependencies.Count; i++)
            {
                if (dependencies[i].state != TaskState.COMPLETE)
                {
                    dependencies[i]._state = TaskState.PENDING;
                    Interrupt();
                }
            }
        }

        private void Start()
        {
            gameFlowManager = GameObject.FindObjectOfType<GameFlowManager>();
        }

        private void Update()
        {
            if (_state == TaskState.ACTIVE)
            {
                CheckDependencies();
                CheckState();
            }
        }
    }
}