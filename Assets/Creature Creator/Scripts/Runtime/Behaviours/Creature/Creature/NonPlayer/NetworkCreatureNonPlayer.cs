// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using Unity.Netcode;
using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class NetworkCreatureNonPlayer : NetworkCreature
    {
        #region Fields
        private bool despawn;
        #endregion

        #region Properties
        public override CreatureBase Creature
        {
            get
            {
                if (NetworkConnectionManager.IsConnected)
                {
                    if (IsOwner)
                    {
                        return local;
                    }
                    else
                    {
                        return remote;
                    }
                }
                else
                {
                    return local;
                }
            }
        }
        #endregion

        #region Methods
        public override void Setup()
        {
            

            base.Setup();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (despawn)
            {
                Despawn();
            }
        }
        public void Despawn()
        {
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            else
            {
                despawn = true;
            }
        }
        #endregion
    }
}