﻿using IndiegalaLibrary.Services;
using IndiegalaLibrary.Views;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IndiegalaLibrary
{
    public class IndiegalaLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private IndiegalaLibrarySettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("f7da6eb0-17d7-497c-92fd-347050914954");

        // Change to something more appropriate
        public override string Name => "Indiegala";

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; } = new IndiegalaLibraryClient();

        private const string dbImportMessageId = "indiegalalibImportError";

        private IndiegalaAccountClient IndiegalaApi;

        public IndiegalaLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new IndiegalaLibrarySettings(this);

            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();
            
                if (cv.Check("IndiegalaLibrary", pluginFolder))
                {
                    cv.ShowNotification(api, "IndiegalaLibrary - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }
        }


        public override IEnumerable<GameInfo> GetGames()
        {

            List<GameInfo> allGames = new List<GameInfo>();
            Dictionary<string, GameInfo> installedGames = new Dictionary<string, GameInfo>();
            Exception importError = null;

            if (IndiegalaApi == null)
            {
                var view = PlayniteApi.WebViews.CreateOffscreenView();
                IndiegalaApi = new IndiegalaAccountClient(view);
            }

            //if (settings.ImportInstalledGames)
            //{
            //    try
            //    {
            //        installedGames = GetInstalledGames();
            //        logger.Debug($"Found {installedGames.Count} installed Indiegala games.");
            //        allGames.AddRange(installedGames.Values.ToList());
            //    }
            //    catch (Exception e)
            //    {
            //        logger.Error(e, "Failed to import installed Indiegala games.");
            //        importError = e;
            //    }
            //}

            if (IndiegalaApi.GetIsUserLoggedIn())
            {
                try
                {
                    allGames = IndiegalaApi.GetOwnedGames();
                    logger.Debug($"Found {allGames.Count} library Indiegala games.");
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to import linked account Indiegala games details.");
                    importError = e;
                }
            }

            if (importError != null)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    dbImportMessageId,
                    string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), Name) +
                    System.Environment.NewLine + importError.Message,
                    NotificationType.Error,
                    () => OpenSettingsView()));
            }
            else
            {
                PlayniteApi.Notifications.Remove(dbImportMessageId);
            }

            return allGames;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new IndiegalaLibrarySettingsView(PlayniteApi, settings, IndiegalaApi);
        }
    }
}