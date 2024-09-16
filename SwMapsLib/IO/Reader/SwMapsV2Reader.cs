﻿using SwMapsLib.Data;
using SwMapsLib.Extensions;
using SwMapsLib.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace SwMapsLib.IO
{
	public class SwMapsV2Reader : ISwMapsDbReader
	{
		public readonly string Swm2Path;


		public SwMapsV2Reader(string swm2Path)
		{
			Swm2Path = swm2Path;
		}

		public SwMapsProject Read()
		{
			using (var conn = new SQLiteConnection($"Data Source={Swm2Path};Version=3;"))
			{
				try
				{
					conn.Open();

					var mediaPath = Directory.GetParent(Path.GetDirectoryName(Swm2Path)).FullName;
					mediaPath = Path.Combine(mediaPath, "Photos");

					var project = new SwMapsProject(Swm2Path, mediaPath);
					project.ProjectInfo = ReadProjectInfo(conn);
					project.FeatureLayers = ReadAllFeatureLayers(conn);
					project.Features = ReadAllFeatures(conn, project.FeatureLayers);
					project.Tracks = ReadAllTracks(conn);
					project.PhotoPoints = ReadAllPhotoPoints(conn);
					project.ProjectAttributes = ReadProjectAttributes(conn);

					foreach (var f in project.Features)
					{
						var layer = project.GetLayer(f.LayerID);
						foreach (var a in f.AttributeValues)
						{
							a.FieldName = layer.AttributeFields.FirstOrDefault(e => e.UUID == a.FieldID)?.FieldName ?? "";
						}
					}
					return project;
				}
				finally
				{
					conn.CloseConnection();
				}
			}
		}

		private Dictionary<string, string> ReadProjectInfo(SQLiteConnection conn)
		{
			var ret = new Dictionary<string, string>();
			var sql = $"SELECT * FROM project_info;";

			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var key = reader.ReadString("attr");
					var value = reader.ReadString("value");
					ret[key] = value;
				}

			return ret;
		}

		public List<SwMapsProjectAttribute> ReadProjectAttributes(SQLiteConnection conn)
		{
			var ret = new List<SwMapsProjectAttribute>();
			var sql = "SELECT * FROM project_attributes";

			using (var cmd = new SQLiteCommand(sql, conn))
			{
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						var a = new SwMapsProjectAttribute();
						a.Name = reader.ReadString("attr");
						a.Value = reader.ReadString("value");

						a.IsRequired = reader.ReadInt32("required_field") == 1;

						var dataType = reader.ReadString("data_type");
						a.DataType = SwMapsTypes.ProjectAttributeTypeFromString(dataType);

						var choices = reader.ReadString("field_choices");
						a.Choices = choices.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries).ToList();

						ret.Add(a);
					}
				}
			}

			return ret;
		}

		public List<SwMapsAttributeField> ReadAttributeFields(SQLiteConnection conn, string layerID)
		{
			var ret = new List<SwMapsAttributeField>();
			var sql = $"SELECT * FROM attribute_fields WHERE layer_id = '{layerID}';";

			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var a = new SwMapsAttributeField();
					a.UUID = reader.ReadString("uuid");
					a.LayerID = reader.ReadString("layer_id");
					a.FieldName = reader.ReadString("field_name");

					var dataType = reader.ReadString("data_type");
					a.DataType = SwMapsTypes.AttributeTypeFromString(dataType);


					a.Choices = reader.ReadString("field_choices").Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					if (ret.Any(at => at.UUID == a.UUID)) continue;
					ret.Add(a);


				}
			return ret;
		}

		public List<SwMapsAttributeValue> ReadAttributeValues(SQLiteConnection conn, string fid)
		{
			var ret = new List<SwMapsAttributeValue>();
			var sql = $"SELECT * FROM attribute_values WHERE item_id='{fid}'";
			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var a = new SwMapsAttributeValue();
					a.FeatureID = fid;
					a.FieldID = reader.ReadString("field_id");
					a.Value = reader.ReadString("value");

					var dataType = reader.ReadString("data_type");
					a.DataType = SwMapsTypes.AttributeTypeFromString(dataType);

					ret.Add(a);
				}

			return ret;
		}

		public List<SwMapsFeature> ReadAllFeatures(SQLiteConnection conn, List<SwMapsFeatureLayer> layers)
		{
			var ret = new List<SwMapsFeature>();
			var sql = "SELECT rowid,* FROM features";
			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var feature = new SwMapsFeature();
					feature.UUID = reader.ReadString("uuid");
					feature.LayerID = reader.ReadString("layer_id");
					feature.Name = reader.ReadString("name");
					feature.FeatureID = (int)reader.ReadInt64("rowid");
					feature.Remarks = reader.ReadString("remarks");
					feature.Points = ReadPoints(conn, feature.UUID);
					feature.AttributeValues = ReadAttributeValues(conn, feature.UUID);

					feature.GeometryType = layers.FirstOrDefault(l => l.UUID == feature.LayerID)?.GeometryType ?? SwMapsGeometryType.Point;
					ret.Add(feature);
				}
			return ret;
		}

		public List<SwMapsFeatureLayer> ReadAllFeatureLayers(SQLiteConnection conn)
		{
			var ret = new List<SwMapsFeatureLayer>();
			var sql = "SELECT * FROM feature_layers;";
			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var layer = new SwMapsFeatureLayer();
					layer.UUID = reader.ReadString("uuid");
					layer.Name = reader.ReadString("name");
					layer.GroupName = reader.ReadString("group_name");

					var geomType = reader.ReadString("geom_type");
					layer.GeometryType = SwMapsTypes.GeometryTypeFromString(geomType);

					var pointSymbol = reader.ReadString("point_symbol");
					layer.PointShape = SwMapsTypes.PointShapeFromString(pointSymbol);

					layer.Color = reader.ReadInt32("color");
					layer.FillColor = reader.ReadInt32("fill_color");
					layer.LineWidth = reader.ReadSingle("line_width");
					layer.LabelFieldID = reader.ReadString("label_field_id");
					layer.Active = reader.ReadInt32("active") == 1;
					layer.Drawn = reader.ReadInt32("drawn") == 1;
					layer.PngSymbol = reader.ReadBlob("png_symbol");
					layer.ZIndex = reader.ReadInt32("z_index");
					layer.AttributeFields = ReadAttributeFields(conn, layer.UUID);
					ret.Add(layer);
				}
			return ret;
		}

		public List<SwMapsPoint> ReadPoints(SQLiteConnection conn, string fid)
		{
			var ret = new List<SwMapsPoint>();
			var sql = $"SELECT * FROM points WHERE fid='{fid}' ORDER BY seq";
			using (var pointCmd = new SQLiteCommand(sql, conn))
			using (var pointReader = pointCmd.ExecuteReader())
				while (pointReader.Read())
				{
					SwMapsPoint vertex = new SwMapsPoint();
					vertex.ID = pointReader.ReadString("uuid");
					vertex.FeatureID = pointReader.ReadString("fid");
					vertex.Seq = pointReader.ReadInt32("seq");

					vertex.Latitude = pointReader.ReadDouble("lat");
					vertex.Longitude = pointReader.ReadDouble("lon");
					vertex.Elevation = pointReader.ReadDouble("elv");
					vertex.OrthoHeight = pointReader.ReadDouble("ortho_ht");


					vertex.Time = pointReader.ReadInt64("time");
					vertex.StartTime = pointReader.ReadInt64("start_time");

					vertex.InstrumentHeight = pointReader.ReadDouble("instrument_ht");
					vertex.FixID = pointReader.ReadInt32("fix_quality");

					vertex.Speed = pointReader.ReadDouble("speed");
					vertex.SnapID = pointReader.ReadString("snap_id");
					vertex.AdditionalData = pointReader.ReadString("additional_data");
					vertex.Bearing = pointReader.ReadDouble("bearing");
					vertex.AccuracyH = pointReader.ReadDouble("accuracy_h");
					vertex.AccuracyV = pointReader.ReadDouble("accuracy_v");
					vertex.PositionData = pointReader.ReadString("pos_data");
					ret.Add(vertex);
				}
			return ret;
		}

		public List<SwMapsTrack> ReadAllTracks(SQLiteConnection conn)
		{
			List<SwMapsTrack> ret = new List<SwMapsTrack>();

			var sql = "SELECT * FROM tracks";
			using (var cmd = new SQLiteCommand(sql, conn))
			using (var trackReader = cmd.ExecuteReader())
				while (trackReader.Read())
				{
					SwMapsTrack tr = new SwMapsTrack();
					tr.UUID = trackReader.ReadString("uuid");
					tr.Name = trackReader.ReadString("name");
					tr.Color = trackReader.ReadInt32("color");
					tr.Remarks = trackReader.ReadString("description");
					ret.Add(tr);
				}

			foreach (var tr in ret)
			{
				tr.Vertices = ReadPoints(conn, tr.UUID);
			}
			return ret;
		}

		public List<SwMapsPhotoPoint> ReadAllPhotoPoints(SQLiteConnection conn)
		{
			var ret = new List<SwMapsPhotoPoint>();
			var sql = "SELECT * FROM photos";
			using (var cmd = new SQLiteCommand(sql, conn))
			using (var reader = cmd.ExecuteReader())
				while (reader.Read())
				{
					var ph = new SwMapsPhotoPoint();
					ph.ID = reader.ReadString("uuid");
					ph.Remarks = reader.ReadString("remarks");
					ph.FileName = reader.ReadString("photo_path");
					ph.Location = ReadPoints(conn, ph.ID).FirstOrDefault();
					ret.Add(ph);
				}
			return ret;
		}
	}
}
