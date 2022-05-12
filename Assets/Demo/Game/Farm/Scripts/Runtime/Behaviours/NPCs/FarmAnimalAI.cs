// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System;
using UnityEngine;
using UnityEngine.AI;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class FarmAnimalAI<T> : AnimalAI<T> where T : FarmAnimalAI<T>
    {
        #region Fields
        [SerializeField] private WanderInfo wander;

        private NavMeshAgent agent;
        #endregion

        #region Properties
        public bool IsMovingToPosition
        {
            get
            {
                if (agent.isPathStale || agent.isStopped)
                {
                    return false;
                }
                else if (agent.pathPending)
                {
                    return true;
                }

                return (agent.remainingDistance > agent.stoppingDistance);
            }
        }

        protected override string StartState => "WAN";
        #endregion

        #region Methods
        public override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnDrawGizmos()
        {
            if (wander.center != null)
            {
                Gizmos.DrawWireSphere(wander.center.position, wander.radius);
            }
        }
        #endregion

        #region Inner Classes
        public class Wandering : Idling
        {
            private float timeLeft;

            public Wandering(T sm) : base("Wandering", sm) { }

            public override void Enter()
            {
                base.Enter();
                timeLeft = StateMachine.wander.cooldown.Random;
            }
            public override void UpdateLogic()
            {
                base.UpdateLogic();

                TimerUtility.OnTimer(ref timeLeft, StateMachine.wander.cooldown.Random, Time.deltaTime, delegate 
                {
                    StateMachine.ChangeState("REP");
                });
            }
        }

        public class Repositioning : BaseState<T>
        {
            public Repositioning(T sm) : base("Repositioning", sm) { }

            public override void Enter()
            {
                Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * StateMachine.transform.forward;
                float distance = UnityEngine.Random.Range(0f, StateMachine.wander.radius);

                Vector3 position = StateMachine.wander.center.position;
                if (NavMesh.SamplePosition(position + (direction * distance), out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    position = hit.position;
                }
                StateMachine.agent.SetDestination(position);
            }
            public override void UpdateLogic()
            {
                if (!StateMachine.IsMovingToPosition)
                {
                    StateMachine.ChangeState("WAN");
                }
            }
        }

        [Serializable]
        public class WanderInfo
        {
            public float radius;
            public MinMax cooldown;
            public Transform center;
        }
        #endregion
    }
}