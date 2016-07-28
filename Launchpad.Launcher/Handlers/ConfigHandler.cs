﻿//
//  ConfigHandler.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using IniParser;
using IniParser.Model;
using System;
using System.IO;
using System.Reflection;
using Launchpad.Launcher.Utility.Enums;
using Launchpad.Launcher.Utility;
using Launchpad.Launcher.Handlers.Protocols;
using System.Security.Cryptography;
using System.Text;
using log4net;

namespace Launchpad.Launcher.Handlers
{
	/// <summary>
	/// Config handler. This class handles reading and writing to the launcher's configuration.
	/// Read and write operations are synchronized by locks, so it should be threadsafe.
	/// This is a singleton class, and it should always be accessed through <see cref="Instance"/>.
	/// </summary>
	public sealed class ConfigHandler
	{
		/// <summary>
		/// Logger instance for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigHandler));

		/// <summary>
		/// The config lock object.
		/// </summary>
		private readonly object ReadLock = new object();
		/// <summary>
		/// The write lock object.
		/// </summary>
		private readonly object WriteLock = new object();

		/// <summary>
		/// The singleton Instance. Will always point to one shared object.
		/// </summary>
		public static readonly ConfigHandler Instance = new ConfigHandler();

		private ConfigHandler()
		{
			this.Initialize();
		}

		/// <summary>
		/// Writes the config data to disk. This method is thread-blocking, and all write operations
		/// are synchronized via lock(<see cref="WriteLock"/>).
		/// </summary>
		/// <param name="Parser">The parser dealing with the current data.</param>
		/// <param name="Data">The data which should be written to file.</param>
		private void WriteConfig(FileIniDataParser Parser, IniData Data)
		{
			lock (WriteLock)
			{
				Parser.WriteFile(GetConfigPath(), Data);
			}
		}

		/// <summary>
		/// Gets the path to the config file on disk.
		/// </summary>
		/// <returns>The config path.</returns>
		private static string GetConfigPath()
		{
			string configPath = $@"{GetConfigDir()}LauncherConfig.ini";

			return configPath;
		}

		/// <summary>
		/// Gets the path to the config directory.
		/// </summary>
		/// <returns>The config dir, terminated with a directory separator.</returns>
		private static string GetConfigDir()
		{
			string configDir = $@"{GetLocalDir()}Config{Path.DirectorySeparatorChar}";
			return configDir;
		}

		/// <summary>
		/// Initializes the config by checking for bad values or files.
		/// Run once when the launcher starts, then avoid unless absolutely neccesary.
		/// </summary>
		public void Initialize()
		{
			//Since Initialize will write to the config, we'll create the parser here and load the file later
			FileIniDataParser Parser = new FileIniDataParser();

			string configDir = GetConfigDir();
			string configPath = GetConfigPath();

			// Get the launcher version from the assembly.
			Version defaultLauncherVersion = typeof(ConfigHandler).Assembly.GetName().Version;

			//Check for pre-unix config. If it exists, fix the values and copy it.
			UpdateOldConfig();

			//Check for old cookie file. If it exists, rename it.
			ReplaceOldUpdateCookie();

			//should be safe to lock the config now for initializing it
			lock (ReadLock)
			{
				if (!Directory.Exists(configDir))
				{
					Directory.CreateDirectory(configDir);
				}
				if (!File.Exists(configPath))
				{
					//here we create a new empty file
					FileStream configStream = File.Create(configPath);
					configStream.Close();

					//read the file as an INI file
					try
					{
						IniData data = Parser.ReadFile(GetConfigPath());

						data.Sections.AddSection("Local");
						data.Sections.AddSection("Remote");
						data.Sections.AddSection("FTP");
						data.Sections.AddSection("HTTP");
						data.Sections.AddSection("BitTorrent");
						data.Sections.AddSection("Launchpad");

						data["Local"].AddKey("LauncherVersion", defaultLauncherVersion.ToString());
						data["Local"].AddKey("GameName", "LaunchpadExample");
						data["Local"].AddKey("SystemTarget", GetCurrentPlatform().ToString());
						data["Local"].AddKey("GUID", GenerateSeededGUID("LaunchpadExample"));
						data["Local"].AddKey("MainExecutableName", "LaunchpadExample");

						data["Remote"].AddKey("ChangelogURL", "http://directorate.asuscomm.com/launchpad/changelog/changelog.html");
						data["Remote"].AddKey("Protocol", "FTP");
						data["Remote"].AddKey("FileRetries", "2");
						data["Remote"].AddKey("Username", "anonymous");
						data["Remote"].AddKey("Password", "anonymous");

						data["FTP"].AddKey("URL", "ftp://directorate.asuscomm.com");

						data["HTTP"].AddKey("URL", "http://directorate.asuscomm.com/launchpad");

						data["BitTorrent"].AddKey("Magnet", "");

						data["Launchpad"].AddKey("bOfficialUpdates", "true");
						data["Launchpad"].AddKey("bAllowAnonymousStats", "true");

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Failed to create configuration file (IOException): " + ioex.Message);
					}

				}
				else
				{
					/*
						This section is for updating old configuration files
						with new sections introduced in updates.

						It's good practice to wrap each updating section in a
						small informational header with the date and change.
					*/

					IniData data = Parser.ReadFile(GetConfigPath());

					// Update the user-visible version of the launcher
					data["Local"]["LauncherVersion"] = defaultLauncherVersion.ToString();

					// ...

					// March 22 - 2016: Changed GUID generation to create a unique GUID for each game name
					// Update config files without GUID keys
					string seededGUID = GenerateSeededGUID(data["Local"].GetKeyData("GameName").Value);
					if (!data["Local"].ContainsKey("GUID"))
					{
						data["Local"].AddKey("GUID", seededGUID);
					}
					else
					{
						// Update the game GUID
						data["Local"]["GUID"] = seededGUID;
					}
					// End March 22 - 2016

					// Update config files without protocol keys
					if (!data["Remote"].ContainsKey("Protocol"))
					{
						data["Remote"].AddKey("Protocol", "FTP");
					}

					// Update config files without changelog keys
					if (!data["Remote"].ContainsKey("ChangelogURL"))
					{
						data["Remote"].AddKey("ChangelogURL", "http://directorate.asuscomm.com/launchpad/changelog/changelog.html");
					}

					// March 21 - 2016: Moves FTP url to its own section
					// March 21 - 2016: Renames FTP credential keys
					// March 21 - 2016: Adds sections for FTP, HTTP and BitTorrent.
					// March 21 - 2016: Adds configuration option for number of times to retry broken files
					if (data["Remote"].ContainsKey("FTPUsername"))
					{
						string username = data["Remote"].GetKeyData("FTPUsername").Value;
						data["Remote"].RemoveKey("FTPUsername");

						data["Remote"].AddKey("Username", username);

					}
					if (data["Remote"].ContainsKey("FTPPassword"))
					{
						string password = data["Remote"].GetKeyData("FTPPassword").Value;
						data["Remote"].RemoveKey("FTPPassword");

						data["Remote"].AddKey("Password", password);
					}

					if (!data.Sections.ContainsSection("FTP"))
					{
						data.Sections.AddSection("FTP");
					}

					if (data["Remote"].ContainsKey("FTPUrl"))
					{
						string ftpurl = data["Remote"].GetKeyData("FTPUrl").Value;
						data["Remote"].RemoveKey("FTPUrl");

						data["FTP"].AddKey("URL", ftpurl);
					}

					if (!data.Sections.ContainsSection("HTTP"))
					{
						data.Sections.AddSection("HTTP");
					}

					if (!data["HTTP"].ContainsKey("URL"))
					{
						data["HTTP"].AddKey("URL", "http://directorate.asuscomm.com/launchpad");
					}

					if (!data.Sections.ContainsSection("BitTorrent"))
					{
						data.Sections.AddSection("BitTorrent");
					}

					if (!data["BitTorrent"].ContainsKey("Magnet"))
					{
						data["BitTorrent"].AddKey("Magnet", "");
					}

					if (!data["Launchpad"].ContainsKey("bAllowAnonymousStats"))
					{
						data["Launchpad"].AddKey("bAllowAnonymousStats", "true");
					}

					if (!data["Remote"].ContainsKey("FileRetries"))
					{
						data["Remote"].AddKey("FileRetries", "2");
					}
					// End March 21 - 2016

					// June 2 - 2016: Adds main executable redirection option
					if (!data["Local"].ContainsKey("MainExecutuableName"))
					{
						string gameName = data["Local"]["GameName"];
						data["Local"].AddKey("MainExecutableName", gameName);
					}

					// ...
					WriteConfig(Parser, data);
				}
			}

			// Initialize the unique installation GUID, if needed.
			if (!File.Exists(GetInstallGUIDPath()))
			{
				// Make sure all the folders needed exist.
				string GUIDDirectoryPath = Path.GetDirectoryName(GetInstallGUIDPath());
				Directory.CreateDirectory(GUIDDirectoryPath);

				// Generate and store a GUID.
				string GeneratedGUID = Guid.NewGuid().ToString();
				File.WriteAllText(GetInstallGUIDPath(), GeneratedGUID);
			}
			else
			{
				// Make sure the GUID file has been populated
				FileInfo guidInfo = new FileInfo(GetInstallGUIDPath());
				if (guidInfo.Length <= 0)
				{
					// Generate and store a GUID.
					string GeneratedGUID = Guid.NewGuid().ToString();
					File.WriteAllText(GetInstallGUIDPath(), GeneratedGUID);
				}
			}
		}

		/// <summary>
		/// Generates a type-3 deterministic GUID for a specified seed string.
		/// The GUID is not designed to be cryptographically secure, nor is it
		/// designed for any use beyond simple generation of a GUID unique to a
		/// single game. If you use it for anything else, your code is bad and
		/// you should feel bad.
		/// </summary>
		/// <returns>The seeded GUI.</returns>
		/// <param name="seed">Seed.</param>
		public static string GenerateSeededGUID(string seed)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(seed));
				return new Guid(hash).ToString();
			}
		}

		/// <summary>
		/// Gets the path to the update cookie on disk.
		/// </summary>
		/// <returns>The update cookie.</returns>
		public static string GetUpdateCookiePath()
		{
			string updateCookie = $@"{GetLocalDir()}.update";
			return updateCookie;
		}

		/// <summary>
		/// Creates the update cookie.
		/// </summary>
		/// <returns>The update cookie's path.</returns>
		public static string CreateUpdateCookie()
		{
			bool bCookieExists = File.Exists(GetUpdateCookiePath());
			if (!bCookieExists)
			{
				File.Create(GetUpdateCookiePath());

				return GetUpdateCookiePath();
			}
			else
			{
				return GetUpdateCookiePath();
			}
		}

		/// <summary>
		/// Gets the install cookie.
		/// </summary>
		/// <returns>The install cookie.</returns>
		public static string GetInstallCookiePath()
		{
			string installCookie = $@"{GetLocalDir()}.install";
			return installCookie;
		}

		/// <summary>
		/// Creates the install cookie.
		/// </summary>
		/// <returns>The install cookie's path.</returns>
		public static string CreateInstallCookie()
		{
			bool bCookieExists = File.Exists(GetInstallCookiePath());

			if (!bCookieExists)
			{
				File.Create(GetInstallCookiePath()).Close();

				return GetInstallCookiePath();
			}
			else
			{
				return GetInstallCookiePath();
			}
		}

		/// <summary>
		/// Gets the local dir.
		/// </summary>
		/// <returns>The local dir, terminated by a directory separator.</returns>
		public static string GetLocalDir()
		{
			Uri codeBaseURI = new UriBuilder(Assembly.GetExecutingAssembly().Location).Uri;
			return Path.GetDirectoryName(Uri.UnescapeDataString(codeBaseURI.AbsolutePath)) + Path.DirectorySeparatorChar;
		}

		/// <summary>
		/// Gets the temporary launcher download directory.
		/// </summary>
		/// <returns>A full path to the directory.</returns>
		public static string GetTempLauncherDownloadPath()
		{
			return $@"{Path.GetTempPath()}{Path.DirectorySeparatorChar}launchpad{Path.DirectorySeparatorChar}launcher";
		}

		/// <summary>
		/// Gets the game path.
		/// </summary>
		/// <returns>The game path, terminated by a directory separator.</returns>
		public string GetGamePath()
		{
			return $@"{GetLocalDir()}Game{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}";
		}

		/// <summary>
		/// Gets the path to the game executable.
		/// </summary>
		/// <returns>The game executable.</returns>
		public string GetGameExecutable()
		{
			string executablePathRootLevel;
			string executablePathTargetLevel;

			// While not recommended nor supported, the user may add an executable extension to the executable name.
			// We strip it out here (if it exists) just to be safe.
			string executableName = GetMainExecutableName().Replace(".exe", "");

			//unix doesn't need (or have!) the .exe extension.
			if (ChecksHandler.IsRunningOnUnix())
			{
				//should return something along the lines of "./Game/<ExecutableName>"
				executablePathRootLevel = $@"{GetGamePath()}{executableName}";

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>"
				executablePathTargetLevel =
					$@"{GetGamePath()}{GetGameName()}{Path.DirectorySeparatorChar}Binaries" +
					$"{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}{executableName}";
			}
			else
			{
				//should return something along the lines of "./Game/<ExecutableName>.exe"
				executablePathRootLevel = $@"{GetGamePath()}{executableName}.exe";

				//should return something along the lines of "./Game/<GameName>/Binaries/<SystemTarget>/<ExecutableName>.exe"
				executablePathTargetLevel =
					$@"{GetGamePath()}{GetGameName()}{Path.DirectorySeparatorChar}Binaries" +
					$"{Path.DirectorySeparatorChar}{GetSystemTarget()}{Path.DirectorySeparatorChar}{executableName}.exe";
			}


			if (File.Exists(executablePathRootLevel))
			{
				return executablePathRootLevel;
			}

			if (File.Exists(executablePathTargetLevel))
			{
				return executablePathTargetLevel;
			}

			Log.Warn("Could not find the game executable. " +
				"\n\tSearched at : " + executablePathRootLevel +
				"\n\t Searched at: " + executablePathTargetLevel);

			throw new FileNotFoundException("The game executable could not be found.");

		}

		/// <summary>
		/// Gets the local game version.
		/// </summary>
		/// <returns>The local game version.</returns>
		public Version GetLocalGameVersion()
		{
			try
			{
                Version gameVersion = null;
				string rawGameVersion = File.ReadAllText(GetGameVersionPath());

				if (Version.TryParse(rawGameVersion, out gameVersion))
				{
					return gameVersion;
				}
				else
				{
					Log.Warn("Could not parse local game version. Contents: " + rawGameVersion);
					return new Version("0.0.0");
				}
			}
			catch (IOException ioex)
			{
				Log.Warn("Could not read local game version (IOException): " + ioex.Message);
				return null;
			}
		}

		/// <summary>
		/// Gets the game version path.
		/// </summary>
		/// <returns>The game version path.</returns>
		public string GetGameVersionPath()
		{
			string localVersionPath = $@"{GetGamePath()}GameVersion.txt";

			return localVersionPath;
		}

		/// <summary>
		/// Gets the custom launcher download URL.
		/// </summary>
		/// <returns>The custom launcher download URL.</returns>
		public string GetLauncherBinariesURL()
		{
			string launcherURL;
			if (GetDoOfficialUpdates())
			{
				launcherURL = $"{GetOfficialBaseProtocolURL()}/launcher/bin/";
			}
			else
			{
				launcherURL = $"{GetBaseProtocolURL()}/launcher/bin/";
			}

			return launcherURL;
		}

		/// <summary>
		/// Gets the launcher version URL.
		/// </summary>
		/// <returns>The launcher version URL to either the official launchpad
		/// binaries or a custom launcher, depending on the settings.</returns>
		public string GetLauncherVersionURL()
		{
			string versionURL;
			if (GetDoOfficialUpdates())
			{
				versionURL = $"{GetOfficialBaseProtocolURL()}/launcher/LauncherVersion.txt";
			}
			else
			{
				versionURL = $"{GetBaseProtocolURL()}/launcher/LauncherVersion.txt";
			}

			return versionURL;
		}

		/// <summary>
		/// Gets the game URL.
		/// </summary>
		/// <returns>The game URL.</returns>
		public string GetGameURL()
		{
			return $"{GetBaseProtocolURL()}/platform/{GetSystemTarget()}/bin/";
		}

		/// <summary>
		/// Gets the changelog URL.
		/// </summary>
		/// <returns>The changelog URL.</returns>
		public string GetChangelogURL()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string changelogURL = data["Remote"]["ChangelogURL"];

					return changelogURL;

				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read changelog URL (IOException): " + ioex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the launcher version. Locks the config file - DO NOT USE INSIDE OTHER LOCKING FUNCTIONS
		/// </summary>
		/// <returns>The launcher version.</returns>
		public Version GetLocalLauncherVersion()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string launcherVersion = data["Local"]["LauncherVersion"];

					Version localLauncherVersion;
					if (Version.TryParse(launcherVersion, out localLauncherVersion))
					{
						return localLauncherVersion;
					}
					else
					{
						Log.Warn("Failed to parse local launcher version. Returning default version of 0.0.0.");
						return new Version("0.0.0");
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read local launcher version (IOException): " + ioex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the name of the game. Locks the config file - DO NOT USE INSIDE OTHER LOCKING FUNCTIONS
		/// </summary>
		/// <returns>The game name.</returns>
		public string GetGameName()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string gameName = data["Local"]["GameName"];

					return gameName;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the game name (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		// TODO: More dynamic loading of protocols? Maybe even a plugin system?
		// Could use a static registry list in PatchProtocolHandler where plugin
		// protocols register their keys and types
		//
		// private static readonly List<ProtocolDescriptor> AvailableProtocols = new List<ProtocolDescriptor>();
		// ProtocolDescriptor protocol = new ProtocolDescriptor();
		// protocol.Key = "HyperspaceRTL";
		// protocol.Type = typeof(this);
		//
		// PatchProtocolHandler.RegisterProtocol(protocol);
		// PatchProtocolHandler.UnregisterProtocol(protocol);
		//
		/// <summary>
		/// Gets an instance of the desired patch protocol. Currently, FTP, HTTP and BitTorrent are supported.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		public PatchProtocolHandler GetPatchProtocol()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string patchProtocol = data["Remote"]["Protocol"];

					switch (patchProtocol)
					{
						case "FTP":
							{
								return new FTPProtocolHandler();
							}
						case "HTTP":
							{
								return new HTTPProtocolHandler();
							}
						case "BitTorrent":
							{
								return new BitTorrentProtocolHandler();
							}
						default:
							{
								throw new NotImplementedException($"Protocol \"{patchProtocol}\" was not recognized or implemented.");
							}
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read desired protocol (IOException): " + ioex.Message);
					return null;
				}
				catch (NotImplementedException nex)
				{
					Log.Error("Failed to load protocol handler (NotImplementedException): " + nex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Gets the set protocol string.
		/// </summary>
		/// <returns>The patch protocol.</returns>
		public string GetPatchProtocolString()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					return data["Remote"]["Protocol"];
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not read the protocol string (IOException): " + ioex.Message);
					return null;
				}
			}
		}

		/// <summary>
		/// Sets the name of the game.
		/// </summary>
		/// <param name="GameName">Game name.</param>
		public void SetGameName(string GameName)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Local"]["GameName"] = GameName;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not setthe game name (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the system target.
		/// </summary>
		/// <returns>The system target.</returns>
		public ESystemTarget GetSystemTarget()
		{
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string systemTarget = data["Local"]["SystemTarget"];

					return Utilities.ParseSystemTarget(systemTarget);
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not set the system target (IOException): " + ioex.Message);
					return ESystemTarget.Unknown;
				}
				catch (ArgumentException aex)
				{
					Log.Warn("Could not parse the system target (ArgumentException): " + aex.Message);
					return ESystemTarget.Unknown;
				}
			}
		}

		/// <summary>
		/// Sets the system target.
		/// </summary>
		/// <param name="SystemTarget">System target.</param>
		public void SetSystemTarget(ESystemTarget SystemTarget)
		{
			//possible values are:
			//Win64
			//Win32
			//Linux
			//Mac
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Local"]["SystemTarget"] = SystemTarget.ToString();

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the system target (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the username for the remote service.
		/// </summary>
		/// <returns>The remote username.</returns>
		public string GetRemoteUsername()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string remoteUsername = data["Remote"]["Username"];

					return remoteUsername;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the remote username (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the username for the remote service.
		/// </summary>
		/// <param name="Username">The remote username..</param>
		public void SetRemoteUsername(string Username)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Remote"]["Username"] = Username;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the remote username (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the password for the remote service.
		/// </summary>
		/// <returns>The remote password.</returns>
		public string GetRemotePassword()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string remotePassword = data["Remote"]["Password"];

					return remotePassword;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the remote password (IOException): " + ioex.Message);
					return String.Empty;
				}
			}
		}

		/// <summary>
		/// Sets the password for the remote service.
		/// </summary>
		/// <param name="Password">The remote password.</param>
		public void SetRemotePassword(string Password)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Remote"]["Password"] = Password;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the remote password (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the number of times the patching protocol should retry to download files.
		/// </summary>
		/// <returns>The number of file retries.</returns>
		public int GetFileRetries()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string fileRetries = data["Remote"]["FileRetries"];

					int retries;
					if (int.TryParse(fileRetries, out retries))
					{
						return retries;
					}
					else
					{
						return 0;
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the maximum file retries (IOException): " + ioex.Message);
					return 0;
				}
			}
		}

		/// <summary>
		/// Gets the base protocol URL.
		/// </summary>
		/// <returns>The base protocol URL.</returns>
		public string GetBaseProtocolURL()
		{
			switch (GetPatchProtocolString())
			{
				case "FTP":
					{
						return GetBaseFTPUrl();
					}
				case "HTTP":
					{
						return GetBaseHTTPUrl();
					}
				default:
					{
						throw new ArgumentOutOfRangeException(nameof(GetPatchProtocolString), null, "Invalid protocol set in the configuration file.");
					}
			}
		}

		public string GetOfficialBaseProtocolURL()
		{
			switch (GetPatchProtocolString())
			{
				case "FTP":
					{
						return "ftp://ue-web-service.com";
                    }
				case "HTTP":
					{
						return "http://ue-web-service.com/launchpad";
					}
				default:
					{
						throw new ArgumentOutOfRangeException(nameof(GetPatchProtocolString), null, "Invalid protocol set in the configuration file.");
					}
			}
		}

		/// <summary>
		/// Gets the base FTP URL.
		/// </summary>
		/// <returns>The base FTP URL.</returns>
		public string GetBaseFTPUrl()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string FTPURL = data["FTP"]["URL"];

					return FTPURL;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the base FTP URL (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the base FTP URL.
		/// </summary>
		/// <param name="Url">URL.</param>
		public void SetBaseFTPUrl(string Url)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["FTP"]["URL"] = Url;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the base FTP URL (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the base HTTP URL.
		/// </summary>
		/// <returns>The base HTTP URL.</returns>
		public string GetBaseHTTPUrl()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string HTTPURL = data["HTTP"]["URL"];

					return HTTPURL;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the base HTTP URL (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the base HTTP URL.
		/// </summary>
		/// <param name="Url">The new URL.</param>
		public void SetBaseHTTPUrl(string Url)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["HTTP"]["URL"] = Url;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the base HTTP URL (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the BitTorrent magnet link.
		/// </summary>
		/// <returns>The magnet link.</returns>
		public string GetBitTorrentMagnet()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string magnetLink = data["BitTorrent"]["Magnet"];

					return magnetLink;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the BitTorrent magnet link (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the BitTorrent magnet link.
		/// </summary>
		/// <param name="Magnet">The new magnet link.</param>
		public void SetBitTorrentMagnet(string Magnet)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["BitTorrent"]["Magnet"] = Magnet;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the BitTorrent magnet link (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets the name of the main executable.
		/// </summary>
		/// <returns>The name of the main executable.</returns>
		public string GetMainExecutableName()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();

					string configPath = GetConfigPath();
					IniData data = Parser.ReadFile(configPath);

					string mainExecutableName = data["Local"]["MainExecutableName"];

					return mainExecutableName;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not get the main executable name (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}


		/// <summary>
		/// Sets the name of the main executable.
		/// </summary>
		/// <param name="MainExecutableName">The new main executable name.</param>
		public void SetMainExecutableName(string MainExecutableName)
		{
			lock (ReadLock)
			{
				lock (WriteLock)
				{
					try
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						data["Local"]["MainExecutableName"] = MainExecutableName;

						WriteConfig(Parser, data);
					}
					catch (IOException ioex)
					{
						Log.Warn("Could not set the main executable name (IOException): " + ioex.Message);
					}
				}
			}
		}

		/// <summary>
		/// Gets if the launcher should receive official updates.
		/// </summary>
		/// <returns><c>true</c>, if the launcher should receive official updates, <c>false</c> otherwise.</returns>
		public bool GetDoOfficialUpdates()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string rawDoOfficialUpdates = data["Launchpad"]["bOfficialUpdates"];

					bool doOfficialUpdates;
					if (bool.TryParse(rawDoOfficialUpdates, out doOfficialUpdates))
					{
						return doOfficialUpdates;
					}
					else
					{
						Log.Warn("Could not parse if we should use official updates. Allowing by default.");
						return true;
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not determine if we should use official updates (IOException): " + ioex.Message);
					return true;
				}
			}
		}

		/// <summary>
		/// Gets if the launcher is allowed to send usage stats.
		/// </summary>
		/// <returns><c>true</c>, if the launcher is allowed to send usage stats, <c>false</c> otherwise.</returns>
		public bool ShouldAllowAnonymousStats()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string rawAllowAnonymousStats = data["Launchpad"]["bAllowAnonymousStats"];

					bool allowAnonymousStats;
					if (bool.TryParse(rawAllowAnonymousStats, out allowAnonymousStats))
					{
						return allowAnonymousStats;
					}
					else
					{
						Log.Warn("Could not parse if we were allowed to send anonymous stats. Allowing by default.");
						return true;
					}
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not determine if we were allowed to send anonymous stats (IOException): " + ioex.Message);
					return true;
				}
			}
		}

		/// <summary>
		/// Gets the launcher's unique GUID. This GUID maps to a game and not a user.
		/// </summary>
		/// <returns>The GUID.</returns>
		public string GetGameGUID()
		{
			lock (ReadLock)
			{
				try
				{
					FileIniDataParser Parser = new FileIniDataParser();
					IniData data = Parser.ReadFile(GetConfigPath());

					string guid = data["Local"]["GUID"];

					return guid;
				}
				catch (IOException ioex)
				{
					Log.Warn("Could not load the game GUID (IOException): " + ioex.Message);
					return string.Empty;
				}
			}
		}

		/// <summary>
		/// Gets the path to the install-unique GUID.
		/// </summary>
		/// <returns>The install GUID path.</returns>
		public string GetInstallGUIDPath()
		{
			return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Launchpad/.installguid";
		}

		/// <summary>
		/// Gets the install-unique GUID. This is separate from the launcher GUID, which maps to a game.
		/// </summary>
		/// <returns>The install GUI.</returns>
		public string GetInstallGUID()
		{
			if (File.Exists(GetInstallGUIDPath()))
			{
				return File.ReadAllText(GetInstallGUIDPath());
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// Replaces and updates the old pre-unix config.
		/// </summary>
		/// <returns><c>true</c>, if an old config was copied over to the new format, <c>false</c> otherwise.</returns>
		private bool UpdateOldConfig()
		{
			string oldConfigPath = $@"{GetLocalDir()}config{Path.DirectorySeparatorChar}launcherConfig.ini";

			string oldConfigDir = $@"{GetLocalDir()}config";

			if (ChecksHandler.IsRunningOnUnix())
			{
				// Case sensitive
				// Is there an old config file?
				if (File.Exists(oldConfigPath))
				{
					lock (ReadLock)
					{
						// Have we not already created the new config dir?
						if (!Directory.Exists(GetConfigDir()))
						{
							// If not, create it.
							Directory.CreateDirectory(GetConfigDir());

							// Copy the old config file to the new location.
							File.Copy(oldConfigPath, GetConfigPath());

							// Read our new file.
							FileIniDataParser Parser = new FileIniDataParser();
							IniData data = Parser.ReadFile(GetConfigPath());

							// Replace the old invalid keys with new, updated keys.
							string launcherVersion = data["Local"]["launcherVersion"];
							string gameName = data["Local"]["gameName"];
							string systemTarget = data["Local"]["systemTarget"];

							data["Local"].RemoveKey("launcherVersion");
							data["Local"].RemoveKey("gameName");
							data["Local"].RemoveKey("systemTarget");

							data["Local"].AddKey("LauncherVersion", launcherVersion);
							data["Local"].AddKey("GameName", gameName);
							data["Local"].AddKey("SystemTarget", systemTarget);

							WriteConfig(Parser, data);
							// We were successful, so return true.

							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
							return true;
						}
						else
						{
							// The new config dir already exists, so we'll just toss out the old one.
							// Delete the old config
							File.Delete(oldConfigPath);
							Directory.Delete(oldConfigDir, true);
							return false;
						}
					}
				}
				else
				{
					return false;
				}
			}
			else
			{
				lock (ReadLock)
				{
					// Windows is not case sensitive, so we'll use direct access without copying.
					if (File.Exists(oldConfigPath))
					{
						FileIniDataParser Parser = new FileIniDataParser();
						IniData data = Parser.ReadFile(GetConfigPath());

						// Replace the old invalid keys with new, updated keys.
						string launcherVersion = data["Local"]["launcherVersion"];
						string gameName = data["Local"]["gameName"];
						string systemTarget = data["Local"]["systemTarget"];

						data["Local"].RemoveKey("launcherVersion");
						data["Local"].RemoveKey("gameName");
						data["Local"].RemoveKey("systemTarget");

						data["Local"].AddKey("LauncherVersion", launcherVersion);
						data["Local"].AddKey("GameName", gameName);
						data["Local"].AddKey("SystemTarget", systemTarget);

						WriteConfig(Parser, data);

						// We were successful, so return true.
						return true;
					}
					else
					{
						return false;
					}

				}
			}

		}

		/// <summary>
		/// Replaces the old update cookie.
		/// </summary>
		private static void ReplaceOldUpdateCookie()
		{
			string oldUpdateCookiePath = $@"{GetLocalDir()}.updatecookie";

			if (File.Exists(oldUpdateCookiePath))
			{
				string updateCookiePath = $@"{GetLocalDir()}.update";

				File.Move(oldUpdateCookiePath, updateCookiePath);
			}
		}

		/// <summary>
		/// Gets the current platform the launcher is running on.
		/// </summary>
		/// <returns>The current platform.</returns>
		public static ESystemTarget GetCurrentPlatform()
		{
			string platformID = Environment.OSVersion.Platform.ToString();
			if (platformID.Contains("Win"))
			{
				platformID = "Windows";
			}

			switch (platformID)
			{
				case "MacOSX":
					{
						return ESystemTarget.Mac;
					}
				case "Unix":
					{
						//Mac may sometimes be detected as Unix, so do an additional check for some Mac-only directories
						if (Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/Users") && Directory.Exists("/Volumes"))
						{
							return ESystemTarget.Mac;
						}
						else
						{
							return ESystemTarget.Linux;
						}
					}
				case "Windows":
					{
						if (Environment.Is64BitOperatingSystem)
						{
							return ESystemTarget.Win64;
						}
						else
						{
							return ESystemTarget.Win32;
						}
					}
				default:
					{
						return ESystemTarget.Unknown;
					}
			}
		}
	}
}
