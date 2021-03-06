﻿using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Ext.Unity.Plugins;

using UnityEngine;

namespace Oxide.Ext.Unity
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class UnityExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Unity";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        /// <summary>
        /// Initializes a new instance of the UnityExtension class
        /// </summary>
        /// <param name="manager"></param>
        public UnityExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new UnityPluginLoader());

            // Register engine clock
            Interface.Oxide.RegisterEngineClock(() => Time.realtimeSinceStartup);

            // Configure remote logging
            RemoteLogger.SetTag("os", SystemInfo.operatingSystem);
            RemoteLogger.SetTag("cpu", SystemInfo.processorType);

            // Register our MonoBehaviour
            UnityScript.Create();
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {

        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {

        }
    }
}
