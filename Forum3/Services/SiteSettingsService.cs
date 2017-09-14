﻿using System;
using System.Collections.Generic;
using System.Linq;
using DataModels = Forum3.Models.DataModels;

namespace Forum3.Services {
	public class SiteSettingsService {
		DataModels.ApplicationDbContext DbContext { get; }

		public SiteSettingsService(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

		string GetSetting(string name, string userId = "") {
			DataModels.SiteSetting setting = null;
			var settingValue = string.Empty;

			if (string.IsNullOrEmpty(userId)) {
				if (!Settings.ContainsKey(name)) {
					setting = DbContext.SiteSettings.FirstOrDefault(r => r.Name == name && r.UserId == userId);

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting == null ? "" : setting.Value);
					}
				}

				Settings.TryGetValue(name, out settingValue);
			}
			else {
				setting = DbContext.SiteSettings.FirstOrDefault(r => r.Name == name && r.UserId == userId);

				if (setting != null)
					settingValue = setting.Value;
			}

			return settingValue;
		}

		public string Get(string name, string userId = "") {
			return GetSetting(name, userId);
		}

		public int GetInt(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return 0;

			return Convert.ToInt32(setting);
		}

		public bool GetBool(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return false;

			return Convert.ToBoolean(setting);
		}
	}
}