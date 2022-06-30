// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    public class Water : MonoBehaviour
    {
        [SerializeField] private bool allowSwimming;
        [SerializeField] private Ability swimAbility;

        private void OnTriggerEnter(Collider other)
        {
            CreatureSourcePlayer player = other.GetComponent<CreatureSourcePlayer>();
            if (!allowSwimming || (player != null && !player.Abilities.Abilities.Contains(swimAbility)))
            {
                this.InvokeAtEndOfFrame(player.Health.Die); // Can't set IsAnimated to false in physics frame?
            }
        }
    }
}