using EFT;
using SAIN.Classes.Transform;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotLookClass : BotBase
    {
        public SAINBotLookClass(BotComponent component) : base(component)
        {
            LookData = new LookAllData();
        }

        public readonly LookAllData LookData;

        // This (And the methods it calls) mirrors a large part of BSG's LookSensor
        // Look at that for potential changes between versions
        public int UpdateLook(float currentTime)
        {
            if (BotOwner.LeaveData == null || BotOwner.LeaveData.LeaveComplete)
            {
                return 0;
            }

            int numUpdated = UpdateLookForEnemies(LookData, currentTime, Bot);
            UpdateLookData(LookData);
            return numUpdated;
        }

        public void UpdateLookData(LookAllData lookData)
        {
            for (int i = 0; i < lookData.ReportsData.Count; i++)
            {
                ReportAiData report = lookData.ReportsData[i];
                BotOwner.BotsGroup.ReportAboutEnemy(report.Enemy, report.VisibleOnlyBySence, BotOwner);
            }

            if (lookData.ReportsData.Count > 0)
            {
                BotOwner.Memory.SetLastTimeSeeEnemy();
            }

            lookData.Reset();
        }

        private static int UpdateLookForEnemies(LookAllData lookAll, float currentTime, BotComponent bot)
        {
            int updated = 0;
            var lookSensor = bot.BotOwner.LookSensor;
            var transform = bot.Transform;

            lookSensor._weaponRootPoint = transform.WeaponRoot;
            lookSensor._lookSensorShootPosition.UpdateShootPosition(transform.WeaponRoot);
            lookSensor.HeadPoint = transform.EyePosition;

            lookAll.Reset();
            var enemies = bot.EnemyController.EnemiesArray;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.ShallCheckLook(currentTime, out float deltaTime))
                {
                    enemy.EnemyInfo.CheckLookEnemy(lookAll, deltaTime);
                    updated++;
                }
            }
            return updated;
        }

        private void SetNotVis(Enemy enemy)
        {
            if (enemy.EnemyInfo.IsVisible)
            {
                enemy.EnemyInfo.SetVisible(false);
            }
        }
    }
}
