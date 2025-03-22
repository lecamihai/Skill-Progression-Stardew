using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Linq;

namespace SkillProgressionMod
{
    public class DisplayManager
    {
        private class SkillDisplayInfo
        {
            public string SkillName;
            public int CurrentLevel;
            public int Progress;
            public int Required;
            public DateTime DisplayUntil;
            public DateTime StartTime;
        }

        private readonly Dictionary<int, SkillDisplayInfo> skillDisplays = new();

        public static Color HexToColor(string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int rgb))
                {
                    return new Color(
                        (byte)((rgb >> 16) & 0xFF),
                        (byte)((rgb >> 8) & 0xFF),
                        (byte)(rgb & 0xFF)
                    );
                }
            }
            catch { }
            return Color.White;
        }

        private Rectangle GetSkillSourceRect(int skillIndex)
        {
            return skillIndex switch
            {
                0 => new Rectangle(0, 0, 16, 16),
                1 => new Rectangle(16, 0, 16, 16),
                2 => new Rectangle(80, 0, 16, 16),
                3 => new Rectangle(32, 0, 16, 16),
                4 => new Rectangle(128, 16, 16, 16),
                5 => new Rectangle(64, 0, 16, 16),
                _ => Rectangle.Empty
            };
        }

        public void UpdateSkillDisplay(int skillIndex, string skillName, int currentLevel, int progress, int required)
        {
            // Check if we have an existing entry for this skill
            if (skillDisplays.TryGetValue(skillIndex, out SkillDisplayInfo existing))
            {
                // Update existing entry if it's from the last 0.5 seconds
                if ((DateTime.Now - existing.StartTime).TotalSeconds <= 0.5)
                {
                    // Accumulate XP gains happening rapidly
                    existing.Progress = progress;
                    existing.Required = required;
                    existing.DisplayUntil = DateTime.Now.AddSeconds(3);
                    return;
                }
            }

            // Create new entry if no recent one exists
            skillDisplays[skillIndex] = new SkillDisplayInfo
            {
                SkillName = skillName,
                CurrentLevel = currentLevel,
                Progress = progress,
                Required = required,
                DisplayUntil = DateTime.Now.AddSeconds(3),
                StartTime = DateTime.Now
            };
        }

        public void Draw(SpriteBatch spriteBatch, ModEntry.ModConfig config)
        {
            int yOffset = 0;
            Vector2 positionBase = new Vector2(50, 100);

            foreach (var kvp in skillDisplays.ToList())
            {
                int skillIndex = kvp.Key;
                SkillDisplayInfo entry = kvp.Value;

                float timeSinceStart = (float)(DateTime.Now - entry.StartTime).TotalSeconds;
                float fadeProgress = Math.Clamp(timeSinceStart / 3f, 0f, 1f);
                float alpha = 1f;

                if (fadeProgress < 0.2f)
                {
                    alpha = EaseOutQuad(fadeProgress / 0.2f);
                }
                else if (fadeProgress > 0.8f)
                {
                    alpha = EaseInQuad(1 - ((fadeProgress - 0.8f) / 0.2f));
                }

                if (DateTime.Now >= entry.DisplayUntil)
                {
                    skillDisplays.Remove(skillIndex);
                    continue;
                }

                string xpText = entry.Required == 0
                    ? "Max Level"
                    : $"{entry.Progress}/{entry.Required}";

                Color textColor = HexToColor(config.TextColor) * alpha;
                Color iconColor = Color.White * alpha;

                Rectangle sourceRect = GetSkillSourceRect(skillIndex);
                float iconBaseScale = 2f;
                float textScale = config.FontSize;
                float iconScale = iconBaseScale * config.FontSize;
                float iconHeight = sourceRect.Height * iconScale;
                float iconWidth = sourceRect.Width * iconScale;

                Vector2 iconPosition = new Vector2(
                    positionBase.X,
                    positionBase.Y + (yOffset * (iconHeight + 5))
                );

                spriteBatch.Draw(
                    Game1.buffsIcons,
                    iconPosition,
                    sourceRect,
                    iconColor,
                    0f,
                    Vector2.Zero,
                    iconScale,
                    SpriteEffects.None,
                    1f
                );

                Vector2 textPosition = new Vector2(
                    iconPosition.X + iconWidth + 10,
                    iconPosition.Y + (iconHeight - Game1.dialogueFont.MeasureString(xpText).Y * textScale) / 2
                );

                spriteBatch.DrawString(
                    Game1.dialogueFont,
                    xpText,
                    textPosition,
                    textColor,
                    0f,
                    Vector2.Zero,
                    textScale,
                    SpriteEffects.None,
                    1f
                );

                yOffset++;
            }
        }

        private float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }

        private float EaseInQuad(float t)
        {
            return t * t;
        }
    }
}