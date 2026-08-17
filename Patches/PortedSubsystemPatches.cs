using EFT;
using EFT.InventoryLogic;
using EFT.Interactive;
using HarmonyLib;
using RootMotion.FinalIK;
using SAIN.Components;
using SAIN.Components.Helpers;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Text;
using UnityEngine;
using Systems.Effects;
using static SAIN.Helpers.Shoot;

namespace SAIN.Patches.Vision
{
    public class GlobalLookSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalLookData), nameof(BotGlobalLookData.Update));
        }

        [PatchPostfix]
        public static void Patch(BotGlobalLookData __instance)
        {
            __instance.CHECK_HEAD_ANY_DIST = true;
            __instance.MIDDLE_DIST_CAN_SHOOT_HEAD = true;
            __instance.SHOOT_FROM_EYES = false;
        }
    }

    public class NoAIESPPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod(nameof(BotOwner.IsEnemyLookingAtMe), BindingFlags.Instance | BindingFlags.Public, null, [typeof(IPlayer)], null);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class CheckFlashlightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.SetLightsState));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player ____player)
        {
            PlayerComponent playerComponent = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(____player?.ProfileId);
            if (playerComponent == null)
            {
                return;
            }

            BotManagerComponent.Instance?.BotHearing.PlayAISound(playerComponent, SAINSoundType.GearSound, playerComponent.Player.WeaponRoot.position, 35f, 1f, true);
            playerComponent.Flashlight.CheckDevice();
        }
    }

    public class DisableLookUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), nameof(LookSensor.Activate));
        }

        [PatchPrefix]
        public static bool Patch(LookSensor __instance)
        {
            if (SAINEnableClass.IsBotExcluded(__instance._botOwner))
            {
                return true;
            }

            __instance.CalcVisibleDistance();
            __instance._taskMaxWaitPeriod = __instance._botOwner.Settings.FileSettings.Look.POSIBLE_VISION_SPACE * 0.75f;
            __instance._isBossOrFollower = __instance._botOwner.Profile.Info.Settings.IsBossOrFollower();
            return false;
        }
    }
}

namespace SAIN.Patches.Shoot.Aim
{
    internal class DisableMalfunctionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.GetMalfunctionState));
        }

        [PatchPostfix]
        public static void Patch(Player ____player, ref Weapon.EMalfunctionState __result)
        {
            if (____player.IsAI && __result != Weapon.EMalfunctionState.None)
            {
                __result = Weapon.EMalfunctionState.None;
            }
        }
    }

    internal class PlayerHitReactionDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HitReaction), nameof(HitReaction.Hit), [typeof(EBodyPartColliderType), typeof(EBodyPart), typeof(Vector3), typeof(Vector3), typeof(bool)]);
        }

        [PatchPrefix]
        public static bool Patch()
        {
            return !GlobalSettingsClass.Instance.Aiming.HitEffects.HIT_REACTION_TOGGLE;
        }
    }

    public class BotSteeringPitchLimitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSteering), nameof(BotSteering.SetYAngle));
        }

        [PatchPrefix]
        public static void Patch(ref float angle)
        {
            angle = Mathf.Max(angle, -65f);
        }
    }

    internal class AimOffsetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingData), nameof(BotAimingData.UpdateEndTargetPoint));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotAimingData __instance)
        {
            if (!SAINEnableClass.IsBotInCombat(__instance._owner))
            {
                return true;
            }

            __instance.EndTargetPoint = __instance.RealTargetPoint + (__instance._offsetStandart * __instance._timeCoef);
            return false;
        }
    }

    public class AimTimePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotAimingData), nameof(BotAimingData.CalcTimeShoot));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotAimingData __instance, float dist, float ang, ref float __result)
        {
            if (!SAINEnableClass.GetSAIN(__instance._owner.ProfileId, out var bot))
            {
                return true;
            }

            __result = CalculateAim(bot, dist, ang, __instance._lastFrameMove, __instance.IsPanic, __instance._nextAimingDelay);
            __instance._nextAimingDelay = 0f;
            __instance.LastAimTime = __result;
            bot.Aim.LastAimTime = __result;
            SAINTrace.LogThrottled(
                $"aimtime-{bot.ProfileId}",
                0.5f,
                $"AIMTIME calculated: {SAINTrace.Bot(bot)}, dist={dist:0.0}, angle={ang:0.0}, result={__result:0.00}, moving={__instance._lastFrameMove}, panic={__instance.IsPanic}, {SAINTrace.Enemy(bot.GoalEnemy)}");
            return false;
        }

        private static float CalculateAim(BotComponent botComponent, float distance, float angle, bool moving, bool panicing, float aimDelay)
        {
            BotOwner botOwner = botComponent.BotOwner;
            SAINAimingSettings sainAimSettings = botComponent.Info.FileSettings.Aiming;
            BotSettingsComponents fileSettings = botOwner.Settings.FileSettings;

            float baseAimTime = fileSettings.Aiming.BOTTOM_COEF;
            CoverPoint coverInUse = botComponent?.Cover.CoverInUse;
            bool inCover = botOwner.Memory.IsInCover || coverInUse?.BotInThisCover == true;
            if (inCover)
            {
                baseAimTime *= fileSettings.Aiming.COEF_FROM_COVER;
            }

            BotCurvSettings curve = botOwner.Settings.Curv;
            float angleTime = curve.AimAngCoef.Evaluate(angle) * sainAimSettings.AngleAimTimeMultiplier;
            float distanceTime = curve.AimTime2Dist.Evaluate(distance) * sainAimSettings.DistanceAimTimeMultiplier;
            float calculatedAimTime = angleTime * distanceTime * botOwner.Settings.Current.CurrentAccuratySpeed;

            if (panicing)
            {
                calculatedAimTime *= fileSettings.Aiming.PANIC_COEF;
            }

            float timeToAimResult = baseAimTime + calculatedAimTime + aimDelay;
            if (moving)
            {
                timeToAimResult *= fileSettings.Aiming.COEF_IF_MOVE;
            }
            if (botOwner.WeaponManager?.ShootController?.IsAiming == true)
            {
                timeToAimResult *= SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimDownSightsAimTimeMultiplier;
            }

            timeToAimResult = Mathf.Clamp(timeToAimResult, 0f, fileSettings.Aiming.MAX_AIM_TIME);
            if (SAINPlugin.LoadedPreset.GlobalSettings.Aiming.FasterCQBReactionsGlobal &&
                sainAimSettings?.FasterCQBReactions == true &&
                distance <= sainAimSettings.FasterCQBReactionsDistance)
            {
                float ratio = distance / sainAimSettings.FasterCQBReactionsDistance;
                float fasterTime = timeToAimResult * ratio;
                timeToAimResult = Mathf.Clamp(fasterTime, sainAimSettings.FasterCQBReactionsMinimum, timeToAimResult);
            }

            Enemy enemy = botComponent?.GoalEnemy;
            if (enemy != null)
            {
                timeToAimResult /= enemy.Aim.AimAndScatterMultiplier;
            }
            return timeToAimResult;
        }
    }

    public class SmoothTurnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSteering), nameof(BotSteering.Steering));
        }

        [PatchPrefix]
        public static bool Patch(BotSteering __instance)
        {
            BotOwner botOwner = __instance._owner;
            if (!GameWorldComponent.TryGetPlayerComponent(botOwner, out PlayerComponent playerComponent) || playerComponent.BotComponent == null)
            {
                return true;
            }

            if (playerComponent.BotComponent.SAINLayersActive)
            {
                var controller = playerComponent.CharacterController;
                controller.UpdateTurnSettings(Time.deltaTime, botOwner, playerComponent.BotComponent, GlobalSettingsClass.Instance.Steering.RANDOMSWAY_TOGGLE);
                controller.UpdateBotTurnData(Time.deltaTime);
                controller.RotatePlayer(playerComponent);
                __instance._lookDirection = controller.TurnData.CurrentLookDirection;
                return false;
            }

            var turnData = playerComponent.CharacterController.TurnData;
            var steeringDir = __instance._lookDirection;
            turnData.CurrentLookDirection = steeringDir;
            turnData.NewTargetLookDirection = steeringDir;
            turnData.LastTargetLookDirection = steeringDir;
            playerComponent.CharacterController.TurnData = turnData;
            return true;
        }
    }
}

namespace SAIN.Patches.Movement
{
    public class MovementContextIsAIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.IsAI));
        }

        [PatchPrefix]
        public static bool Patch(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class CanBeSnappedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Player), nameof(Player.CanBeSnapped));
        }

        [PatchPrefix]
        public static bool Patch(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class GlobalShootSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalShootData), nameof(BotGlobalShootData.Update));
        }

        [PatchPostfix]
        public static void PatchPrefix(BotGlobalShootData __instance)
        {
            __instance.MAX_DIST_COEF = 100f;
        }
    }

    public class StopShootCauseAnimatorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ShootData), nameof(ShootData.SetCanShootByState));
        }

        [PatchPrefix]
        public static void PatchPrefix(ShootData __instance, ref bool state)
        {
            if (SAINEnableClass.IsBotInCombat(__instance._owner))
            {
                SAINTrace.LogThrottled(
                    $"canshootstate-{__instance._owner.ProfileId}",
                    0.5f,
                    $"SHOOT SetCanShootByState forced true: bot={__instance._owner.name}, profile={__instance._owner.ProfileId}, requested={state}");
                state = true;
            }
        }
    }

    public class PoseStaminaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerPhysicalClass), nameof(PlayerPhysicalClass.ConsumePoseLevelChange));
        }

        [PatchPrefix]
        public static bool PatchPrefix(PlayerPhysicalClass __instance)
        {
            return !__instance._player.IsAI;
        }
    }

    public class AimStaminaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerPhysicalClass), nameof(PlayerPhysicalClass.Aim));
        }

        [PatchPrefix]
        public static bool PatchPrefix(PlayerPhysicalClass __instance)
        {
            return !__instance._player.IsAI;
        }
    }
}

namespace SAIN.Patches.Hearing
{
    public class OnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnMakingShot));
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, IWeapon weapon, Vector3 force)
        {
            if (GameWorldComponent.TryGetPlayerComponent(__instance, out PlayerComponent playerComponent))
            {
                playerComponent.OnMakingShot(weapon, force);
            }
        }
    }

    public class RegisterShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.RegisterShot), [typeof(Item), typeof(EftBulletClass)]);
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player, Item weapon, EftBulletClass shot)
        {
            GameWorldComponent.Instance?.RegisterShot(____player, shot, weapon);
        }
    }

    public class OnWeaponModifiedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.WeaponModified));
        }

        [PatchPrefix]
        public static void PatchPrefix(Player.FirearmController __instance, Player ____player)
        {
            if (GameWorldComponent.TryGetPlayerComponent(____player, out PlayerComponent playerComponent))
            {
                playerComponent.Equipment.WeaponModified(__instance.Weapon);
            }
        }
    }

    public class HearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotHearingSensor), nameof(BotHearingSensor.Init));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotHearingSensor __instance)
        {
            return !SAINEnableClass.IsSAINDisabledForBot(__instance._botOwner);
        }
    }

    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EffectsCommutator), nameof(EffectsCommutator.PlayHitEffect), [typeof(EftBulletClass), typeof(PlayerHitInfo)]);
        }

        [PatchPostfix]
        public static void PatchPostfix(EftBulletClass info)
        {
            BotManagerComponent.Instance?.BotHearing.BulletImpacted(info);
        }
    }

    public class GrenadeCollisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Grenade), nameof(Grenade.OnCollisionHandler));
        }

        [PatchPostfix]
        public static void Patch(Grenade __instance)
        {
            BotManagerComponent.Instance?.GrenadeController.GrenadeCollided(__instance, 35);
        }
    }
}

namespace SAIN.Patches.Talk
{
    public class PlayerHurtPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.ApplyHitDebuff), [typeof(float), typeof(float), typeof(EBodyPart), typeof(EDamageType)]);
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, float damage)
        {
            if (__instance?.HealthController?.IsAlive == true &&
                __instance.IsAI &&
                (!__instance.MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers) || damage > 4f))
            {
                __instance.Speaker?.Play(EPhraseTrigger.OnBeingHurt, __instance.HealthStatus, true, null);
            }
        }
    }

    public class PlayerTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.Say), [typeof(EPhraseTrigger), typeof(bool), typeof(float), typeof(ETagStatus), typeof(int), typeof(bool)]);
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player __instance, EPhraseTrigger phrase, ETagStatus mask, bool aggressive)
        {
            switch (phrase)
            {
                case EPhraseTrigger.OnDeath:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnAgony:
                case EPhraseTrigger.OnBreath:
                    BotManagerComponent.Instance?.BotHearing.PlayerTalked(phrase, mask, __instance);
                    return true;
            }

            if (__instance.IsAI)
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching || !SAINEnableClass.GetSAIN(__instance.ProfileId, out _))
                {
                    BotManagerComponent.Instance?.BotHearing.PlayerTalked(phrase, mask, __instance);
                    return true;
                }
                return false;
            }

            BotManagerComponent.Instance?.BotHearing.PlayerTalked(phrase, mask, __instance);
            return true;
        }
    }

    public class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), nameof(BotTalk.Say), [typeof(EPhraseTrigger), typeof(bool), typeof(ETagStatus?)]);
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotTalk __instance, EPhraseTrigger type)
        {
            if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching)
            {
                return true;
            }
            if (__instance._owner?.HealthController?.IsAlive == false)
            {
                return true;
            }
            switch (type)
            {
                case EPhraseTrigger.OnDeath:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnAgony:
                case EPhraseTrigger.OnBreath:
                    return true;
            }
            if (!SAINEnableClass.GetSAIN(__instance._owner.ProfileId, out BotComponent bot))
            {
                return true;
            }
            switch (type)
            {
                case EPhraseTrigger.HandBroken:
                case EPhraseTrigger.LegBroken:
                    bot.Talk.GroupSay(type, null, false, 60);
                    break;
            }
            return false;
        }
    }

    public class BotTalkManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), nameof(BotTalk.ManualUpdate));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotTalk __instance)
        {
            return SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching ||
                !SAINEnableClass.GetSAIN(__instance._owner.ProfileId, out _);
        }
    }
}

namespace SAIN.Patches.Shoot.Grenades
{
    public class SetGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGrenadeController), nameof(BotGrenadeController.SetThrowParams), [typeof(ThrowWeapItemClass)]);
        }

        [PatchPostfix]
        public static void Patch(ThrowWeapItemClass potentialGrenade, BotGrenadeController __instance)
        {
            if (potentialGrenade == null || !BotManagerComponent.Instance.GetSAIN(__instance._owner, out var botComponent))
            {
                return;
            }
            botComponent.Grenade.MyGrenade = potentialGrenade;
        }
    }

    public class ResetGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGrenadeController), nameof(BotGrenadeController.EndAll), [typeof(ThrowWeapItemClass)]);
        }

        [PatchPostfix]
        public static void Patch(BotGrenadeController __instance)
        {
            if (BotManagerComponent.Instance.GetSAIN(__instance._owner, out var botComponent))
            {
                botComponent.Grenade.MyGrenade = __instance.grenade;
            }
        }
    }
}

namespace SAIN.Patches.Shoot.RateOfFire
{
    public class BotShootPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ShootData), nameof(ShootData.Shoot));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ShootData __instance, ref bool __result)
        {
            BotOwner botOwner = __instance._owner;
            if (!SAINEnableClass.GetSAIN(botOwner.ProfileId, out BotComponent bot))
            {
                return true;
            }
            __result = false;
            if (__instance.ShootController == null)
            {
                SAINTrace.LogThrottled($"shootpatch-controller-null-{botOwner.ProfileId}", 0.5f, $"SHOOTPATCH rejected null ShootController: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(bot.GoalEnemy)}");
                return false;
            }
            BotUnderbarrelLauncherController underbarrelLauncherController = botOwner.WeaponManager.UnderbarrelLauncherController;
            if (underbarrelLauncherController.IsActive)
            {
                if (underbarrelLauncherController.NeedToReload() && !underbarrelLauncherController.TryReload())
                {
                    SAINTrace.Log($"SHOOTPATCH underbarrel reload failed, disabling: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(bot.GoalEnemy)}");
                    underbarrelLauncherController.TryDisable();
                    return false;
                }
                if (!underbarrelLauncherController.CheckShootAttemptAndDisableIfNeeded())
                {
                    SAINTrace.LogThrottled($"shootpatch-underbarrel-{botOwner.ProfileId}", 0.5f, $"SHOOTPATCH underbarrel check rejected: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(bot.GoalEnemy)}");
                    return false;
                }
                __instance.nextFingerDownCan = Time.time - 0.1f;
            }
            if (!__instance.Shooting && __instance.nextFingerDownCan < Time.time)
            {
                bool fullAuto = bot.Info.WeaponInfo.SelectedFireMode == Weapon.EFireMode.fullauto;
                if (fullAuto)
                {
                    __instance.nextFingerUpTime = Time.time + FullAutoBurstLength(bot, bot.DistanceToAimTarget);
                }
                __instance.nextFingerDownCan = Time.time + bot.Info.WeaponInfo.Firerate.CalcFirerateInterval();
                __instance.Shooting = true;
                __instance.timeFingerDown = Time.time;
                __instance.LastTriggerPressd = Time.time;
                __instance.ShootController.IsInLauncherMode();
                __instance.ShootController.SetTriggerPressed(true);
                botOwner.AimingManager.CurrentAiming.TriggerPressedDone();
                __result = true;
                SAINTrace.Log(
                    $"SHOOTPATCH trigger pressed: {SAINTrace.Bot(bot)}, fullAuto={fullAuto}, nextDown={__instance.nextFingerDownCan:0.00}, nextUp={__instance.nextFingerUpTime:0.00}, distance={bot.DistanceToAimTarget:0.0}, {SAINTrace.Enemy(bot.GoalEnemy)}");
                return false;
            }
            SAINTrace.LogThrottled(
                $"shootpatch-cooldown-{botOwner.ProfileId}",
                0.25f,
                $"SHOOTPATCH blocked cooldown/current shooting: {SAINTrace.Bot(bot)}, shooting={__instance.Shooting}, nextDown={__instance.nextFingerDownCan:0.00}, time={Time.time:0.00}, {SAINTrace.Enemy(bot.GoalEnemy)}");
            return false;
        }
    }
}
