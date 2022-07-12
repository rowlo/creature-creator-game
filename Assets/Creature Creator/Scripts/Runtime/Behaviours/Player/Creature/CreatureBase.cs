// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(CreatureConstructor))]
    [RequireComponent(typeof(CreatureAnimator))]
    [RequireComponent(typeof(CreatureCollider))]
    [RequireComponent(typeof(CreatureCloner))]
    [RequireComponent(typeof(CreaturePhotographer))]
    [RequireComponent(typeof(CreatureKiller))]
    [RequireComponent(typeof(CreatureEffector))]
    [RequireComponent(typeof(CreatureScaler))]
    [RequireComponent(typeof(CreatureHealth))]
    [RequireComponent(typeof(CreatureEnergy))]
    [RequireComponent(typeof(CreatureAge))]
    [RequireComponent(typeof(CreatureHider))]
    public class CreatureBase : MonoBehaviour
    {
        #region Fields
        [SerializeField] private CreatureConstructor constructor;
        [SerializeField] private CreatureAnimator animator;
        [SerializeField] private new CreatureCollider collider;
        [SerializeField] private CreatureCloner cloner;
        [SerializeField] private CreaturePhotographer photographer;
        [SerializeField] private CreatureKiller killer;
        [SerializeField] private CreatureEffector effector;
        [SerializeField] private CreatureScaler scaler;
        [SerializeField] private CreatureHealth health;
        [SerializeField] private CreatureEnergy energy;
        [SerializeField] private CreatureAge age;
        [SerializeField] private CreatureHider hider;
        #endregion

        #region Properties
        public CreatureConstructor Constructor => constructor;
        public CreatureAnimator Animator => animator;
        public CreatureCollider Collider => collider;
        public CreatureCloner Cloner => cloner;
        public CreaturePhotographer Photographer => photographer;
        public CreatureKiller Killer => killer;
        public CreatureEffector Effector => effector;
        public CreatureScaler Scaler => scaler;
        public CreatureHealth Health => health;
        public CreatureEnergy Energy => energy;
        public CreatureAge Age => age;
        public CreatureHider Hider => hider;
        #endregion

        #region Methods
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            constructor = GetComponent<CreatureConstructor>();
            animator = GetComponent<CreatureAnimator>();
            collider = GetComponent<CreatureCollider>();
            photographer = GetComponent<CreaturePhotographer>();
            cloner = GetComponent<CreatureCloner>();
            killer = GetComponent<CreatureKiller>();
            effector = GetComponent<CreatureEffector>();
            scaler = GetComponent<CreatureScaler>();
            health = GetComponent<CreatureHealth>();
            energy = GetComponent<CreatureEnergy>();
            age = GetComponent<CreatureAge>();
            hider = GetComponent<CreatureHider>();
        }
#endif

        public virtual void Setup()
        {
            gameObject.SetActive(true);
            Scaler.Setup();
            Animator.Setup();
        }
        #endregion
    }
}