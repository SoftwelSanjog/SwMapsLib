﻿using SwMapsLib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwMapsLib.IO
{
	//Reads SW Maps V1 and V2 Project Files
	public class SwmzReader
	{
		static string TempFolder = System.IO.Path.GetTempPath() + "\\SW_Maps\\";

		public readonly string SwmzPath;
		public readonly string DbPath; //Extracted path
		public readonly string ProjectTempDir;
		public bool IsV1Project;

		public bool ShortenZipNames = false;
		public SwMapsProject Read(bool readMediaFiles = false)
		{
			ISwMapsDbReader reader;

			if (IsV1Project)
				reader = new SwMapsV1Reader(DbPath);
			else
				reader = new SwMapsV2Reader(DbPath);

			SwMapsProject project = reader.Read();
			if (readMediaFiles)
			{
				var dir = Path.Combine(ProjectTempDir, "Photos");

				if (Directory.Exists(dir))
				{
					var files = Directory.EnumerateFiles(dir);
					foreach (var file in files)
					{
						var name = Path.GetFileName(file);
						var data = File.ReadAllBytes(file);
						project.MediaFiles[name] = data;
					}
				}
			}

			//GNSS Raw Data
			var gnssRawDir = Path.Combine(ProjectTempDir, "RawFiles");
			if (Directory.Exists(gnssRawDir))
			{
				var files = Directory.EnumerateFiles(gnssRawDir);
				foreach (var file in files)
				{
					project.GnssRawDataFiles.Add(file);
				}
			}
			return project;
		}


		public SwmzReader(string swmzPath, bool shortenZipNames = false)
		{
			SwmzPath = swmzPath;
			ShortenZipNames = shortenZipNames;

			if (ShortenZipNames) {
			    
				ProjectTempDir =Path.Combine( TempFolder + Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
			}
			else {
				ProjectTempDir =Path.Combine(TempFolder + Path.GetFileNameWithoutExtension(swmzPath).Trim());
			}
			if (Directory.Exists(ProjectTempDir))
				Directory.Delete(ProjectTempDir, true);

			Directory.CreateDirectory(ProjectTempDir);

			DbPath = "";

			using (var zip = ZipFile.Open(swmzPath, ZipArchiveMode.Read))
			{
				zip.ExtractToDirectory(ProjectTempDir);
			}
			var dirs = Directory.EnumerateDirectories(ProjectTempDir);

			foreach (var f in dirs)
			{
				if (Path.GetFileName(f) == "Projects")
				{
					var files = Directory.EnumerateFiles(f);
					foreach (var file in files)
						if (file.EndsWith(".swm2", StringComparison.OrdinalIgnoreCase))
						{
							DbPath = file;
							IsV1Project = false;
						}
				}
				else if (Path.GetFileName(f) == "MapProjects")
				{
					var files = Directory.EnumerateFiles(f);
					foreach (var file in files)
						if (file.EndsWith(".swmaps", StringComparison.OrdinalIgnoreCase))
						{
							DbPath = file;
							IsV1Project = true;
						}
				}
			}


		}
	}
}
