using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINShootData : BotComponentClassBase
    {
        public Enemy LastShotEnemy { get; private set; }

        public SAINShootData(BotComponent bot) : base(bot)
        {
            TickRequirement = ESAINTickState.OnlyBotInCombat;
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += CheckClearEnemy;
            base.Init();
        }

        public override void ManualUpdate()
        {
            CheckEndShoot();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= CheckClearEnemy;
            base.Dispose();
        }

        private void CheckEndShoot()
        {
            if (!_shooting) return;
            BotWeaponManager weaponManager = BotOwner.WeaponManager;
            if (weaponManager == null || !weaponManager.HaveBullets || weaponManager.Reload.Reloading)
            {
                SAINTrace.Log($"SHOOT ending no weapon/bullets/reload: {SAINTrace.Bot(Bot)}, haveBullets={weaponManager?.HaveBullets}, reloading={weaponManager?.Reload?.Reloading}, {SAINTrace.Enemy(LastShotEnemy)}");
                EndShoot();
                return;
            }
            if (LastShotEnemy?.EnemyPlayer?.HealthController?.IsAlive != true)
            {
                SAINTrace.Log($"SHOOT ending dead/missing enemy: {SAINTrace.Bot(Bot)}, {SAINTrace.Enemy(LastShotEnemy)}");
                EndShoot();
                LastShotEnemy = null;
                return;
            }
            if (!BotOwner.ShootData.Shooting)
            {
                SAINTrace.Log($"SHOOT ending vanilla shooting false: {SAINTrace.Bot(Bot)}, {SAINTrace.Enemy(LastShotEnemy)}");
                EndShoot();
            }
        }

        private void CheckClearEnemy(string profileId, Enemy enemy)
        {
            if (LastShotEnemy == enemy)
            {
                LastShotEnemy = null;
                if (_shooting)
                {
                    EndShoot();
                }
            }
        }

        public void EndShoot()
        {
            if (_shooting)
            {
                SAINTrace.Log($"SHOOT EndShoot: {SAINTrace.Bot(Bot)}, {SAINTrace.Enemy(LastShotEnemy)}");
            }
            _shooting = false;
            BotOwner.ShootData?.EndShoot();
        }

        public Enemy GetEnemyToShoot(Enemy priorityEnemy = null)
        {
            if (AimAndShootAtEnemy(priorityEnemy, Bot))
            {
                UpdateADS(priorityEnemy);
                return priorityEnemy;
            }
            Enemy targetEnemy = CheckEnemiesForShootableTargets(Bot.EnemyController.VisibleEnemies);
            if (targetEnemy != null)
            {
                UpdateADS(targetEnemy);
                return targetEnemy;
            }
            UpdateADS(priorityEnemy);
            Bot.Aim.LoseAimTarget();
            return null;
        }

        public bool ShootAnyVisibleEnemies(Enemy priorityEnemy = null)
        {
            if (Bot.Decision.CurrentSelfDecision == ESelfActionType.Reload)
            {
                SAINTrace.LogThrottled($"shoot-skip-reload-{Bot.ProfileId}", 0.5f, $"SHOOT skipped reload decision: {SAINTrace.Bot(Bot)}, {SAINTrace.Enemy(priorityEnemy)}");
                return false;
            }
            if (Bot.Mover.Running && 
                (Bot.Mover.ActivePath.CurrentSprintStatus == Mover.EBotSprintStatus.Running || 
                Bot.Mover.ActivePath.CurrentSprintStatus == Mover.EBotSprintStatus.Turning))
            {
                SAINTrace.LogThrottled($"shoot-skip-sprint-{Bot.ProfileId}", 0.5f, $"SHOOT skipped sprinting: {SAINTrace.Bot(Bot)}, sprintStatus={Bot.Mover.ActivePath.CurrentSprintStatus}, {SAINTrace.Enemy(priorityEnemy)}");
                return false;
            }
            return GetEnemyToShoot(priorityEnemy) != null;
        }

        private void UpdateADS(Enemy enemy)
        {
            Bot.AimDownSightsController.UpdateADSstatus(enemy);
        }

        public Enemy CheckEnemiesForShootableTargets(EnemyList VisibleEnemies)
        {
            foreach (Enemy Enemy in VisibleEnemies)
                if (Enemy.IsVisible && Time.time - Enemy.Vision.LastChangeVisionTime > 0.33f && AimAndShootAtEnemy(Enemy, Bot))
                    return Enemy;
            return null;
        }

        private bool AimAndShootAtEnemy(Enemy Enemy, BotComponent bot)
        {
            if (Enemy == null)
            {
                return false;
            }

            if (Enemy.Player?.HealthController?.IsAlive == false)
            {
                SAINTrace.LogThrottled($"shoot-skip-dead-{bot.ProfileId}-{Enemy.EnemyProfileId}", 0.5f, $"SHOOT skipped dead enemy: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(Enemy)}");
                return false;
            }

            var weaponManager = bot.BotOwner.WeaponManager;
            if (weaponManager == null)
            {
                SAINTrace.LogThrottled($"shoot-skip-weapon-null-{bot.ProfileId}", 0.5f, $"SHOOT skipped weaponManager null: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(Enemy)}");
                return false;
            }

            bool reloading = weaponManager.Reload.Reloading;
            if (reloading || !weaponManager.HaveBullets)
            {
                SAINTrace.LogThrottled($"shoot-skip-ammo-{bot.ProfileId}", 0.5f, $"SHOOT skipped ammo/reload: {SAINTrace.Bot(bot)}, reloading={reloading}, haveBullets={weaponManager.HaveBullets}, slot={weaponManager.Selector?.EquipmentSlot}, {SAINTrace.Enemy(Enemy)}");
                if (!reloading && weaponManager.Selector.EquipmentSlot == EquipmentSlot.Holster && !weaponManager.Selector.TryChangeToMain())
                    SelectWeapon(Enemy);

                return false;
            }

            if (!bot.Aim.CanAim)
            {
                SAINTrace.LogThrottled($"shoot-skip-canaim-{bot.ProfileId}", 0.5f, $"SHOOT skipped CanAim false: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(Enemy)}");
                return false;
            }

            Vector3? target = GetAimTarget(Enemy, bot);
            if (target != null &&
                Enemy != null)
            {
                bot.BotLight.HandleLightForEnemy(Enemy);

                if (bot.Aim.AimAtTarget(target.Value, Enemy, out bool AimComplete, bot.BotOwner.AimingManager.CurrentAiming, bot))
                {
                    SAINTrace.LogThrottled($"shoot-aim-{bot.ProfileId}-{Enemy.EnemyProfileId}", 0.25f, $"SHOOT aiming: {SAINTrace.Bot(bot)}, target={SAINTrace.Vec(target.Value)}, aimComplete={AimComplete}, lastAimTime={bot.Aim.LastAimTime:0.00}, {SAINTrace.Enemy(Enemy)}");
                    ShootWhenAimComplete(Enemy, bot, AimComplete);
                    return true;
                }
                SAINTrace.LogThrottled($"shoot-aim-fail-{bot.ProfileId}-{Enemy.EnemyProfileId}", 0.5f, $"SHOOT AimAtTarget returned false: {SAINTrace.Bot(bot)}, target={SAINTrace.Vec(target.Value)}, {SAINTrace.Enemy(Enemy)}");
            }
            else
            {
                SAINTrace.LogThrottled($"shoot-no-target-{bot.ProfileId}-{Enemy.EnemyProfileId}", 0.5f, $"SHOOT no aim target: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(Enemy)}");
            }
            return false;
        }

        private void ShootWhenAimComplete(Enemy Enemy, BotComponent bot, bool AimComplete)
        {
            if (AimComplete)
            {
                var shootData = bot.BotOwner.ShootData;
                if (!shootData.Shooting)
                {
                    LastShotEnemy = Enemy;
                    _shooting = true;
                    SAINTrace.Log($"SHOOT trigger press: {SAINTrace.Bot(bot)}, {SAINTrace.Enemy(Enemy)}");
                    bot.BotOwner.ShootData.Shoot();
                    Enemy.EnemyInfo?.SetLastShootTime();
                }
            }
        }

        private void SelectWeapon(Enemy Enemy)
        {
            FindOptimalWeaponForDistance(Enemy.RealDistance);
            if (CurrentSlot != optimalSlot)
            {
                TryChangeWeapon(optimalSlot);
            }
        }

        private EquipmentSlot CurrentSlot => BotOwner.WeaponManager.Selector.EquipmentSlot;

        private void TryChangeWeapon(EquipmentSlot slot)
        {
            if (_nextChangeWeaponTime < Time.time)
            {
                var selector = BotOwner?.WeaponManager?.Selector;
                if (selector != null)
                {
                    _nextChangeWeaponTime = Time.time + 1f;
                    switch (slot)
                    {
                        case EquipmentSlot.FirstPrimaryWeapon:
                            selector.TryChangeToMain();
                            break;

                        case EquipmentSlot.SecondPrimaryWeapon:
                            selector.ChangeToSecond();
                            break;

                        case EquipmentSlot.Holster:
                            selector.TryChangeWeapon(true);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void FindOptimalWeaponForDistance(float distance)
        {
            if (_nextCheckOptimalTime < Time.time)
            {
                _nextCheckOptimalTime = Time.time + 0.5f;

                var equipment = Bot.PlayerComponent.Equipment;

                float? primaryEngageDist = null;
                var primary = equipment.PrimaryWeapon;
                if (IsWeaponDurableEnough(primary))
                {
                    primaryEngageDist = primary.EngagementDistance;
                }

                float? secondaryEngageDist = null;
                var secondary = equipment.SecondaryWeapon;
                if (IsWeaponDurableEnough(secondary))
                {
                    secondaryEngageDist = secondary.EngagementDistance;
                }

                float? holsterEngageDist = null;
                var holster = equipment.HolsterWeapon;
                if (IsWeaponDurableEnough(holster))
                {
                    holsterEngageDist = holster.EngagementDistance;
                }

                float minDifference = Mathf.Abs(distance - primaryEngageDist ?? 0);
                optimalSlot = EquipmentSlot.FirstPrimaryWeapon;

                float difference = Mathf.Abs(distance - secondaryEngageDist ?? 0);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    optimalSlot = EquipmentSlot.SecondPrimaryWeapon;
                }

                if (!BotOwner.WeaponManager.HaveBullets)
                {
                    difference = Mathf.Abs(distance - holsterEngageDist ?? 0);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        optimalSlot = EquipmentSlot.Holster;
                    }
                }
            }
        }

        private static bool IsWeaponDurableEnough(WeaponInfo info, float min = 0.5f) => info != null && info.Durability > min && info.Weapon.ChamberAmmoCount > 0;

        private static Vector3? GetAimTarget(Enemy enemy, BotComponent bot)
        {
            if (enemy != null &&
                enemy.IsVisible &&
                enemy.CanShoot)
            {
                //Vector3? test = enemy.Shoot.Targets.GetPointToShoot();
                //if (test == null) {
                //    Logger.LogWarning($"cant get point to shoot with new system! oh no!");
                //}

                Vector3? centerMass = FindCenterMassPoint(enemy, bot);
                Vector3? partToShoot = GetEnemyPartToShoot(enemy.EnemyInfo);
                Vector3? modifiedTarget = CheckYValue(centerMass, partToShoot);
                Vector3? finalTarget = modifiedTarget ?? partToShoot ?? centerMass;

                return finalTarget;
            }
            return null;
        }

        private static Vector3? CheckYValue(Vector3? centerMass, Vector3? partTarget)
        {
            if (centerMass != null &&
                partTarget != null &&
                centerMass.Value.y < partTarget.Value.y)
            {
                Vector3 newTarget = partTarget.Value;
                newTarget.y = centerMass.Value.y;
                return new Vector3?(newTarget);
            }
            return null;
        }

        private static Vector3? FindCenterMassPoint(Enemy enemy, BotComponent bot)
        {
            if (enemy.IsAI)
            {
                return null;
            }
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimCenterMassGlobal)
            {
                return null;
            }
            if (!bot.Info.FileSettings.Aiming.AimCenterMass)
            {
                return null;
            }
            if (bot.Info.FileSettings.Aiming.AimForHead)
            {
                return null;
            }
            return enemy.CenterMass;
        }

        private static Vector3? GetEnemyPartToShoot(EnemyInfo enemy)
        {
            if (enemy != null)
            {
                Vector3 value;
                if (enemy.Distance < 6f)
                {
                    value = enemy.CurrPosition + Vector3.up;
                }
                else
                {
                    value = enemy.GetPartToShoot();
                }
                return new Vector3?(value);
            }
            return null;
        }

        private bool _shooting;
        private EquipmentSlot optimalSlot;
        private float _nextCheckOptimalTime;
        private float _nextChangeWeaponTime;
    }
}
