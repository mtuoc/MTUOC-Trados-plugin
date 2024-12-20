using NLog;
using Sdl.LanguagePlatform.Core;
using SDL.Trados.MTUOC.DTO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SDL.Trados.MTUOC.Services.Tags
{
    [Obsolete("service used by old websocket protocol")]
    internal sealed class TagsService : ITagsService
    {
        public static ITagsService Instance { get; } = new TagsService();
        private Dictionary<string, TagDto> _tagsDictionary;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private TagsService()
        {
        }

        public Segment GetTaggedSegment(string text)
        {
            var segment = new Segment(); //our segment to return
                                         //get our array of elements..it will be array of tagtexts and text in the order received from google
            try
            {
                var targetElements = GetTargetElements(text);
                //build our segment looping through elements

                for (var i = 0; i < targetElements.Length; i++)
                {
                    var itemText = targetElements[i]; //the text to be compared/added
                    if (_tagsDictionary.ContainsKey(itemText)) //if our text in question is in the tagtext list
                    {
                        var padleft = _tagsDictionary[itemText].PadLeft;
                        var padright = _tagsDictionary[itemText].PadRight;
                        if (padleft.Length > 0) segment.Add(padleft); //add leading space if applicable in the source text
                        segment.Add(_tagsDictionary[itemText].SdlTag); //add the actual tag element after casting it back to a Tag
                        if (padright.Length > 0)
                            segment.Add(padright); //add trailing space if applicable in the source text
                    }
                    else
                    {
                        //if it is not in the list of tagtexts then the element is just the text
                        if (text.Trim().Length > 0) //if the element is something other than whitespace, i.e. some text in addition
                        {
                            text = text.Trim(); //trim out extra spaces, since they are dealt with by associating them with the tags
                            segment.Add(text); //add to the segment
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}\n {e.StackTrace}");
            }

            return segment; //this will return a tagged segment
        }

        public string GetSourceText(Segment sourceSegment)
        {
            var result = string.Empty;
            _tagsDictionary = new Dictionary<string, TagDto>();
            try
            {
                for (var i = 0; i < sourceSegment.Elements.Count; i++)
                {
                    var elType = sourceSegment.Elements[i].GetType();

                    if (elType.ToString() == "Sdl.LanguagePlatform.Core.Tag") //if tag, add to dictionary
                    {
                        var theTag = new TagDto((Tag)sourceSegment.Elements[i].Duplicate());
                        var tagText = string.Empty;
                        var tagId = theTag.SdlTag.TagID;
                        if (theTag.SdlTag.Type == TagType.Start)
                        {
                            tagText = "<tg" + tagId + ">";
                        }
                        if (theTag.SdlTag.Type == TagType.End)
                        {
                            tagText = "</tg" + tagId + ">";
                        }
                        if (theTag.SdlTag.Type == TagType.Standalone || theTag.SdlTag.Type == TagType.TextPlaceholder || theTag.SdlTag.Type == TagType.LockedContent)
                        {
                            tagText = "<tg" + tagId + "/>";
                        }

                        result += tagText;

                        //now we have to figure out whether this tag is preceded and/or followed by whitespace
                        if (i > 0 && !sourceSegment.Elements[i - 1].GetType().ToString().Equals("Sdl.LanguagePlatform.Core.Tag"))
                        {
                            var prevText = sourceSegment.Elements[i - 1].ToString();
                            if (!string.IsNullOrEmpty(prevText.Trim()))//and not just whitespace
                            {
                                //get number of trailing spaces for that segment
                                var whitespace = prevText.Length - prevText.TrimEnd().Length;
                                //add that trailing space to our tag as leading space
                                theTag.PadLeft = prevText.Substring(prevText.Length - whitespace);
                            }
                        }
                        if (i < sourceSegment.Elements.Count - 1 && !sourceSegment.Elements[i + 1].GetType().ToString().Equals("Sdl.LanguagePlatform.Core.Tag"))
                        {
                            //here we don't care whether it is only whitespace
                            //get number of leading spaces for that segment
                            var nextText = sourceSegment.Elements[i + 1].ToString();
                            var whitespace = nextText.Length - nextText.TrimStart().Length;
                            //add that trailing space to our tag as leading space
                            theTag.PadRight = nextText.Substring(0, whitespace);
                        }

                        //add our new tag code to the dict with the corresponding tag if it's not already there
                        if (!_tagsDictionary.ContainsKey(tagText))
                        {
                            _tagsDictionary.Add(tagText, theTag);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"{e.Message}\n {e.StackTrace}");
            }
            return result;
        }

        /// <summary>
        /// puts returned string into an array of elements
        /// </summary>
        /// <returns></returns>
        private string[] GetTargetElements(string text)
        {
            //first create a regex to put our array separators around the tags
            var str = text;
            const string aplhanumericPattern = @"</?([a-z]*)[0-9]*/?>";

            var alphaRgx = new Regex(aplhanumericPattern, RegexOptions.IgnoreCase);
            var alphaMatches = alphaRgx.Matches(str);
            if (alphaMatches.Count > 0)
            {
                str = AddSeparators(str, alphaMatches);
            }

            var stringSeparators = new[] { "```" };
            var strAr = str.Split(stringSeparators, StringSplitOptions.None);
            return strAr;
        }

        private string AddSeparators(string text, MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                text = text.Replace(match.Value, "```" + match.Value + "```");
            }
            return text;
        }
    }
}
