using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System;
using System.IO;
using GoogleMobileAds.Api;
using Unity.Services.Core;

#if UNITY_STANDALONE
using Steamworks;
#endif

namespace DanielLochner.Assets.CreatureCreator
{
    public class PremiumManager : DataManager<PremiumManager, Premium>, IStoreListener
    {
        #region Fields
        private BannerView bannerAd;
        private RewardedAd rewardAd;

        private bool wasPrevPurchased;
        #endregion

        #region Properties
        public override string SALT
        {
            get
            {
#if UNITY_STANDALONE
                if (EducationManager.Instance.IsEducational)
                {
                    return SystemInfo.deviceUniqueIdentifier;
                }
                else
                {
                    return SteamUser.GetSteamID().ToString();
                }
#elif UNITY_IOS || UNITY_ANDROID
                return SystemInfo.deviceUniqueIdentifier;
#endif
            }
        }

        private string BannerAdUnitId
        {
            get
            {
#if UNITY_EDITOR
                string adUnitId = "unused";
#elif UNITY_ANDROID
                string adUnitId = "ca-app-pub-8574849693522303/8775844882";
#elif UNITY_IOS
                string adUnitId = "ca-app-pub-8574849693522303/2350037330";
#else
                string adUnitId = "unexpected_platform";
#endif
                return adUnitId;
            }
        }
        private string RewardAdUnitId
        {
            get
            {
#if UNITY_EDITOR
                string adUnitId = "unused";
#elif UNITY_ANDROID
                string adUnitId = "ca-app-pub-8574849693522303/4330129572";
#elif UNITY_IOS
                string adUnitId = "ca-app-pub-8574849693522303/3208619599";
#else
                string adUnitId = "unexpected_platform";
#endif
                return adUnitId;
            }
        }
        private int BannerAdWidth
        {
            get
            {
                float p = Screen.safeArea.width / Screen.width;
                return 160 * Mathf.RoundToInt((p * Display.main.systemWidth / Screen.dpi) / 3f);
            }
        }
        public RewardedItem RequestedItem
        {
            get;
            set;
        }

        public Action<PurchaseFailureReason> OnPremiumFailed
        {
            get;
            set;
        }
        public Action OnPremiumPurchased
        {
            get;
            set;
        }

        public IExtensionProvider Extensions
        {
            get;
            private set;
        }
        public IStoreController Controller
        {
            get;
            private set;
        }

        public bool IsIAPInitialized
        {
            get => Controller != null && Extensions != null;
        }
        public bool IsRewardAdLoaded
        {
            get => rewardAd != null && rewardAd.CanShowAd();
        }
        #endregion

        #region Methods
        protected override void Awake()
        {
            if (File.Exists(Path.Combine(Application.persistentDataPath, "settings.dat")) && !File.Exists(Path.Combine(Application.persistentDataPath, "premium.dat")))
            {
                wasPrevPurchased = true;
            }
            base.Awake();
        }
        protected override void Start()
        {
            base.Start();
            if (SystemUtility.IsDevice(DeviceType.Desktop) || wasPrevPurchased)
            {
                Data.IsPremium = true;
                Save();
            }

            if (SystemUtility.IsDevice(DeviceType.Handheld) || Application.isEditor)
            {
                InitializeAds();
                InitializePurchasesAsync();
            }
        }

        #region Ads
        private void InitializeAds()
        {
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            MobileAds.Initialize(OnInitialized);
        }

        public void OnInitialized(InitializationStatus status)
        {
            RequestBannerAd();
        }

        #region Banner
        public void RequestBannerAd()
        {
            //if (Data.IsPremium) return;

            //bannerAd?.Destroy();
            //bannerAd = new BannerView(BannerAdUnitId, AdSize.GetLandscapeAnchoredAdaptiveBannerAdSizeWithWidth(BannerAdWidth), AdPosition.Top);

            //bannerAd.OnBannerAdLoaded += OnBannerAdLoaded;
            //bannerAd.OnBannerAdLoadFailed += OnBannerAdLoadFailed;

            //bannerAd.LoadAd(new AdRequest());
            //bannerAd.Hide();
        }

        public void ShowBannerAd()
        {
            //if (Data.IsPremium) return;
            //bannerAd?.Show();
        }
        public void HideBannerAd()
        {
            //bannerAd?.Hide();
        }

        private void OnBannerAdLoadFailed(LoadAdError error)
        {
            if (error.GetCode() == 2)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    this.InvokeUntil(() => Application.internetReachability != NetworkReachability.NotReachable, RequestBannerAd);
                }
            }
        }
        private void OnBannerAdLoaded()
        {
        }
        #endregion

        #region Reward
        public void RequestRewardAd(Action<RewardedAd, LoadAdError> onLoaded)
        {
            if (IsRewardAdLoaded) return;

            try
            {
                RewardedAd.Load(RewardAdUnitId, new AdRequest(), delegate (RewardedAd ad, LoadAdError error) 
                {
                    rewardAd = ad;

                    if (ad == null || error != null)
                    {
                        InformationDialog.Inform($"Error ({error?.GetCode()})", error?.GetMessage());
                        return;
                    }

                    ad.OnAdFullScreenContentOpened += OnRewardAdOpened;
                    ad.OnAdFullScreenContentClosed += OnRewardAdClosed;

                    onLoaded(ad, error);
                });
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        public void ShowRewardAd()
        {
            rewardAd?.Show(OnRewardAdCompleted);
        }

        private void OnRewardAdOpened()
        {
            MobileAds.SetApplicationMuted(true);
        }
        private void OnRewardAdClosed()
        {
            MobileAds.SetApplicationMuted(false);
        }
        private void OnRewardAdCompleted(Reward reward)
        {
            PremiumMenu.Instance.Close(true);

            RewardsMenu.Instance.Clear();
            if (RequestedItem != null)
            {
                Access(RequestedItem);
                AccessRandom(3);
            }
            else
            {
                AccessRandom(4);
            }
            RewardsMenu.Instance.Open(false, LocalizationUtility.Localize("premium_free_success_title"));

            EditorManager.Instance?.UpdateUsability();
            OnRewardAdClosed();
        }

        public void Access(RewardedItem item)
        {
            switch (item.Type)
            {
                case ItemType.BodyPart:
                    RewardsMenu.Instance.Add(DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", item.Id));
                    Data.UsableBodyParts.Add(item.Id, true);
                    break;

                case ItemType.Pattern:
                    RewardsMenu.Instance.Add(DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", item.Id));
                    Data.UsablePatterns.Add(item.Id, true);
                    break;
            }
            Save();
        }
        public void AccessRandom(int count)
        {
            List<RewardedItem> items = new List<RewardedItem>();
            foreach (var kv in DatabaseManager.GetDatabase("Body Parts").Objects)
            {
                BodyPart bodyPart = kv.Value as BodyPart;
                if (bodyPart.Premium && !Data.UsableBodyParts.ContainsKey(kv.Key))
                {
                    items.Add(new RewardedItem(ItemType.BodyPart, kv.Key));
                }
            }
            foreach (var kv in DatabaseManager.GetDatabase("Patterns").Objects)
            {
                Pattern pattern = kv.Value as Pattern;
                if (pattern.Premium && !Data.UsablePatterns.ContainsKey(kv.Key))
                {
                    items.Add(new RewardedItem(ItemType.Pattern, kv.Key));
                }
            }

            items.Shuffle();

            for (int i = 0; i < count && i < items.Count; i++)
            {
                Access(items[i]);
            }
        }
        #endregion
        #endregion

        #region IAPs
        private async void InitializePurchasesAsync()
        {
            await UnityServices.InitializeAsync();

            UnityPurchasing.Initialize(this, GetConfig());
        }

        private ConfigurationBuilder GetConfig()
        {
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct("cc_premium", ProductType.NonConsumable, new IDs()
            {
                { "cc_premium", GooglePlay.Name },
                { "cc_premium", AppleAppStore.Name}
            });
            return builder;
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Controller = controller;
            Extensions = extensions;
        }
        public void OnInitializeFailed(InitializationFailureReason error)
        {
        }
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
        }
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchase)
        {
            if (purchase.purchasedProduct.definition.id == "cc_premium")
            {
                Data.IsPremium = true;
                Save();

                OnPremiumPurchased?.Invoke();
            }

            return PurchaseProcessingResult.Complete;
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            if (product.definition.id == "cc_premium")
            {
                OnPremiumFailed?.Invoke(reason);
            }
        }
        #endregion

        #region Helper
        public bool IsBodyPartUsable(string bodyPartId)
        {
            if (Data.IsPremium) return true;

            if (DatabaseManager.GetDatabaseEntry<BodyPart>("Body Parts", bodyPartId).Premium)
            {
                return Data.UsableBodyParts.ContainsKey(bodyPartId);
            }
            else
            {
                return true;
            }
        }
        public bool IsPatternUsable(string patternId)
        {
            if (Data.IsPremium) return true;

            if (DatabaseManager.GetDatabaseEntry<Pattern>("Patterns", patternId).Premium)
            {
                return Data.UsablePatterns.ContainsKey(patternId);
            }
            else
            {
                return true;
            }
        }

        public bool IsEverythingUsable()
        {
            if (Data.IsPremium) return true;

            foreach (string id in DatabaseManager.GetDatabase("Body Parts").Objects.Keys)
            {
                if (!IsBodyPartUsable(id))
                {
                    return false;
                }
            }
            foreach (string id in DatabaseManager.GetDatabase("Patterns").Objects.Keys)
            {
                if (!IsPatternUsable(id))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
        #endregion

        #region Nested
        [Serializable]
        public class RewardedItem
        {
            public ItemType Type;
            public string Id;

            public RewardedItem(ItemType type, string id)
            {
                Type = type;
                Id = id;
            }
        }

        public enum ItemType
        {
            BodyPart,
            Pattern
        }
        #endregion
    }
}