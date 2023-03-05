using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.CreatureCreator
{
    public class AchievementUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image icon;
        [SerializeField] private HoverUI hoverUI;
        [SerializeField] public CanvasGroup canvasGroup;
        #endregion

        #region Methods
        public void Setup(string achievementId)
        {
            Achievement achievement = StatsManager.Instance.GetAchievement(achievementId);

            name = achievementId;
            icon.sprite = achievement.unlockedIcon;

            hoverUI.OnEnter.AddListener(delegate
            {
                if (!Input.GetMouseButton(0))
                {
                    AchievementMenu.Instance.Setup(achievement);
                }
            });
            hoverUI.OnExit.AddListener(delegate
            {
                AchievementMenu.Instance.Clear();
            });
        }
        #endregion
    }
}