using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.ElseHeartBreak
{
    /// <summary>
    /// Responsible for loading ElseHeartBreak core plugins
    /// </summary>
    public class ElseHeartBreakPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ElseHeartBreakCore) };
    }
}
