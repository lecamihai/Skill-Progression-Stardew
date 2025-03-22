//ModEntry.cs
using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using GenericModConfigMenu;
using System.Collections.Generic;

namespace SkillProgressionMod
{
    public class ModEntry : Mod
{
        private DisplayManager displayManager;
        private ModConfig config;
        private static ModEntry Instance;
        private SaveData saveData;

        public class SaveData
        {
            public Dictionary<int, int> MaxLevelShownDays { get; set; } = new Dictionary<int, int>();
        }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            config = helper.ReadConfig<ModConfig>();
            displayManager = new DisplayManager();

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(PostfixGainExperience))
            );

            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveData = Helper.Data.ReadSaveData<SaveData>("maxLevelShownDays") ?? new SaveData();
            if (saveData.MaxLevelShownDays == null)
                saveData.MaxLevelShownDays = new Dictionary<int, int>();
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("maxLevelShownDays", saveData);
        }


        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => config = new ModConfig(),
                save: () => Helper.WriteConfig(config)
            );

            // Text Color Option
            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Text Color",
                tooltip: () => "Hex color code (e.g., #FF5733)",
                getValue: () => config.TextColor,
                setValue: value => config.TextColor = value
            );

            // Font Size Option
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Font Size",
                tooltip: () => "Size multiplier (0.5-3.0)",
                getValue: () => config.FontSize,
                setValue: value => config.FontSize = value,
                min: 0.5f,
                max: 3f,
                interval: 0.1f
            );

            // Preview Text Inside Config Menu
            configMenu.AddComplexOption(
                mod: ModManifest,
                name: () => "Preview",
                draw: (SpriteBatch sb, Vector2 pos) => {
                    string previewText = "Foraging: 30/200 (Level 2)";
                    Color textColor = DisplayManager.HexToColor(config.TextColor);
                    float scale = config.FontSize;

                    sb.DrawString(
                        Game1.dialogueFont,
                        previewText,
                        new Vector2(pos.X + 16, pos.Y + 8), // Offset within menu
                        textColor,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        1f
                    );
                },
                height: () => (int)(Game1.dialogueFont.MeasureString("X").Y * config.FontSize) + 16
            );
        }

        private static void PostfixGainExperience(Farmer __instance, int which, int howMuch)
        {
            try
            {
                if (Instance == null || Instance.displayManager == null || __instance == null || !__instance.IsLocalPlayer || which == 5)
                    return;

                int currentXP = __instance.experiencePoints[which];
                int currentLevel = __instance.GetUnmodifiedSkillLevel(which);
                bool isMaxLevel = currentLevel >= 10;

                int currentLevelXP = Farmer.getBaseExperienceForLevel(currentLevel);
                int nextLevelXP = isMaxLevel ? -1 : Farmer.getBaseExperienceForLevel(currentLevel + 1);

                int progress = isMaxLevel ? 0 : currentXP - currentLevelXP;
                int required = isMaxLevel ? 0 : (nextLevelXP != -1 ? nextLevelXP - currentLevelXP : 0);

                string skillName = Farmer.getSkillDisplayNameFromIndex(which);
                bool shouldShow = true;

                if (isMaxLevel)
                {
                    int currentDay = Game1.Date.TotalDays;
                    if (Instance.saveData.MaxLevelShownDays.TryGetValue(which, out int lastShownDay))
                    {
                        if (currentDay == lastShownDay)
                        {
                            shouldShow = false;
                        }
                        else
                        {
                            Instance.saveData.MaxLevelShownDays[which] = currentDay;
                        }
                    }
                    else
                    {
                        Instance.saveData.MaxLevelShownDays.Add(which, currentDay);
                    }
                }

                if (shouldShow)
                {
                    Instance.displayManager.UpdateSkillDisplay(
                        which,
                        skillName,
                        currentLevel,
                        progress,
                        required
                    );
                }
            }
            catch (Exception ex)
            {
                Instance?.Monitor?.Log($"Error in PostfixGainExperience: {ex}", LogLevel.Error);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            displayManager.Draw(e.SpriteBatch, config);
        }

        public class ModConfig
        {
            public string TextColor { get; set; } = "#FFFFFF";
            public float FontSize { get; set; } = 1f;
        }
    }
}