﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sdl.FileTypeSupport.Framework.BilingualApi;

namespace Sdl.Community.Anonymizer.Process_Xliff
{
	public class DecryptDataProcessor : AbstractBilingualContentProcessor
	{
		public override void ProcessParagraphUnit(IParagraphUnit paragraphUnit)
		{
			base.ProcessParagraphUnit(paragraphUnit);
			if (paragraphUnit.IsStructure) { return; }

			foreach (var segmentPair in paragraphUnit.SegmentPairs.ToList())
			{
				var decryptVisitor = new DecryptSegmentVisitor();
				decryptVisitor.DecryptText(segmentPair.Source, ItemFactory, PropertiesFactory);
				if (segmentPair.Target != null)
				{
					decryptVisitor.DecryptText(segmentPair.Target, ItemFactory, PropertiesFactory);
				}
			}
		}
	}
}
