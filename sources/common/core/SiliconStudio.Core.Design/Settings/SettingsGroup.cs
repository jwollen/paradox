﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpYaml.Events;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Core.Settings
{
    public class SettingsGroup
    {
        /// <summary>
        /// A dictionary containing every existing <see cref="SettingsKey"/>.
        /// </summary>
        private readonly Dictionary<UFile, SettingsKey> settingsKeys = new Dictionary<UFile, SettingsKey>();

        /// <summary>
        /// A <see cref="SettingsProfile"/> that contains the default value of all registered <see cref="SettingsKey"/>.
        /// </summary>
        private readonly SettingsProfile defaultProfile;

        private readonly List<SettingsProfile> profileList = new List<SettingsProfile>();

        private SettingsProfile currentProfile;

        public SettingsGroup()
        {
            defaultProfile = new SettingsProfile(this, null);
            profileList.Add(defaultProfile);
            currentProfile = defaultProfile;
            Logger = new LoggerResult();
        }

        /// <summary>
        /// Gets the logger associated to the <see cref="SettingsGroup"/>.
        /// </summary>
        public LoggerResult Logger { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="SettingsProfile"/> that is currently active.
        /// </summary>
        public SettingsProfile CurrentProfile { get { return currentProfile; } set { ChangeCurrentProfile(currentProfile, value); } }

        /// <summary>
        /// Gets the list of registered profiles.
        /// </summary>
        public IEnumerable<SettingsProfile> Profiles { get { return profileList; } }

        /// <summary>
        /// Raised when a settings file has been loaded.
        /// </summary>
        public event EventHandler<SettingsFileLoadedEventArgs> SettingsFileLoaded;

        /// <summary>
        /// Gets a list of all registered <see cref="SettingsKey"/> instances.
        /// </summary>
        /// <returns>A list of all registered <see cref="SettingsKey"/> instances.</returns>
        public List<SettingsKey> GetAllSettingsKeys()
        {
            return settingsKeys.Values.ToList();
        }

        /// <summary>
        /// Creates a new settings profile.
        /// </summary>
        /// <param name="setAsCurrent">If <c>true</c>, the created profile will also be set as <see cref="CurrentProfile"/>.</param>
        /// <param name="parent">The parent profile of the settings to create. If <c>null</c>, a default profile will be used.</param>
        /// <returns>A new instance of the <see cref="SettingsProfile"/> class.</returns>
        public SettingsProfile CreateSettingsProfile(bool setAsCurrent, SettingsProfile parent = null)
        {
            var profile = new SettingsProfile(this, parent ?? defaultProfile);
            profileList.Add(profile);
            if (setAsCurrent)
                CurrentProfile = profile;

            return profile;
        }

        /// <summary>
        /// Loads a settings profile from the given file.
        /// </summary>
        /// <param name="filePath">The path of the file from which to load settings.</param>
        /// <param name="setAsCurrent">If <c>true</c>, the loaded profile will also be set as <see cref="CurrentProfile"/>.</param>
        /// <param name="parent">The profile to use as parent for the loaded profile. If <c>null</c>, a default profile will be used.</param>
        /// <returns><c>true</c> if settings were correctly loaded, <c>false</c> otherwise.</returns>
        public SettingsProfile LoadSettingsProfile(UFile filePath, bool setAsCurrent, SettingsProfile parent = null)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");

            if (!File.Exists(filePath))
            {
                Logger.Error("Settings file [{0}] was not found", filePath);
                return null;
            }

            SettingsProfile profile;
            try
            {
                SettingsFile settingsFile;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    settingsFile = (SettingsFile)YamlSerializer.Deserialize(stream);
                }
                profile = new SettingsProfile(this, parent ?? defaultProfile) { FilePath = filePath };

                foreach (var settings in settingsFile.Settings)
                {
                    SettingsKey key;
                    var value = settings.Value;
                    object finalValue = value;
                    if (settingsKeys.TryGetValue(settings.Key, out key))
                    {
                        finalValue = key.ConvertValue(value);
                    }
                    profile.SetValue(settings.Key, finalValue);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error while loading settings file [{0}]: {1}", e, filePath, e.FormatForReport());
                return null;
            }

            profileList.Add(profile);
            if (setAsCurrent)
            {
                CurrentProfile = profile;
            }
            
            var handler = SettingsFileLoaded;
            if (handler != null)
            {
                SettingsFileLoaded(null, new SettingsFileLoadedEventArgs(filePath));
            }
            return profile;
        }

        /// <summary>
        /// Reloads a profile from its file, updating the value that have changed.
        /// </summary>
        /// <param name="profile">The profile to reload.</param>
        public void ReloadSettingsProfile(SettingsProfile profile)
        {
            var filePath = profile.FilePath;
            if (filePath == null) throw new ArgumentException("profile");
            if (!File.Exists(filePath))
            {
                Logger.Error("Settings file [{0}] was not found", filePath);
                throw new ArgumentException("profile");
            }

            try
            {
                SettingsFile settingsFile;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    settingsFile = (SettingsFile)YamlSerializer.Deserialize(stream);
                }

                foreach (var settings in settingsFile.Settings)
                {
                    SettingsKey key;
                    var value = settings.Value;
                    object finalValue = value;
                    if (settingsKeys.TryGetValue(settings.Key, out key))
                    {
                        finalValue = key.ConvertValue(value);
                    }
                    profile.SetValue(settings.Key, finalValue);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error while loading settings file [{0}]: {1}", e, filePath, e.FormatForReport());
            }

            var handler = SettingsFileLoaded;
            if (handler != null)
            {
                SettingsFileLoaded(null, new SettingsFileLoadedEventArgs(filePath));
            }
        }

        /// <summary>
        /// Unloads a profile that was previously loaded.
        /// </summary>
        /// <param name="profile">The profile to unload.</param>
        public void UnloadSettingsProfile(SettingsProfile profile)
        {
            if (profile == defaultProfile)
                throw new ArgumentException("The default profile cannot be unloaded");
            if (profile == CurrentProfile)
                throw new InvalidOperationException("Unable to unload the current profile.");
            profileList.Remove(profile);
        }

        /// <summary>
        /// Saves the given settings profile to a file at the given path.
        /// </summary>
        /// <param name="profile">The profile to save.</param>
        /// <param name="filePath">The path of the file.</param>
        /// <returns><c>true</c> if the file was correctly saved, <c>false</c> otherwise.</returns>
        public bool SaveSettingsProfile(SettingsProfile profile, UFile filePath)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            try
            {
                profile.Saving = true;
                Directory.CreateDirectory(filePath.GetFullDirectory());

                var settingsFile = new SettingsFile();
                foreach (var entry in profile.Settings.Values)
                {
                    try
                    {
                        // Find key
                        SettingsKey key;
                        settingsKeys.TryGetValue(entry.Name, out key);
                        settingsFile.Settings.Add(entry.Name, entry.GetSerializableValue(key));
                    }
                    catch (Exception)
                    {
                    }
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    YamlSerializer.Serialize(stream, settingsFile);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error while saving settings file [{0}]: {1}", e, filePath, e.FormatForReport());
                return false;
            }
            finally
            {
                profile.Saving = false;
            }
            return true;
        }

        /// <summary>
        /// Gets the settings key that matches the given name.
        /// </summary>
        /// <param name="name">The name of the settings property to fetch.</param>
        /// <returns>The settings key that matches the given name, or <c>null</c>.</returns>
        public SettingsKey GetSettingsKey(UFile name)
        {
            SettingsKey key;
            settingsKeys.TryGetValue(name, out key);
            return key;
        }

        /// <summary>
        /// Clears the current settings, including registered <see cref="SettingsKey"/> and <see cref="SettingsProfile"/> instances. This method should be used only for tests.
        /// </summary>
        public void ClearSettings()
        {
            CurrentProfile = defaultProfile;
            CurrentProfile.ValidateSettingsChanges();
            profileList.Clear();
            defaultProfile.Settings.Clear();
            settingsKeys.Clear();
        }
        
        internal void RegisterSettingsKey(UFile name, object defaultValue, SettingsKey settingsKey)
        {
            settingsKeys.Add(name, settingsKey);
            var entry = SettingsEntry.CreateFromValue(defaultProfile, name, defaultValue);
            defaultProfile.RegisterEntry(entry);
            // Ensure that the value is converted to the key type in each loaded profile.
            foreach (var profile in Profiles.Where(x => x != defaultProfile))
            {
                if (profile.Settings.TryGetValue(name, out entry))
                {
                    var parsingEvents = entry.Value as List<ParsingEvent>;
                    var convertedValue = parsingEvents != null ? settingsKey.ConvertValue(parsingEvents) : entry.Value;
                    entry = SettingsEntry.CreateFromValue(profile, name, convertedValue);
                    profile.Settings[name] = entry;
                }
            }
        }

        private void ChangeCurrentProfile(SettingsProfile oldProfile, SettingsProfile newProfile)
        {
            if (oldProfile == null) throw new ArgumentNullException("oldProfile");
            if (newProfile == null) throw new ArgumentNullException("newProfile");
            currentProfile = newProfile;

            foreach (var key in settingsKeys)
            {
                object oldValue;
                oldProfile.GetValue(key.Key, out oldValue, true, false);
                object newValue;
                newProfile.GetValue(key.Key, out newValue, true, false);
                var oldList = oldValue as IList;
                var newList = newValue as IList;

                bool isDifferent;
                if (oldList != null && newList != null)
                {
                    isDifferent = oldList.Count != newList.Count;
                    for (int i = 0; i < oldList.Count && !isDifferent; ++i)
                    {
                        if (!Equals(oldList[i], newList[i]))
                            isDifferent = true;
                    }
                }
                else
                {
                    isDifferent = !Equals(oldValue, newValue);
                }
                if (isDifferent)
                {
                    newProfile.NotifyEntryChanged(key.Key);
                }
            }

            // Changes have been notified, empty the list of modified settings.
            newProfile.ValidateSettingsChanges();
        }
    }
}