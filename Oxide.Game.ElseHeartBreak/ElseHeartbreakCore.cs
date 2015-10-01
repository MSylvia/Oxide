using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.ElseHeartBreak
{
    /// <summary>
    /// The core ElseHeartBreak plugin
    /// </summary>
    public class ElseHeartBreakCore : CSPlugin
    {
        // Track when the server has been initialized
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the ElseHeartBreakCore class
        /// </summary>
        public ElseHeartBreakCore()
        {
            // Set attributes
            Name = "elseheartbreakcore";
            Title = "ElseHeartBreak Core";
            Author = "MSylvia";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "elseheartbreak");
        }

        /// <summary>
        /// Finished loading game
        /// </summary>
        [HookMethod("RunGameWorld_OnLoadedGame")]
        private void RunGameWorld_OnLoadedGame()
        {
            RunGameWorld.instance.ShowNotification("Mod Loaded");
            UnityEngine.Debug.Log("--------------------------------------------------");
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            plugin.CallHook("Init");
            if (!loggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }
    }
}
