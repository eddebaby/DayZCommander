using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using SharpCompress.Common;
using SharpCompress.Reader;

// ReSharper disable InconsistentNaming
namespace Dotjosh.DayZCommander.App.Core
{
	public class GameUpdater
	{
		const string _armaBetaPage = "http://www.arma2.com/beta-patch.php";
		const string _dayZPage = "http://cdn.armafiles.info/latest/";
		public int? LatestArma2OABetaRevision { get; private set; }
		public string LatestArma2OABetaUrl { get; private set; }
		public Version LatestDayZVersion { get; private set; }

		public GameUpdater()
		{
			GetLatestVersions();
		}

		public bool UpdateArma2OABeta()
		{
			return false;
		}

		public bool UpdateDayZ()
		{
			if(string.IsNullOrEmpty(LocalMachineInfo.DayZPath))
			{
				return false;
			}
			var dayZFiles = GetDayZFiles();
			foreach(var dayZFile in dayZFiles)
			{
				var dayZAddonPath = Path.Combine(LocalMachineInfo.DayZPath, @"Addons").MakeSurePathExists();
				var dayZFileUrl = Path.Combine(_dayZPage, dayZFile);
				var dayZFilePath = Path.Combine(LocalMachineInfo.DayZPath, dayZFile);
				var webClient = new WebClient();
				webClient.DownloadFile(dayZFileUrl, dayZFilePath);
				if(dayZFile.EndsWithAny("zip", "rar"))
				{
					using(var stream = File.OpenRead(dayZFilePath))
					{
						using(var reader = ReaderFactory.Open(stream))
						{
							while(reader.MoveToNextEntry())
							{
								if(reader.Entry.IsDirectory)
								{
									continue;
								}
								reader.WriteEntryToDirectory(dayZAddonPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
							}
						}
					}
					File.Delete(dayZFilePath);
				}
				//ReaderFactory.Open()
				//DownloadFile();
			}
			return false;
		}

		private void GetLatestVersions()
		{
			GetLatestArma2OABetaRevision();
			GetLatestDayZVersion();
		}

		private void GetLatestArma2OABetaRevision()
		{
			string responseBody;
			if(!HttpGet(_armaBetaPage, out responseBody))
			{
				return;
			}
			var latestBetaUrlMatch = Regex.Match(responseBody, @"Latest\s+beta\s+patch:\s*<a\s+href\s*=\s*(?:'|"")([^'""]+)(?:'|"")", RegexOptions.IgnoreCase);
			if(!latestBetaUrlMatch.Success)
			{
				return;
			}
			LatestArma2OABetaUrl = latestBetaUrlMatch.Groups[1].Value;
			var latestBetaRevisionMatch = Regex.Match(LatestArma2OABetaUrl, @"(\d+)\.(?:zip|rar)", RegexOptions.IgnoreCase);
			if(!latestBetaRevisionMatch.Success)
			{
				return;
			}
			LatestArma2OABetaRevision = latestBetaRevisionMatch.Groups[1].Value.TryIntNullable();
		}

		private void GetLatestDayZVersion()
		{
			string responseBody;
			if(!HttpGet(_dayZPage, out responseBody))
			{
				return;
			}
			var latestCodeFileMatch = Regex.Match(responseBody, @"<a\s+href\s*=\s*(?:'|"")(dayz_code_[^'""]+)(?:'|"")", RegexOptions.IgnoreCase);
			if(!latestCodeFileMatch.Success)
			{
				return;
			}
			var latestCodeFile = latestCodeFileMatch.Groups[1].Value;
			var latestCodeVersionMatch = Regex.Match(latestCodeFile, @"\d(?:\.\d){1,3}");
			if(!latestCodeVersionMatch.Success)
			{
				return;
			}
			Version version;
			if(Version.TryParse(latestCodeVersionMatch.Value, out version))
			{
				LatestDayZVersion = version;
			}
		}

		private List<string> GetDayZFiles()
		{
			var files = new List<string>();
			string responseBody;
			if(!HttpGet(_dayZPage, out responseBody))
			{
				return files;
			}
			var fileMatches = Regex.Matches(responseBody, @"<a\s+href\s*=\s*(?:'|"")([^'""]+\.[^'""]{3})(?:'|"")", RegexOptions.IgnoreCase);
			foreach(Match match in fileMatches)
			{
				if(!match.Success)
				{
					continue;
				}
				var file = match.Groups[1].Value;
				if(string.IsNullOrEmpty(file))
				{
					continue;
				}

				files.Add(file);
			}

			return files;
		}

		public bool HttpGet(string page, out string responseBody)
		{
			responseBody = null;
			var request = (HttpWebRequest)WebRequest.Create(page);
			request.Method = "GET";
			request.Timeout = 120000; // ms

			try
			{
				using(var response = request.GetResponse())
				{
					using(var responseStream = response.GetResponseStream())
					{
						if(responseStream == null)
						{
							return false;
						}
						var streamReader = new StreamReader(responseStream);
						responseBody = streamReader.ReadToEnd();
						streamReader.Close();
					}
				}
			}
			catch//(Exception e)
			{
				return false;
			}
			return true;
		}
	}
}