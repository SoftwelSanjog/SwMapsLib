﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwMapsLib.Data
{
	public class SwMapsProject
	{
		public Dictionary<string, string> ProjectInfo = new Dictionary<string, string>();
		public List<SwMapsFeatureLayer> FeatureLayers = new List<SwMapsFeatureLayer>();
		public List<SwMapsFeature> Features = new List<SwMapsFeature>();
		public List<SwMapsTrack> Tracks = new List<SwMapsTrack>();
		public List<SwMapsPhotoPoint> PhotoPoints = new List<SwMapsPhotoPoint>();
		public List<SwMapsProjectAttribute> ProjectAttributes = new List<SwMapsProjectAttribute>();

		public Dictionary<string, byte[]> MediaFiles = new Dictionary<string, byte[]>();
		public List<string> GnssRawDataFiles { get; set; } = new List<string>();


		public string DatabasePath { get; }
		public string MediaFolderPath { get; }

		public string TemplateName
		{
			get
			{
				if (ProjectInfo.ContainsKey("template_name")) return ProjectInfo["template_name"];
				return "";
			}
			set
			{
				ProjectInfo["template_name"] = value;
			}
		}

		public string TemplateAuthor
		{
			get
			{
				if (ProjectInfo.ContainsKey("template_author")) return ProjectInfo["template_author"];
				return "";
			}
			set
			{
				ProjectInfo["template_author"] = value;
			}
		}

		public SwMapsFeature GetFeature(string id)
		{
			return Features.FirstOrDefault(iterator => iterator.UUID == id);
		}

		public SwMapsFeatureLayer GetLayer(string id)
		{
			return FeatureLayers.FirstOrDefault(iterator => iterator.UUID == id);
		}

		public List<SwMapsFeature> GetAllFeatures(SwMapsFeatureLayer layer)
		{
			return Features.Where(f => f.LayerID == layer.UUID).ToList();
		}

		public SwMapsProject(string dbpath, string mediaPath)
		{
			DatabasePath = dbpath;
			MediaFolderPath = mediaPath;
		}
		public SwMapsProject()
		{
			DatabasePath = "";
			MediaFolderPath = "";
		}

		/// <summary>
		/// Reassigns the sequence numbers for all the points in this project
		/// </summary>
		internal void ResequenceAll()
		{
			foreach (var f in Features)
			{
				f.Points.OrderBy(p => p.Seq);

				for (int i = 0; i < f.Points.Count; i++)
				{
					f.Points[i].Seq = i;
					f.Points[i].FeatureID = f.UUID;
				}
			}

			foreach (var t in Tracks)
			{
				t.Vertices.OrderBy(p => p.Seq);


				for (int i = 0; i < t.Vertices.Count; i++)
				{
					t.Vertices[i].Seq = i;
					t.Vertices[i].FeatureID = t.UUID;
				}
			}

			foreach (var t in PhotoPoints)
			{
				t.Location.Seq = 0;
			}
		}

		public List<string> GetAllMediaFiles()
		{
			var ret = new List<string>();

			foreach (var f in Features)
			{
				foreach (var attr in f.AttributeValues)
				{
					if (attr.DataType == SwMapsAttributeType.Audio
						|| attr.DataType == SwMapsAttributeType.Photo
						|| attr.DataType == SwMapsAttributeType.Video)
					{
						var path = GetMediaFilePath(attr.Value);
						if (path == null) continue;
						ret.Add(path);
					}
				}
			}

			foreach (var ph in PhotoPoints)
			{
				var path = ph.FileName;
				if (path == null) continue;
				ret.Add(path);
			}

			return ret;
		}



		public string GetMediaFilePath(string mediaFileName)
		{
			if (mediaFileName == null || mediaFileName == "") return null;

			try
			{
				if (File.Exists(mediaFileName))
				{
					return mediaFileName;
				}
				else if (File.Exists(Path.Combine(MediaFolderPath, mediaFileName)))
				{
					return Path.Combine(MediaFolderPath, mediaFileName);
				}
			}
			catch { return null; }

			return null;
		}
	}
}
