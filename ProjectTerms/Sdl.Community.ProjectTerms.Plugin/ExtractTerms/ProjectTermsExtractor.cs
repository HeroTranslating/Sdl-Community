﻿using Sdl.FileTypeSupport.Framework.IntegrationApi;
using Sdl.FileTypeSupport.Framework.Core.Utilities.IntegrationApi;
using Sdl.ProjectAutomation.Core;
using System.Collections.Generic;
using System.Text;
using Sdl.Community.ProjectTerms.Plugin.ExtractTerms;
using System.Linq;
using System;
using Sdl.Community.ProjectTerms.Plugin.Exceptions;

namespace Sdl.Community.ProjectTerms.Plugin
{
    public class ProjectTermsExtractor
    {
        private List<string> sourceTerms;
        private Dictionary<string, List<KeyValuePair<string, string>>> bilingualContentPair;
        public IFileTypeManager FileTypeManager { get; set; }

        public ProjectTermsExtractor()
        {
            sourceTerms = new List<string>();
            bilingualContentPair = new Dictionary<string, List<KeyValuePair<string, string>>>();
            FileTypeManager = DefaultFileTypeManager.CreateInstance(true);
        }

        private void AddItemToBilingualContentPair(string sourceText, string targetText, string targetLang)
        {
            if (bilingualContentPair.ContainsKey(sourceText))
            {
                List<KeyValuePair<string, string>> targetTerms = bilingualContentPair[sourceText];
                if (targetTerms.Where(x => x.Key.Equals(targetText) && x.Value.Equals(targetLang)) == null) return;

                if(targetTerms.Where(x => x.Key.Equals(targetText)) != null)
                {
                    targetTerms.Add(new KeyValuePair<string, string>(targetText, targetLang));
                }
            }
            else
            {
                List<KeyValuePair<string, string>> targetTermsList = new List<KeyValuePair<string, string>>();
                KeyValuePair<string, string> targetContent = new KeyValuePair<string, string>(targetText, targetLang);
                targetTermsList.Add(targetContent);
                bilingualContentPair.Add(sourceText, targetTermsList);
            }
        }

        public void ExtractBilingualContent(ProjectFile[] targetFiles)
        {
            try
            {
                foreach (var file in targetFiles)
                {
                    IMultiFileConverter converter = FileTypeManager.GetConverter(file.LocalFilePath, (sender, e) => { });
                    TextExtractionBilingualContentHandler extractor = new TextExtractionBilingualContentHandler();
                    converter.AddBilingualProcessor(new Sdl.FileTypeSupport.Framework.Core.Utilities.BilingualApi.BilingualContentHandlerAdapter(extractor));
                    converter.Parse();

                    for (int i = 0; i < extractor.SourceText.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(extractor.SourceText[i]) || string.IsNullOrWhiteSpace(extractor.TargetText[i])) continue;
                        AddItemToBilingualContentPair(extractor.SourceText[i].ToLower(), extractor.TargetText[i].ToLower(), file.Language.DisplayName);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ProjectTermsException(PluginResources.Error_ExtractContent + e.Message);
            }
        }

        public void ExtractProjectFileTerms(ProjectFile projectFile, IMultiFileConverter multiFileConverter)
        {
            try
            {
                if (projectFile.Role != FileRole.Translatable) return;

                FileTypeManager.SettingsBundle = Core.Settings.SettingsUtil.CreateSettingsBundle(null);
                // disable xliff validation to speed up things
                FileTypeManager.SettingsBundle.GetSettingsGroup("SDL XLIFF 1.0 v 1.0.0.0").GetSetting<bool>("ValidateXliff").Value = false;

                TextExtractionBilingualSourceContentHandler extractor = new TextExtractionBilingualSourceContentHandler();
                multiFileConverter.AddBilingualProcessor(new Sdl.FileTypeSupport.Framework.Core.Utilities.BilingualApi.BilingualContentHandlerAdapter(extractor));
                multiFileConverter.Parse();

                foreach (var text in extractor.SourceText)
                {
                    foreach (var term in GetTerms(text))
                    {
                        sourceTerms.Add(term.ToLower());
                    }
                }
            }
            catch (Exception e)
            {
                throw new ProjectTermsException(PluginResources.Error_ExtractContent + e.Message);
            }
        }

        protected virtual IEnumerable<string> GetTerms(string text)
        {
            StringBuilder term = new StringBuilder();
            bool containsLetter = false;
            List<string> terms = new List<string>();

            foreach (char ch in text)
            {
                if (char.IsLetter(ch))
                {
                    term.Append(ch);
                    containsLetter = true;
                }
                else if (char.IsDigit(ch))
                {
                    term.Append(ch);
                }
                else
                {
                    if (!term.Equals("") && containsLetter)
                    {
                        terms.Add(term.ToString());
                    }
                    term.Clear();
                    containsLetter = false;
                }
            }

            if (!term.ToString().Equals("")) terms.Add(term.ToString());

            return terms;
        }

        public List<string> GetSourceTerms()
        {
            return sourceTerms;
        }

        public Dictionary<string, List<KeyValuePair<string, string>>> GetBilingualContentPair()
        {
            return bilingualContentPair;
        }
    }
}