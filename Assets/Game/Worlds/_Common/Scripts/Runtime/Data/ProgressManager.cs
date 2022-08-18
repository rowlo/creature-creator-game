// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using Steamworks;

namespace DanielLochner.Assets.CreatureCreator
{
    public class ProgressManager : DataManager<ProgressManager, Progress>
    {
        public override string SALT => SteamUser.GetSteamID().ToString();

        //public bool IsComplete
        //{
        //    get => Data.UnlockedBodyParts.Count == DatabaseManager.GetDatabase("Body Parts").Objects.Count && Data.UnlockedPatterns.Count  == DatabaseManager.GetDatabase("Patterns").Objects.Count;
        //}
    }
}