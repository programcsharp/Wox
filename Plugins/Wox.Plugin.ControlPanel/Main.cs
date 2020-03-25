﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;

namespace Wox.Plugin.ControlPanel
{
    public class Main : IPlugin, IPluginI18n, ISavable, ISettingProvider
    {
        private PluginInitContext context;
        private List<ControlPanelItem> controlPanelItems = new List<ControlPanelItem>();
        private string iconFolder;
        private string fileType;
        internal static Settings _settings { get; set; }
        private readonly PluginJsonStorage<Settings> _settingsStorage;

        public Main()
        {
            _settingsStorage = new PluginJsonStorage<Settings>();
            _settings = _settingsStorage.Load();
        }

        public void Init(PluginInitContext context)
        {
            this.context = context;
            controlPanelItems = ControlPanelList.Create(48);
            iconFolder = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, @"Images\ControlPanelIcons\");
            fileType = ".bmp";

            if (!Directory.Exists(iconFolder))
            {
                Directory.CreateDirectory(iconFolder);
            }

            foreach (ControlPanelItem item in controlPanelItems)
            {
                if (!File.Exists(iconFolder + item.GUID + fileType) && item.Icon != null)
                {
                    item.Icon.ToBitmap().Save(iconFolder + item.GUID + fileType);
                }
            }
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();

            foreach (var item in controlPanelItems)
            {
                var titleMatch = StringMatcher.FuzzySearch(query.Search, item.LocalizedString);
                var subTitleMatch = StringMatcher.FuzzySearch(query.Search, item.InfoTip);
                
                item.Score = Math.Max(titleMatch.Score, subTitleMatch.Score);
                if (item.Score > 0)
                {
                    var result = new Result
                    {
                        Title = item.LocalizedString,
                        SubTitle = item.InfoTip,
                        Score = item.Score,
                        IcoPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory,
                            @"Images\\ControlPanelIcons\\" + item.GUID + fileType),
                        Action = e =>
                        {
                            try
                            {
                                Process.Start(item.ExecutablePath);
                            }
                            catch (Exception)
                            {
                                //Silently Fail for now.. todo
                            }
                            return true;
                        }
                    };

                    if (item.Score == titleMatch.Score)
                    {
                        result.TitleHighlightData = _settings.EnableHighlightData
                                                        ? titleMatch.MatchData
                                                        : null;
                    }
                    else
                    {
                        result.SubTitleHighlightData = _settings.EnableHighlightData
                                                       ? subTitleMatch.MatchData
                                                       : null ;
                    }

                    results.Add(result);
                }
            }

            List<Result> panelItems = results.OrderByDescending(o => o.Score).Take(5).ToList();
            return panelItems;
        }

        public string GetTranslatedPluginTitle()
        {
            return context.API.GetTranslation("wox_plugin_controlpanel_plugin_name");
        }

        public string GetTranslatedPluginDescription()
        {
            return context.API.GetTranslation("wox_plugin_controlpanel_plugin_description");
        }

        public void Save()
        {
            _settingsStorage.Save();
        }

        public Control CreateSettingPanel()
        {
            return new ControlPanelSettings();
        }
    }
}