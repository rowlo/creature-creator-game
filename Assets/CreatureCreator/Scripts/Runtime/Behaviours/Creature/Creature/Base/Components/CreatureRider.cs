// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using Unity.Netcode;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

namespace DanielLochner.Assets.CreatureCreator
{
    public class CreatureRider : NetworkBehaviour
    {
        #region Fields
        private List<CreatureRider> riders = new List<CreatureRider>();

        private ClientNetworkTransform clientNetworkTransform;
        private CreatureRider baseRider;
        #endregion

        #region Properties
        public CreatureConstructor Constructor { get; set; }
        public CreatureCollider Collider { get; set; }
        public CreatureAnimator Animator { get; set; }

        public NetworkVariable<BaseData> Base { get; set; } = new NetworkVariable<BaseData>();

        public bool IsRiding => Base.Value != null;
        public bool IsBase => riders.Count > 0;
        #endregion

        #region Methods
        private void Awake()
        {
            Constructor = GetComponent<CreatureConstructor>();
            Collider = GetComponent<CreatureCollider>();
            Animator = GetComponent<CreatureAnimator>();

            clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        }
        private void Start()
        {
            Base.OnValueChanged += OnBaseChanged;

            if (Base.Value != null)
            {
                OnBaseChanged(null, Base.Value);
            }
        }
        private void Update()
        {
            if (IsLocalPlayer)
            {
                HandleInput();
            }
        }
        private void LateUpdate()
        {
            if (Base.Value != null)
            {
                if (baseRider != null)
                {
                    HandlePositionAndRotation();
                }
                else
                if (IsServer)
                {
                    Dismount();
                }
            }
        }

        public void Ride(CreatureRider rider)
        {
            RideServerRpc(new NetworkObjectReference(rider.NetworkObject));
        }
        [ServerRpc(RequireOwnership = false)]
        private void RideServerRpc(NetworkObjectReference baseNetObjRef)
        {
            if (IsRiding || IsBase) return;

            CreatureRider baseRider = GetRider(baseNetObjRef);

            // Base
            if (baseRider.Base.Value != null)
            {
                baseNetObjRef = baseRider.Base.Value.reference;
                baseRider = GetRider(baseNetObjRef);
            }

            // Height
            float height = baseRider.Constructor.Dimensions.Height;
            foreach (CreatureRider rider in baseRider.riders)
            {
                height += rider.Constructor.Dimensions.Height;
            }
            baseRider.riders.Add(this);

            Base.Value = new BaseData(baseNetObjRef, height);
        }

        public void Dismount()
        {
            DismountServerRpc();
        }
        [ServerRpc(RequireOwnership = false)]
        private void DismountServerRpc()
        {
            if (IsBase)
            {
                foreach (CreatureRider rider in new List<CreatureRider>(riders))
                {
                    rider.Dismount();
                }
            }
            else
            if (IsRiding)
            {
                CreatureRider baseRider = GetRider(Base.Value.reference);
                if (baseRider != null)
                {
                    foreach (CreatureRider rider in baseRider.riders)
                    {
                        if (rider.Base.Value.height > Base.Value.height)
                        {
                            rider.Base.Value = new BaseData(rider.Base.Value, -Constructor.Dimensions.Height);
                        }
                    }
                    baseRider.riders.Remove(this);
                }

                Base.Value = null;
            }
        }

        private void OnBaseChanged(BaseData oldBase, BaseData newBase)
        {
            bool isVisible = Constructor.gameObject.activeSelf;
            bool isRiding = newBase != null;

            if (isRiding)
            {
                baseRider = GetRider(newBase.reference);
                HandlePositionAndRotation();
            }
            else
            {
                baseRider = null;
            }

            if (IsLocalPlayer)
            {
                Constructor.Rigidbody.isKinematic = isRiding;
                clientNetworkTransform.Teleport(transform.position, transform.rotation, transform.localScale);
            }

            clientNetworkTransform.enabled = !isRiding;
            Animator.enabled = !isRiding && isVisible;
            Collider.enabled = !isRiding && isVisible;
        }

        private void HandleInput()
        {
            if (InputUtility.GetKeyDown(KeybindingsManager.Data.Dismount))
            {
                Dismount();
            }
        }
        private void HandlePositionAndRotation()
        {
            transform.position = baseRider.transform.position + (Base.Value.height * baseRider.transform.up);
            transform.rotation = baseRider.transform.rotation;
        }

        #region Helper
        private CreatureRider GetRider(NetworkObjectReference reference)
        {
            if (reference.TryGet(out NetworkObject networkObject))
            {
                return networkObject.GetComponent<CreatureRider>();
            }
            return null;
        }
        #endregion
        #endregion

        #region Nested
        public class BaseData : INetworkSerializable
        {
            public NetworkObjectReference reference;
            public float height;

            public BaseData()
            {
            }
            public BaseData(NetworkObjectReference reference, float height)
            {
                this.reference = reference;
                this.height = height;
            }
            public BaseData(BaseData data, float offset)
            {
                this.reference = data.reference;
                this.height = data.height + offset;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref reference);
                serializer.SerializeValue(ref height);
            }
        }
        #endregion
    }
}