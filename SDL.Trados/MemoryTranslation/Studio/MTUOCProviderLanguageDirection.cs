using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sdl.Core.Globalization;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemory;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Tags;
using SDL.Trados.MTUOC.Services.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SDL.Trados.MTUOC.Studio
{
    [Obsolete("service used by old websocket protocol")]
    public class MTUOCProviderLanguageDirection : ITranslationProviderLanguageDirection
    {
        private static readonly Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly MTUOCProvider _mtuocProvider;
        private readonly SettingsDto _settings;
        private readonly LanguagePair _languageDirection;

        public MTUOCProviderLanguageDirection(MTUOCProvider provider, SettingsDto settings, LanguagePair languageDirection)
        {
            _mtuocProvider = provider;
            _settings = settings;
            _languageDirection = languageDirection;
        }

        #region ITranslationProviderLanguageDirection

        public ImportResult[] AddOrUpdateTranslationUnits(TranslationUnit[] translationUnits, int[] previousTranslationHashes, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        public ImportResult[] AddOrUpdateTranslationUnitsMasked(TranslationUnit[] translationUnits, int[] previousTranslationHashes, ImportSettings settings, bool[] mask)
        {
            throw new NotImplementedException();
        }

        public ImportResult AddTranslationUnit(TranslationUnit translationUnit, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        public ImportResult[] AddTranslationUnits(TranslationUnit[] translationUnits, ImportSettings settings)
        {
            throw new NotImplementedException();
        }

        public ImportResult[] AddTranslationUnitsMasked(TranslationUnit[] translationUnits, ImportSettings settings, bool[] mask)
        {
            throw new NotImplementedException();
        }

        public bool CanReverseLanguageDirection
        {
            get { return false; }
        }

        public SearchResults SearchSegment(SearchSettings settings, Segment segment)
        {
            return SearchSegment(segment);
        }

        public SearchResults[] SearchSegments(SearchSettings settings, Segment[] segments)
        {
            IList<SearchResults> results = new List<SearchResults>();
            foreach (var item in segments)
                results.Add(SearchSegment(settings, item));

            return results.ToArray();
        }

        public SearchResults[] SearchSegmentsMasked(SearchSettings settings, Segment[] segments, bool[] mask)
        {
            return SearchSegments(settings, segments);
        }

        public SearchResults SearchText(SearchSettings settings, string segment)
        {
            var translation = new Segment();
            var newSegment = new Segment();
            var results = new SearchResults
            {
                GetTextResult(translation, newSegment, segment)
            };
            return results;
        }

        public SearchResults SearchTranslationUnit(SearchSettings settings, TranslationUnit translationUnit)
        {
            return SearchSegment(settings, translationUnit.SourceSegment);
        }

        public SearchResults[] SearchTranslationUnits(SearchSettings settings, TranslationUnit[] translationUnits)
        {
            return SearchSegments(settings, translationUnits.Select(x => x.SourceSegment).ToArray());
        }

        public SearchResults[] SearchTranslationUnitsMasked(SearchSettings settings, TranslationUnit[] translationUnits, bool[] mask)
        {
            return InternalSearchTranslationUnitsMasked(settings, translationUnits, mask);
        }

#if (Version2019 || Version2021)
        public CultureInfo SourceLanguage
        {
            get { return _languageDirection.SourceCulture; }
        }

        public CultureInfo TargetLanguage
        {
            get { return _languageDirection.TargetCulture; }
        }
#else
        public CultureCode SourceLanguage
        {
            get { return _languageDirection.SourceCulture; }
        }

        public CultureCode TargetLanguage
        {
            get { return _languageDirection.TargetCulture; }
        }
#endif
        public ITranslationProvider TranslationProvider
        {
            get { return _mtuocProvider; }
        }

        public ImportResult UpdateTranslationUnit(TranslationUnit translationUnit)
        {
            throw new NotImplementedException();
        }

        public ImportResult[] UpdateTranslationUnits(TranslationUnit[] translationUnits)
        {
            throw new NotImplementedException();
        }

#endregion

        #region Methods

        private SearchResults[] InternalSearchTranslationUnitsMasked(SearchSettings settings, TranslationUnit[] translationUnits, bool[] mask)
        {
            var noOfResults = mask.Length;

            var results = new List<SearchResults>(noOfResults);
            var preTranslateList = new List<PreTranslateSegmentDto>(noOfResults);

            for (int i = 0; i < noOfResults; i++)
            {
                results.Add(null);
                preTranslateList.Add(null);
            }

            // plugin is called from pre-translate batch task
            // we receive the data in chunk of 10 segments
            if (translationUnits.Length > 2)
            {
                var i = 0;
                foreach (var tu in translationUnits)
                {
                    if (mask[i])
                    {
                        var preTranslate = new PreTranslateSegmentDto
                        {
                            SearchSettings = settings,
                            TranslationUnit = tu
                        };
                        preTranslateList.RemoveAt(i);
                        preTranslateList.Insert(i, preTranslate);
                    }
                    i++;
                }
                if (preTranslateList.Count > 0)
                {
                    //Create temp file with translations
                    var translatedSegments = PrepareTempData(preTranslateList).Result;
                    var preTranslateSearchResults = GetPreTranslationSearchResults(translatedSegments);

                    foreach (var result in preTranslateSearchResults)
                    {
                        if (result != null)
                        {
                            var index = preTranslateSearchResults.IndexOf(result);
                            results.RemoveAt(index);
                            results.Insert(index, result);
                        }
                    }
                }
            }
            else
            {
                var i = 0;
                foreach (var tu in translationUnits)
                {
                    if (mask[i])
                    {
                        var result = SearchTranslationUnit(settings, tu);
                        results.RemoveAt(i);
                        results.Insert(i, result);
                    }
                    i++;
                }
            }
            return results.ToArray();
        }

        private List<SearchResults> GetPreTranslationSearchResults(List<PreTranslateSegmentDto> preTranslateList)
        {
            var resultsList = new List<SearchResults>(preTranslateList.Capacity);

            for (int i = 0; i < resultsList.Capacity; i++)
            {
                resultsList.Add(null);
            }

            foreach (var preTranslate in preTranslateList)
            {
                if (preTranslate != null)
                {
                    var translation = new Segment(_languageDirection.TargetCulture);
                    var newSeg = preTranslate.TranslationUnit.SourceSegment.Duplicate();
                    if (newSeg.HasTags)
                    {
                        TagsService.Instance.GetSourceText(newSeg);
                        translation = TagsService.Instance.GetTaggedSegment(preTranslate.PlainTranslation);
                        preTranslate.TranslationSegment = translation;
                    }
                    else
                    {
                        translation.Add(preTranslate.PlainTranslation);
                    }

                    var searchResult = CreateSearchResult(newSeg, translation);
                    var results = new SearchResults
                    {
                        SourceSegment = newSeg
                    };
                    results.Add(searchResult);

                    var index = preTranslateList.IndexOf(preTranslate);
                    resultsList.RemoveAt(index);
                    resultsList.Insert(index, results);
                }
            }
            return resultsList;
        }

        private async Task<List<PreTranslateSegmentDto>> PrepareTempData(List<PreTranslateSegmentDto> preTranslateSegments)
        {
            try
            {
                for (var i = 0; i < preTranslateSegments.Count; i++)
                {
                    if (preTranslateSegments[i] != null)
                    {
                        string sourceText;
                        var newSeg = preTranslateSegments[i].TranslationUnit.SourceSegment.Duplicate();

                        if (newSeg.HasTags)
                        {
                            sourceText = TagsService.Instance.GetSourceText(newSeg);
                        }
                        else
                        {
                            sourceText = newSeg.ToPlain();
                        }

                        preTranslateSegments[i].SourceText = sourceText;
                    }
                }

                foreach (var item in preTranslateSegments)
                {
                    if (item != null)
                    {
                        item.PlainTranslation = await Startup.DIContainer.GetService<WebSocketFactory>().GetService().SendAsync(item.SourceText, string.Empty, _settings.FullServerUri);
                    }
                }

                return preTranslateSegments;
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}\n {e.StackTrace}");
            }

            preTranslateSegments.ForEach(seg =>
            {
                if (seg.PlainTranslation == null)
                    seg.PlainTranslation = string.Empty;
            });
            return preTranslateSegments;
        }

        private SearchResults SearchSegment(Segment segment)
        {
            var translation = new Segment(_languageDirection.TargetCulture);
            var results = new SearchResults { SourceSegment = segment.Duplicate() };

            try
            {
                var newSegment = segment.Duplicate();
                if (newSegment.HasTags)
                {
                    var sourceText = TagsService.Instance.GetSourceText(segment);
                    var translatedText = Task.Run(async () => await Startup.DIContainer.GetService<WebSocketFactory>().GetService().SendAsync(sourceText, string.Empty, _settings.FullServerUri)).Result;
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        translation = TagsService.Instance.GetTaggedSegment(translatedText);
                        results.Add(CreateSearchResult(newSegment, translation));
                    }
                }
                else
                {
                    var sourcetext = newSegment.ToPlain();
                    var textResult = GetTextResult(translation, newSegment, sourcetext);
                    if (textResult != null)
                        results.Add(textResult);
                }
            }
            catch (AggregateException agrEx)
            {
                _logger.Error($"SearchSegment method (Connection Disposed): {agrEx.Message}\n {agrEx.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"SearchSegment method: {ex.Message}\n {ex.StackTrace}");
                throw;
            }

            return results;
        }

        private SearchResult GetTextResult(Segment translation, Segment newSegment, string sourcetext)
        {
            var translatedText = Task.Run(async () => await Startup.DIContainer.GetService<WebSocketFactory>().GetService().SendAsync(sourcetext, string.Empty, _settings.FullServerUri)).Result;
            if (!string.IsNullOrEmpty(translatedText))
            {
                translation.Add(translatedText);
                return CreateSearchResult(newSegment, translation);
            }

            return null;
        }

        private SearchResult CreateSearchResult(Segment segment, Segment translation)
        {
            var tu = new TranslationUnit
            {
                SourceSegment = segment.Duplicate(),
                TargetSegment = translation
            };

            tu.ResourceId = new PersistentObjectToken(tu.GetHashCode(), Guid.Empty);
            
            tu.Origin = TranslationUnitOrigin.MachineTranslation;
            var searchResult = new SearchResult(tu)
            {
                TranslationProposal = new TranslationUnit(tu),
                ScoringResult = new ScoringResult
                {
                    BaseScore = _settings.Score
                }
            };
            tu.ConfirmationLevel = ConfirmationLevel.Translated;

            return searchResult;
        }

        #endregion
    }
}
