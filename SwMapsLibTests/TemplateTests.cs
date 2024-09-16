﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwMapsLib.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwMapsLibTests
{
	[TestClass]
	public class TemplateTests
	{
		[TestMethod]
		public void ReadV2Template()
		{
			var path = @"Data\SW_WSP_V3_Rural_Template_V2.swmt";
			var reader = new TemplateReader(path);
			var template = reader.Read();

			var writer = new TemplateV2Writer(template);
			writer.WriteTemplate(@"Data\SW_WSP_V3_Rural_Template_V2_re.swmt");

		}

		[TestMethod]
		public void ConvertV1ToV2()
		{
			var path = @"Data\SW_WSP_V3_Rural_Template_V1.swmt";
			var reader = new TemplateReader(path);
			var template = reader.Read();
			var writer = new TemplateV2Writer(template);
			writer.WriteTemplate(@"Data\SW_WSP_V3_Rural_Template_V2.swmt");

		}
	}
}
