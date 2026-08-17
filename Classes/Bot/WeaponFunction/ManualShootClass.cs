using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class ManualShootClass : BotComponentClassBase
    {
        public ManualShootClass(BotComponent bot) : base(bot)
        {
            TickRequirement = ESAINTickState.OnlyNoSleep;
        }

        public override void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += CheckClearEnemy;
            base.Init();
        }

        public override void ManualUpdate()
        {
            CheckReset();
            base.ManualUpdate();
        }

        public override void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= CheckClearEnemy;
            base.Dispose();
        }

        private Enemy ManualShootEnemy;

        private void CheckClearEnemy(string ID, Enemy Enemy)
        {
            if (Enemy == ManualShootEnemy)
            {
                Reset();
            }
        }

        public void Reset()
        {
            if (Reason != EShootReason.None)
            {
                SAINTrace.Log($"MANUALSHOOT reset: {SAINTrace.Bot(Bot)}, reason={Reason}, pos={SAINTrace.Vec(ShootPosition)}, {SAINTrace.Enemy(ManualShootEnemy)}");
            }
            BotOwner.ShootData.EndShoot();
            Reason = EShootReason.None;
            ShootPosition = Vector3.zero;
            ManualShootEnemy = null;
        }

        private void CheckReset()
        {
            if (Reason != EShootReason.None && (ManualShootEnemy?.EnemyPlayer?.HealthController?.IsAlive != true || !BotOwner.WeaponManager.HaveBullets || _timeStartManualShoot + 2f < Time.time))
            {
                Reset();
            }
        }

        public bool TryShoot(Enemy Enemy, Vector3 targetPos, bool checkFF = true, EShootReason reason = EShootReason.None)
        {
            if (Enemy != null &&
                CanShoot(checkFF) &&
                Bot.Steering.AngleToPointFromLookDir(targetPos) <= 10 &&
                Bot.FriendlyFire.UpdateFriendlyFireStatus(targetPos, Bot.Transform.WeaponData.FirePort, Bot.Transform.WeaponData.PointDirection, Bot))
            {
                if (!Shooting)
                {
                    SAINTrace.Log($"MANUALSHOOT trigger attempt: {SAINTrace.Bot(Bot)}, reason={reason}, pos={SAINTrace.Vec(targetPos)}, {SAINTrace.Enemy(Enemy)}");
                    if (BotOwner.ShootData.Shoot())
                    {
                        _timeStartManualShoot = Time.time;
                        SAINTrace.Log($"MANUALSHOOT trigger accepted: {SAINTrace.Bot(Bot)}, reason={reason}, pos={SAINTrace.Vec(targetPos)}, {SAINTrace.Enemy(Enemy)}");
                    }
                    else
                    {
                        SAINTrace.Log($"MANUALSHOOT trigger rejected by ShootData: {SAINTrace.Bot(Bot)}, reason={reason}, pos={SAINTrace.Vec(targetPos)}, {SAINTrace.Enemy(Enemy)}");
                        return false;
                    }
                }

                ManualShootEnemy = Enemy;
                ShootPosition = targetPos;
                Reason = reason;
                return true;
            }
            SAINTrace.LogThrottled($"manualshoot-skip-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT skipped: {SAINTrace.Bot(Bot)}, reason={reason}, pos={SAINTrace.Vec(targetPos)}, angle={Bot.Steering.AngleToPointFromLookDir(targetPos):0.0}, clearShot={Bot.FriendlyFire.ClearShot}, {SAINTrace.Enemy(Enemy)}");
            Reset();
            return false;
        }

        public bool Shooting => BotOwner.ShootData.Shooting;

        public bool CanShoot(bool checkFF = true)
        {
            if (checkFF && !Bot.FriendlyFire.ClearShot)
            {
                //BotOwner.ShootData.EndShoot();
                //return false;
            }
            BotWeaponManager weaponManager = BotOwner.WeaponManager;
            if (weaponManager.IsMelee)
            {
                SAINTrace.LogThrottled($"manualshoot-cant-melee-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT CanShoot false melee: {SAINTrace.Bot(Bot)}");
                return false;
            }
            if (!weaponManager.IsWeaponReady)
            {
                SAINTrace.LogThrottled($"manualshoot-cant-ready-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT CanShoot false weapon not ready: {SAINTrace.Bot(Bot)}");
                return false;
            }
            if (weaponManager.Reload.Reloading)
            {
                SAINTrace.LogThrottled($"manualshoot-cant-reload-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT CanShoot false reloading: {SAINTrace.Bot(Bot)}");
                return false;
            }
            if (!BotOwner.ShootData.CanShootByState)
            {
                SAINTrace.LogThrottled($"manualshoot-cant-state-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT CanShoot false CanShootByState: {SAINTrace.Bot(Bot)}");
                return false;
            }
            if (!weaponManager.HaveBullets)
            {
                SAINTrace.LogThrottled($"manualshoot-cant-bullets-{Bot.ProfileId}", 0.5f, $"MANUALSHOOT CanShoot false no bullets: {SAINTrace.Bot(Bot)}");
                return false;
            }
            return true;
        }

        private float _timeStartManualShoot;

        public Vector3 ShootPosition { get; set; }

        public EShootReason Reason { get; private set; }
    }
}
