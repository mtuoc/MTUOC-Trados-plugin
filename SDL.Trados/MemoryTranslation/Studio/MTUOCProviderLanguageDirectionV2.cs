using Microsoft.Extensions.DependencyInjection;
using Sdl.Core.Globalization;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemory;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Http;
using SDL.Trados.MTUOC.Services.Tags;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SDL.Trados.MTUOC.Studio
{
    public class MTUOCProviderLanguageDirectionV2 : ITranslationProviderLanguageDirection
    {
        private readonly IHttpService _httpService;
        private readonly MTUOCProvider _provider;
        private readonly LanguagePair _languageDirection;
        private readonly SettingsDto _settings;
        private TranslationUnit _inputTu;

        public MTUOCProviderLanguageDirectionV2(MTUOCProvider provider, LanguagePair languages, SettingsDto settings)
        {
            _httpService = Startup.DIContainer.GetService<IHttpService>();
            _provider = provider;
            _languageDirection = languages;
            _settings = settings;
        }

        #region ITranslationProviderLanguageDirection

        public ITranslationProvider TranslationProvider
        {
            get
            {
                return _provider;
            }
        }

#if (Version2019 || Version2021)
        public CultureInfo SourceLanguage
        {
            get
            {
                return _languageDirection.SourceCulture;
            }
        }

        public CultureInfo TargetLanguage
        {
            get
            {
                return _languageDirection.TargetCulture;
            }
        }
#else
        public CultureCode SourceLanguage
        {
            get
            {
                return _languageDirection.SourceCulture;
            }
        }

        public CultureCode TargetLanguage
        {
            get
            {
                return _languageDirection.TargetCulture;
            }
        }
#endif
        public bool CanReverseLanguageDirection
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnits"></param>
        /// <param name="previousTranslationHashes"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public ImportResult[] AddOrUpdateTranslationUnits(TranslationUnit[] translationUnits, int[] previousTranslationHashes, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Call <see cref="AddTranslationUnit(TranslationUnit, ImportSettings)"/> : Not required for this implementation.
        /// </summary>
        /// <param name="translationUnits"></param>
        /// <param name="previousTranslationHashes"></param>
        /// <param name="settings"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public ImportResult[] AddOrUpdateTranslationUnitsMasked(TranslationUnit[] translationUnits, int[] previousTranslationHashes, ImportSettings settings, bool[] mask)
        {
            ImportResult[] result = { AddTranslationUnit(translationUnits[translationUnits.GetLength(0) - 1], settings) };
            return result;
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnit"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public ImportResult AddTranslationUnit(TranslationUnit translationUnit, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnits"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public ImportResult[] AddTranslationUnits(TranslationUnit[] translationUnits, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnits"></param>
        /// <param name="settings"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public ImportResult[] AddTranslationUnitsMasked(TranslationUnit[] translationUnits, ImportSettings settings, bool[] mask)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs the actual search by looping through the
        /// delimited segment pairs contained in the text file.
        /// Depening on the search mode, a segment lookup (with exact machting) or a source / target
        /// concordance search is done.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public SearchResults SearchSegment(SearchSettings settings, Segment segment)
        {
            var translation = new Segment(_languageDirection.TargetCulture);//this will be the translated segment
            var newseg = segment.Duplicate();
            var results = new SearchResults { SourceSegment = segment.Duplicate() };

            if (_inputTu.ConfirmationLevel == ConfirmationLevel.Translated ||
                _inputTu.ConfirmationLevel == ConfirmationLevel.ApprovedTranslation)
                return TranslateUntranslatedSegment(segment, translation, results);

            translation = newseg.HasTags
                ? TranslationWithTags(newseg)
                : Translation(translation, newseg);
            results.Add(CreateSearchResult(newseg, translation));

            return results;
        }

        /// <summary>
        /// Performs the actual search by looping through the
        /// delimited segment pairs contained in the text file.
        /// Depening on the search mode, a segment lookup (with exact machting) or a source / target
        /// concordance search is done.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="segments"></param>
        public SearchResults[] SearchSegments(SearchSettings settings, Segment[] segments)
        {
            var results = new SearchResults[segments.Length];
            for (var p = 0; p < segments.Length; ++p)
            {
                results[p] = SearchSegment(settings, segments[p]);
            }
            return results;
        }

        /// <summary>
        /// SearchSegmentsMasked
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="segments"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public SearchResults[] SearchSegmentsMasked(SearchSettings settings, Segment[] segments, bool[] mask)
        {
            if (segments == null)
            {
                throw new ArgumentNullException("segments in SearchSegmentsMasked");
            }
            if (mask == null || mask.Length != segments.Length)
            {
                throw new ArgumentException("mask in SearchSegmentsMasked");
            }

            var results = new SearchResults[segments.Length];
            for (var p = 0; p < segments.Length; ++p)
            {
                if (mask[p])
                {
                    results[p] = SearchSegment(settings, segments[p]);
                }
                else
                {
                    results[p] = null;
                }
            }
            return results;
        }

        /// <summary>
        /// SearchText
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public SearchResults SearchText(SearchSettings settings, string segment)
        {
            var currentSegment = new Segment(_languageDirection.SourceCulture);
            currentSegment.Add(segment);
            return SearchSegment(settings, currentSegment);
        }

        /// <summary>
        /// SearchTranslationUnit
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="translationUnit"></param>
        /// <returns></returns>
        public SearchResults SearchTranslationUnit(SearchSettings settings, TranslationUnit translationUnit)
        {
            _inputTu = translationUnit;
            return SearchSegment(settings, translationUnit.SourceSegment);
        }

        /// <summary>
        /// SearchTranslationUnits
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="translationUnits"></param>
        /// <returns></returns>
        public SearchResults[] SearchTranslationUnits(SearchSettings settings, TranslationUnit[] translationUnits)
        {
            var results = new SearchResults[translationUnits.Length];
            for (var p = 0; p < translationUnits.Length; ++p)
            {
                //need to use the tu confirmation level in searchsegment method
                _inputTu = translationUnits[p];
                results[p] = SearchSegment(settings, translationUnits[p].SourceSegment); //changed this to send whole tu
            }
            return results;
        }

        /// <summary>
        /// SearchTranslationUnitsMasked
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="translationUnits"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public SearchResults[] SearchTranslationUnitsMasked(SearchSettings settings, TranslationUnit[] translationUnits, bool[] mask)
        {
            var results = new List<SearchResults>();
            var i = 0;
            foreach (var tu in translationUnits)
            {
                if (mask == null || mask[i])
                {
                    var result = SearchTranslationUnit(settings, tu);
                    results.Add(result);
                }
                else
                {
                    results.Add(null);
                }
                i++;
            }
            return results.ToArray();
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnit"></param>
        /// <returns></returns>
        public ImportResult UpdateTranslationUnit(TranslationUnit translationUnit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not required for this implementation.
        /// </summary>
        /// <param name="translationUnit"></param>
        /// <returns></returns>
        public ImportResult[] UpdateTranslationUnits(TranslationUnit[] translationUnits)
        {
            throw new NotImplementedException();
        }

#endregion

        #region Methods


        private SearchResults TranslateUntranslatedSegment(Segment segment, Segment translation, SearchResults results)
        {
            translation.Add(PluginResources.SegmentAlreadyTranslated);
            //later get these strings from resource file
            results.Add(CreateSearchResult(segment, translation));
            return results;
        }

        private Segment Translation(Segment translation, Segment newseg)
        {
            var sourcetext = newseg.ToPlain();
            var translatedText = LookupAmz(sourcetext);
            translation.Add(translatedText);
            return translation;
        }

        private Segment TranslationWithTags(Segment newseg)
        {
            //return our tagged target segment
            var tagplacer = new TagsServiceV2();
            tagplacer.SetOriginSegment(newseg);
            //tagplacer is constructed and gives us back a properly marked up source string 
            var translatedText = LookupAmz(tagplacer.PreparedSourceText);
            //now we send the output back to tagplacer for our properly tagged segment
            return tagplacer.GetTaggedSegment(translatedText).Duplicate();
        }

        /// <summary>
        /// Creates the translation unit as it is later shown in the Translation Results
        /// window of SDL Trados Studio. This member also determines the match score
        /// (in our implementation always 100%, as only exact matches are supported)
        /// as well as the confirmation level, i.e. Translated.
        /// </summary>
        /// <param name="searchSegment"></param>
        /// <param name="translation"></param>
        /// <param name="sourceSegment"></param>
        /// <returns></returns>

        private SearchResult CreateSearchResult(Segment searchSegment, Segment translation)
        {
            var tu = new TranslationUnit
            {
                SourceSegment = searchSegment.Duplicate(),//this makes the original source segment, with tags, appear in the search window
                TargetSegment = translation,
                Origin = _settings.TranslationType,
                ConfirmationLevel = ConfirmationLevel.Draft
            };
            tu.ResourceId = new PersistentObjectToken(tu.GetHashCode(), Guid.Empty);

            var searchResult = new SearchResult(tu)
            {
                ScoringResult = new ScoringResult
                {
                    BaseScore = _settings.Score
                }
            };

            return searchResult;
        }

        private string LookupAmz(string sourcetext)
        {
            var translatedText = _httpService.SendMessage(_settings, sourcetext, _languageDirection.SourceCulture, _languageDirection.TargetCulture, string.Empty);
            return translatedText.Tgt;
        }

        #endregion
    }
}
