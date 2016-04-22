﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using uLink;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.HideHoldOut.Libraries;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// The core Hide & Hold Out plugin
    /// </summary>
    public class HideHoldOutCore : CSPlugin
    {
        #region Setup

        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        #region Localization

        // The language library
        private readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        private readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"CommandUsageLoad", "Usage: load *|<pluginname>+"},
            {"CommandUsageGrant", "Usage: grant <group|user> <name|id> <permission>"},
            {"CommandUsageGroup", "Usage: group <add|remove|set> <name> [title] [rank]"},
            {"CommandUsageReload", "Usage: reload *|<pluginname>+"},
            {"CommandUsageRevoke", "Usage: revoke <group|user> <name|id> <permission>"},
            {"CommandUsageShow", "Usage: show <group|user> <name>\nUsage: show <groups|perms>"},
            {"CommandUsageUnload", "Usage: unload *|<pluginname>+"},
            {"CommandUsageUserGroup", "Usage: usergroup <add|remove> <username> <groupname>"},
            {"GroupAlreadyExists", "Group '{0}' already exists"},
            {"GroupChanged", "Group '{0}' changed"},
            {"GroupCreated", "Group '{0}' created"},
            {"GroupDeleted", "Group '{0}' deleted"},
            {"GroupNotFound", "Group '{0}' doesn't exist"},
            {"GroupParentChanged", "Group '{0}' parent changed to '{1}'"},
            {"GroupParentNotChanged", "Group '{0}' parent was not changed"},
            {"GroupParentNotFound", "Group parent '{0}' doesn't exist"},
            {"GroupPermissionGranted", "Group '{0}' granted permission '{1}'"},
            {"GroupPermissionRevoked", "Group '{0}' revoked permission '{1}'"},
            {"NoPluginsFound", "No plugins are currently available"},
            {"PermissionNotFound", "Permission '{0}' doesn't exist"},
            {"PermissionsNotLoaded", "Unable to load permission files! Permissions will not work until resolved.\n => {0}"},
            {"PlayerLanguage", "Player language set to {0}"},
            {"PluginNotLoaded", "Plugin '{0}' not loaded."},
            {"PluginReloaded", "Reloaded plugin {0} v{1} by {2}"},
            {"PluginUnloaded", "Unloaded plugin {0} v{1} by {2}"},
            {"ShowGroups", "Groups: {0}"},
            {"ServerLanguage", "Server language set to {0}"},
            {"UnknownChatCommand", "Unknown command: {0}"},
            {"UserAddedToGroup", "User '{0}' added to group: {1}"},
            {"UserNotFound", "User '{0}' not found"},
            {"UserPermissionGranted", "User '{0}' granted permission '{1}'"},
            {"UserPermissionRevoked", "User '{0}' revoked permission '{1}'"},
            {"UserRemovedFromGroup", "User '{0}' removed from group '{1}'"},
            {"YouAreNotAdmin", "You are not an admin"}
        };

        #endregion

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, PlayerInfos> loadingPlugins = new Dictionary<string, PlayerInfos>();

        // Get ChatManager NetworkView
        private static readonly FieldInfo ChatNetViewField = typeof(ChatManager).GetField("Chat_NetView", BindingFlags.NonPublic | BindingFlags.Instance);
        public static NetworkView ChatNetView = ChatNetViewField.GetValue(NetworkController.NetManager_.chatManager) as NetworkView;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the HideHoldOutCore class
        /// </summary>
        public HideHoldOutCore()
        {
            // Set attributes
            Name = "HideHoldOutCore";
            Title = "HideHoldOut Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(PlayerInfos player)
        {
            if (permission.IsLoaded) return true;
            ReplyWith(string.Format(GetMessage("PermissionsNotLoaded", permission.LastException.Message)), player);
            return false;
        }

        #endregion

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "hide & hold out");
            RemoteLogger.SetTag("version", NetworkController.NetManager_.get_GAME_VERSION);

            // Register messages for localization
            lang.RegisterMessages(messages, this);

            // Add general chat commands
            //cmdlib.AddChatCommand("oxide.plugins", this, "CmdPlugins");
            //cmdlib.AddChatCommand("plugins", this, "CmdPlugins");
            /*cmdlib.AddChatCommand("oxide.load", this, "CmdLoad");
            cmdlib.AddChatCommand("load", this, "CmdLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "CmdUnload");
            cmdlib.AddChatCommand("unload", this, "CmdUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "CmdReload");
            cmdlib.AddChatCommand("reload", this, "CmdReload");*/
            cmdlib.AddChatCommand("oxide.version", this, "CmdVersion");
            cmdlib.AddChatCommand("version", this, "CmdVersion");
            cmdlib.AddConsoleCommand("oxide.version", this, "CmdVersion");
            cmdlib.AddConsoleCommand("version", this, "CmdVersion");

            // Add permission chat commands
            /*cmdlib.AddChatCommand("oxide.group", this, "CmdGroup");
            cmdlib.AddChatCommand("group", this, "CmdGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "CmdGrant");
            cmdlib.AddChatCommand("grant", this, "CmdGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("oxide.show", this, "CmdShow");
            cmdlib.AddChatCommand("show", this, "CmdShow");*/

            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
                permission.RegisterValidate(s =>
                {
                    ulong temp;
                    if (!ulong.TryParse(s, out temp)) return false;
                    var digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                    return digits >= 17;
                });
                permission.CleanUp();
            }
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();

            if (!loadingPlugins.ContainsKey(plugin.Name)) return;
            ReplyWith($"Loaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            loadingPlugins.Remove(plugin.Name);
        }

        #endregion

        #region Server Hooks

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Add 'oxide' and 'modded' tags
            //SteamGameServer.SetGameTags("oxide,modded");

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", NetworkController.NetManager_.ServManager.Server_NAME);
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="approval"></param>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(NetworkPlayerApproval approval)
        {
            // Set PlayerInfos
            var player = new PlayerInfos { account_id = approval.loginData.ReadString(), Nickname = "Unnamed" };
            // Steamworks.SteamFriends.GetPersonaName(); // TODO: Make use of this?

            // Reject invalid connections
            if (player.account_id == "0" /*|| string.IsNullOrEmpty(player.Nickname)*/)
            {
                approval.Deny(NetworkConnectionError.ConnectionBanned);
                return false;
            }

            // Call out and see if we should reject
            var canlogin = Interface.CallHook("CanClientLogin", player);
            if (canlogin is NetworkConnectionError)
            {
                approval.Deny((NetworkConnectionError)canlogin);
                return true;
            }

            return Interface.CallHook("OnUserApprove", approval, player);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="netPlayer"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(string id, string name, NetworkPlayer netPlayer)
        {
            // Get PlayerInfos
            var player = FindPlayerById(id);

            // Set PlayerInfos // TODO: Find a better hook location that already has this set
            player.account_id = id;
            player.Nickname = name;
            player.NetPlayer = netPlayer;

            // Let covalence know
            Libraries.Covalence.HideHoldOutCovalenceProvider.Instance.PlayerManager.NotifyPlayerConnect(player);

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = player.account_id;
                permission.UpdateNickname(userId, player.Nickname);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[0]);
            }

            Interface.Oxide.CallHook("OnPlayerConnected", player);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerInfos player)
        {
            // Let covalence know
            Libraries.Covalence.HideHoldOutCovalenceProvider.Instance.PlayerManager.NotifyPlayerDisconnect(player);
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(string id, string message)
        {
            if (message.Trim().Length <= 1) return true;
            var str = message.Substring(0, 1);
            var player = FindPlayerById(id);

            // Is it a chat command?
            if (!str.Equals("/") && !str.Equals("!")) return Interface.Oxide.CallHook("OnPlayerChat", player, message);

            // Get the command string
            var command = message.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(player, cmd, args)) // TODO: Fix NRE
            {
                ReplyWith(string.Format(GetMessage("UnknownChatCommand", player.account_id), cmd), player);
                return true;
            }

            Interface.Oxide.CallHook("OnChatCommand", player, command);

            return true;
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("CmdVersion")]
        private void CmdVersion(PlayerInfos player = null) => ReplyWith($"Oxide v{OxideMod.Version}, H2o v{NetworkController.NetManager_.get_GAME_VERSION}", player);

        #endregion

        #region Command Handling

        /*/// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnServerCommand")]
        private object OnServerCommand(string arg)
        {
            return arg == null || arg.Trim().Length == 0 ? null : cmdlib.HandleConsoleCommand(arg);
        }*/

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
        {
            var arglist = new List<string>();
            var sb = new StringBuilder();
            var inlongarg = false;

            foreach (var c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        var arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                        sb = new StringBuilder();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    var arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                var arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
            }
            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }
            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        private void ReplyWith(string message, PlayerInfos player = null)
        {
            if (player == null)
            {
                Interface.Oxide.LogInfo(message);
                return;
            }
            ChatNetView.RPC("NET_Receive_msg", player.NetPlayer, "\r\n" + message, chat_msg_type.standard, player.account_id);
        }

        /// <summary>
        /// Gets the localized message from key, with optional user ID
        /// </summary>
        /// <param name="key"></param>
        /// <param name="userId"></param>
        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        /// <summary>
        /// Returns the PlayerInfos for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayerById(ulong id) => NetworkController.NetManager_.ServManager.GetPlayerInfos_accountID(id.ToString());

        /// <summary>
        /// Returns the PlayerInfos for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayerById(string id) => NetworkController.NetManager_.ServManager.GetPlayerInfos_accountID(id);

        /// <summary>
        /// Returns the PlayerInfos for the specified uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayerByNetPlayer(NetworkPlayer player) => NetworkController.NetManager_.ServManager.GetPlayerInfos(player);

        #endregion
    }
}
