using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Helpers
{
    public static class SAINTrace
    {
        public static bool Enabled = true;

        private static readonly Dictionary<string, float> NextLogTimes = new Dictionary<string, float>();

        public static void Log(string message)
        {
            if (!Enabled)
            {
                return;
            }

            SAINPlugin.WriteStartupLog("[TRACE] " + message);
        }

        public static void LogThrottled(string key, float interval, string message)
        {
            if (!ShouldLog(key, interval))
            {
                return;
            }

            Log(message);
        }

        public static bool ShouldLog(string key, float interval)
        {
            if (!Enabled)
            {
                return false;
            }

            float now = Time.realtimeSinceStartup;
            if (NextLogTimes.TryGetValue(key, out float nextTime) && nextTime > now)
            {
                return false;
            }

            NextLogTimes[key] = now + interval;
            return true;
        }

        public static string Bot(BotComponent bot)
        {
            if (bot == null)
            {
                return "bot=null";
            }

            string role = Safe(() => bot.BotOwner?.Profile?.Info?.Settings?.Role.ToString(), "role=null");
            return $"bot={bot.name}, profile={bot.ProfileId}, {role}, active={bot.BotActive}, standby={bot.BotInStandBy}, layers={bot.SAINLayersActive}, layer={bot.ActiveLayer}, action={bot.CurrentAction?.Name}";
        }

        public static string Enemy(Enemy enemy)
        {
            if (enemy == null)
            {
                return "enemy=null";
            }

            string nickname = Safe(() => enemy.EnemyPlayer?.Profile?.Nickname, "name=null");
            string role = Safe(() => enemy.EnemyPlayer?.Profile?.Info?.Settings?.Role.ToString(), "role=null");
            return $"enemy={enemy.EnemyProfileId}, {nickname}, {role}, dist={enemy.RealDistance:0.0}, visible={enemy.IsVisible}, canShoot={enemy.CanShoot}, known={enemy.EnemyKnown}";
        }

        public static string Vec(Vector3 value)
        {
            return $"({value.x:0.0},{value.y:0.0},{value.z:0.0})";
        }

        private static string Safe(Func<string> valueFactory, string fallback)
        {
            try
            {
                string value = valueFactory();
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
