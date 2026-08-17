using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Memory;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.Components
{
    //public enum EBotActiveState
    //{
    //    Active,
    //    Combat,
    //    Sleep,
    //    Disposed,
    //}

    public class BotComponent : BotComponentBase, ISPlayer
    {
        public Vector3 NavMeshPosition => Transform.NavData.Position;

        public float GetDistanceToPlayer(string ProfileId)
        {
            return PlayerComponent.GetDistanceToPlayer(ProfileId);
        }

        public bool IsPlayerInRange(string ProfileId, float maxDistance, out float playerDistance)
        {
            return PlayerComponent.IsPlayerInRange(ProfileId, maxDistance, out playerDistance);
        }

        public void ActivateIfBotActive(BotOwner botOwner)
        {
            if (botOwner.BotState == EBotState.Active)
            {
                Activate(botOwner);
            }
        }

        public void Activate(BotOwner botOwner)
        {
            if (_Activated)
            {
                return;
            }
            var playerComponent = botOwner.GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
#if DEBUG
                Logger.LogError("Person Null");
#endif
                return;
            }
            if (botOwner.BotState == EBotState.Active)
            {
                if (InitializeBot(playerComponent, botOwner))
                {
                    _Activated = true;
                    OnBotActivated?.Invoke(this);
                    return;
                }
                Dispose();
            }
        }

        public IBotAction CurrentAction => BotActivation.CurrentAction;

        public bool IsInCombat => BotActivation.BotInCombat;

        private bool _Activated = false;

        public event Action<BotComponent> OnBotActivated;

        public bool IsCheater { get; private set; }

        public bool BotActive => BotActivation.BotActive;
        public bool BotInStandBy => BotActivation.BotInStandBy;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool HasEnemy => Enemy.IsEnemyActive(EnemyController.GoalEnemy);
        public Enemy GoalEnemy => HasEnemy ? EnemyController.GoalEnemy : null;

        public BotGlobalEventsClass GlobalEvents { get; private set; }
        public BotBusyHandsDetector BusyHandsDetector { get; private set; }
        public SAINShootData Shoot { get; private set; }
        public BotWeightManagement WeightManagement { get; private set; }
        public SAINBotMedicalClass Medical { get; private set; }
        public SAINActivationClass BotActivation { get; private set; }
        public DoorOpener DoorOpener { get; private set; }
        public ManualShootClass ManualShoot { get; private set; }
        public CurrentTargetClass CurrentTarget { get; private set; }
        public BotBackpackDropClass BackpackDropper { get; private set; }
        public BotLightController BotLight { get; private set; }
        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFire { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public BotSquadContainer Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public BotGrenadeManager Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }
        public AimClass Aim { get; private set; }

        public bool IsDead => Player?.HealthController?.IsAlive != true;
        public bool GameEnding => BotActivation.GameEnding;
        public bool SAINLayersActive => BotActivation.SAINLayersActive;

        public float DistanceToAimTarget {
            get
            {
                //if (BotOwner.AimingManager.CurrentAiming != null)
                //{
                //    return BotOwner.AimingManager.CurrentAiming.LastDist2Target;
                //}
                if (EnemyController.GoalEnemy != null)
                {
                    return EnemyController.GoalEnemy.KnownPlaces.BotDistanceFromLastKnown;
                }
                return float.MaxValue;
            }
        }

        public float LastCheckVisibleTime;

        public ESAINLayer ActiveLayer {
            get
            {
                return BotActivation.ActiveLayer;
            }
            set
            {
                BotActivation.SetActiveLayer(value);
            }
        }

        public void ManualUpdate(float currentTime, float deltaTime)
        {
            BotOwner botOwner = BotOwner;
            if (botOwner != null)
            {
                Player player = botOwner.GetPlayer;
                if (player != null)
                {
                    TickClassGroup(AlwaysTickClasses, currentTime);

                    bool active = BotActive;
                    if (active)
                    {
                        TickClassGroup(TickWhenActiveClasses, currentTime);
                    }

                    bool inStandBy = !active || BotInStandBy;
                    if (!inStandBy)
                    {
                        TickClassGroup(TickWhenNoSleepClasses, currentTime);
                        handleDumbShit();
                    }

                    bool inCombat = active && !inStandBy && SAINLayersActive && GoalEnemy != null;
                    BotActivation.SetInCombat(inCombat);
                    if (inCombat)
                    {
                        TickClassGroup(TickWhenCombatClasses, currentTime);
                    }
                }
            }
        }

        private static void TickClassGroup(List<IBotClass> List, float CurrentTime)
        {
            for (int i = 0; i < List.Count; i++)
            {
                 List[i]?.ManualUpdate();
            }
        }

        public bool InitializeBot(PlayerComponent playerComponent, BotOwner botOwner)
        {
            base.Init(playerComponent, botOwner);
            if (!CreateClasses())
            {
                return false;
            }
            if (!AddToSquad())
            {
                return false;
            }
            if (!InitClasses())
            {
                return false;
            }
            if (!FinishInit(playerComponent))
            {
                return false;
            }
            return true;
        }

        private bool CreateClasses()
        {
            try
            {
                // Must be first, other classes use it
                Info = new SAINBotInfoClass(this);

                NoBushESP = gameObject.AddComponent<SAINNoBushESP>();

                Squad = new BotSquadContainer(this);
                BusyHandsDetector = new BotBusyHandsDetector(this);
                GlobalEvents = new BotGlobalEventsClass(this);
                Shoot = new SAINShootData(this);
                WeightManagement = new BotWeightManagement(this);
                Memory = new SAINMemoryClass(this);
                BotStuck = new SAINBotUnstuckClass(this);
                Hearing = new SAINHearingSensorClass(this);
                Talk = new SAINBotTalkClass(this);
                Decision = new SAINDecisionClass(this);
                Cover = new SAINCoverClass(this);
                SelfActions = new SAINSelfActionClass(this);
                Steering = new SAINSteeringClass(this);
                Grenade = new BotGrenadeManager(this);
                Mover = new SAINMoverClass(this);
                EnemyController = new SAINEnemyController(this);
                FriendlyFire = new SAINFriendlyFireClass(this);
                Vision = new SAINVisionClass(this);
                Search = new SAINSearchClass(this);
                Vault = new SAINVaultClass(this);
                Suppression = new SAINBotSuppressClass(this);
                AILimit = new SAINAILimit(this);
                AimDownSightsController = new AimDownSightsController(this);
                SpaceAwareness = new SAINBotSpaceAwareness(this);
                DoorOpener = new DoorOpener(this);
                Medical = new SAINBotMedicalClass(this);
                BotLight = new BotLightController(this);
                BackpackDropper = new BotBackpackDropClass(this);
                CurrentTarget = new CurrentTargetClass(this);
                ManualShoot = new ManualShootClass(this);
                BotActivation = new SAINActivationClass(this);
                Aim = new AimClass(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Creating Classes, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        public void AddBotClass(IBotClass Class)
        {
            if (Class == null)
            {
                Logger.LogError($"Bot Class of is null, cannot add it to list!");
                return;
            }
            BotClasses.Add(Class);
        }

        public void AddBotTickClass(IBotClass Class)
        {
            if (Class.CanEverTick)
            {
                switch (Class.TickRequirement)
                {
                    case ESAINTickState.AlwaysUpdate:
                        AlwaysTickClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotActive:
                        TickWhenActiveClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyNoSleep:
                        TickWhenNoSleepClasses.Add(Class);
                        break;

                    case ESAINTickState.OnlyBotInCombat:
                        TickWhenCombatClasses.Add(Class);
                        break;

                    default:
                        break;
                }
            }
        }

        private bool AddToSquad()
        {
            try
            {
                Squad.SquadInfo.AddMember(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error adding member to squad!: {ex}");
                return false;
            }
            return true;
        }

        private bool InitClasses()
        {
            try
            {
                NoBushESP.Init(PlayerComponent.BotOwner, this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Initializing Components, Disposing... : {ex}");
                return false;
            }
            foreach (var botClass in BotClasses)
            {
                try
                {
                    botClass.Init();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error When Initializing Class [{botClass}], Disposing... : {ex}");
                    return false;
                }
            }
            return true;
        }

        private bool FinishInit(PlayerComponent playerComponent)
        {
            try
            {
                if (!VerifyBrain(playerComponent))
                {
                    Logger.LogError("Init SAIN ERROR, Disposing...");
                    return false;
                }

                try
                {
                    BotOwner.LookSensor.MaxShootDist = float.MaxValue;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error setting MaxShootDist during init, but continuing with initialization...: {ex}");
                }

                try
                {
                    var settings = GlobalSettingsClass.Instance.General.Jokes;
                    if (settings.RandomCheaters &&
                        (EFTMath.RandomBool(settings.RandomCheaterChance) || Player.Profile.Nickname.ToLower().Contains("solarint")))
                    {
                        IsCheater = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error when initializing dumb shit for this bot, continuing anyways since its some dumb shit. Error: {ex}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Finishing Bot Initialization, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool VerifyBrain(PlayerComponent playerComp)
        {
            string assignedBrainName = playerComp.BotOwner?.Brain?.BaseBrain?.ShortName();

            if (Info.Profile.IsPMC)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPMCBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PMC") ? true : false;
            }

            if (Info.Profile.IsPlayerScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedPlayerScavBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "PlayerScav") ? true : false;
            }

            if (Info.Profile.IsScav)
            {
                IEnumerable<string> allowedBrainNames = AIBrains.GetAllowedScavBrains().Select(brain => brain.ToString());
                return IsAssignedBrainAllowed(assignedBrainName, allowedBrainNames, "Scav") ? true : false;
            }

            return true;
        }

        private bool IsAssignedBrainAllowed(string assignedBrainName, IEnumerable<string> allowedBrainNames, string botCategory)
        {
            if (allowedBrainNames.Contains(assignedBrainName))
            {
                return true;
            }

            Logger.LogAndNotifyError($"{BotOwner.name} is a ${botCategory} but does not have any of these BaseBrains: ${string.Join(", ", allowedBrainNames)}! Current Brain Assignment: [{assignedBrainName}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");

            return false;
        }

        private void OnDisable()
        {
            BotActivation.SetActive(false);
            StopAllCoroutines();
        }

        private void OnEnable()
        {

        }

        public void LateUpdate()
        {
            //BotActivation?.LateUpdate();
            //EnemyController?.LateUpdate();
        }

        private void handleDumbShit()
        {
            if (IsCheater)
            {
                if (defaultMoveSpeed == 0)
                {
                    defaultMoveSpeed = Player.MovementContext.MaxSpeed;
                    defaultSprintSpeed = Player.MovementContext.SprintSpeed;
                }
                Player.Grounder.enabled = GoalEnemy == null;
                if (GoalEnemy != null)
                {
                    Player.MovementContext.SetCharacterMovementSpeed(350, true);
                    Player.MovementContext.SprintSpeed = 50f;
                    Player.ChangeSpeed(100f);
                    Player.UpdateSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    Player.MovementContext.ChangeSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    BotOwner.SetTargetMoveSpeed(100f);
                }
                else
                {
                    Player.MovementContext.SetCharacterMovementSpeed(defaultMoveSpeed, false);
                    Player.MovementContext.SprintSpeed = defaultSprintSpeed;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            BotActivation?.SetActive(false);
            StopAllCoroutines();

            foreach (var botClass in BotClasses)
            {
                try
                {
                    botClass.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Dispose Class [{botClass}] Error: {ex}");
                }
            }

            if (NoBushESP != null)
                Destroy(NoBushESP);
            if (BotOwner != null)
                BotOwner.OnBotStateChange -= resetBot;

            Destroy(this);
        }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        /// <summary>
        /// All Bot Component classes.
        /// </summary>
        private readonly List<IBotClass> BotClasses = [];

        /// <summary>
        /// Bot classes that should tick no matter what.
        /// </summary>
        private readonly List<IBotClass> AlwaysTickClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is active.
        /// </summary>
        private readonly List<IBotClass> TickWhenActiveClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot not sleeping.
        /// </summary>
        private readonly List<IBotClass> TickWhenNoSleepClasses = [];

        /// <summary>
        /// Bot classes that should tick when a bot is in combat.
        /// </summary>
        private readonly List<IBotClass> TickWhenCombatClasses = [];

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;
    }
}
