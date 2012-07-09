using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

// ReSharper disable InconsistentNaming
namespace Dotjosh.DayZCommander.App.Core
{
	public class GameUpdater
	{
		public int? LatestArma2OABetaRevision { get; private set; }
		public string LatestArma2OABetaUrl { get; private set; }
		public Version LatestDayZVersion { get; private set; }

		public GameUpdater()
		{
			GetLatestVersions();
		}

		public void UpdateArma2OABeta()
		{
		}

		public void UpdateDayZ()
		{
		}

		private void GetLatestVersions()
		{
			GetLatestArma2OABetaRevision();
			GetLatestDayZVersion();
		}

		private void GetLatestArma2OABetaRevision()
		{
			const string armaBetaPage = "http://www.arma2.com/beta-patch.php";
			string responseBody;
			if(!HttpGet(armaBetaPage, out responseBody))
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
			const string dayZPage = "http://cdn.armafiles.info/latest/";
			string responseBody;
			if(!HttpGet(dayZPage, out responseBody))
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