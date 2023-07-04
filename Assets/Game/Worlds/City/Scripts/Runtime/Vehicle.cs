using PathCreation.Examples;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class Vehicle : NetworkBehaviour
    {
        #region Fields
        [SerializeField] private PathFollower follower;
        [SerializeField] private List<Transform> wheelsLeft;
        [SerializeField] private List<Transform> wheelsRight;
        [SerializeField] private float speed;
        [SerializeField] private float radius;
        #endregion

        #region Methods
        private void Start()
        {
            follower.enabled = IsServer;

            if (!IsServer && !NetworkObject.IsSpawned)
            {
                Destroy(gameObject);
            }
        }

#if UNITY_STANDALONE
        private void FixedUpdate()
        {
            float w = (speed / radius) * Mathf.Rad2Deg;
            float a = w * Time.fixedDeltaTime;

            foreach (Transform wheel in wheelsLeft)
            {
                wheel.Rotate(Vector3.right, a, Space.Self);
            }
            foreach (Transform wheel in wheelsRight)
            {
                wheel.Rotate(Vector3.left, a, Space.Self);
            }
        }
#endif

        private void OnCollisionEnter(Collision collision)
        {
            if (IsServer)
            {
                CreatureBase creature = collision.collider.GetComponent<CreatureBase>();
                if (creature != null && !creature.Health.IsDead)
                {
                    creature.Health.Kill(DamageReason.Vehicle);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (follower != null && follower.pathCreator != null)
            {
                follower.speed = speed;

                float d = follower.pathCreator.path.GetClosestDistanceAlongPath(transform.position);
                transform.position = follower.pathCreator.path.GetPointAtDistance(d);
                transform.rotation = follower.pathCreator.path.GetRotationAtDistance(d);
            }
        }
#endif
        #endregion
    }
}