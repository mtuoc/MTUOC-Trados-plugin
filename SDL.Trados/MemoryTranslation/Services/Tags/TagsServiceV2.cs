using Sdl.LanguagePlatform.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SDL.Trados.MTUOC.Services.Tags
{
    internal class TagInfo
    {
        public string TagId { get; set; }
        public int Index { get; set; }
        public TagType TagType { get; set; }
        public bool IsClosed { get; set; }
    }

    /// <summary>
    /// Used to add info to associate with an SDL tag object
    /// </summary>
    internal class MtTag
    {
        internal MtTag(Tag tag)
        {
            this.SdlTag = tag;
            PadLeft = string.Empty;
            PadRight = string.Empty;
        }

        internal string PadLeft { get; set; }

        internal string PadRight { get; set; }

        internal Tag SdlTag { get; }
    }

    internal sealed class TagsServiceV2 : ITagsServiceV2
    {
        private string _returnedText;
        private Segment _sourceSegment;
        private Dictionary<string, MtTag> dict;

        public List<TagInfo> TagsInfo { get; set; }
        public TagsServiceV2()
        {

        }

        /// <summary>
        /// Returns the source text with markup replacing the tags in the source segment
        /// </summary>
        public string PreparedSourceText { get; private set; }

        /// <summary>
        /// Returns a tagged segments from a target string containing markup, where the target string represents the translation of the class instance's source segment
        /// </summary>
        /// <param name="returnedText"></param>
        /// <returns></returns>
        public Segment GetTaggedSegment(string returnedText)
        {
            //decode the returned text
            _returnedText = returnedText;

            //our dictionary, dict, is already built
            var segment = new Segment(); //our segment to return
            var targetElements = GetTargetElements();//get our array of elements..it will be array of tagtexts and text in the order received from google

            //build our segment looping through elements
            for (var i = 0; i < targetElements.Length; i++)
            {
                var text = targetElements[i]; //the text to be compared/added
                if (dict.ContainsKey(text)) //if our text in question is in the tagtext list
                {
                    try
                    {
                        var padleft = dict[text].PadLeft;
                        var padright = dict[text].PadRight;
                        if (padleft.Length > 0) segment.Add(padleft); //add leading space if applicable in the source text
                        segment.Add(dict[text].SdlTag); //add the actual tag element after casting it back to a Tag
                        if (padright.Length > 0) segment.Add(padright); //add trailing space if applicable in the source text
                    }
                    catch
                    { }
                }
                else
                {   //if it is not in the list of tagtexts then the element is just the text
                    if (text.Trim().Length > 0) //if the element is something other than whitespace, i.e. some text in addition
                    {
                        text = text.Trim(); //trim out extra spaces, since they are dealt with by associating them with the tags
                        segment.Add(text); //add to the segment
                    }
                }
            }
            //Microsoft sends back closing tags that need to be removed
            //   segment = RemoveTrailingClosingTags(segment);

            return segment; //this will return a tagged segment
        }

        /// <summary>
        /// Set de original segment
        /// </summary>
        /// <param name="source"></param>
        public void SetOriginSegment(Segment source)
        {
            _sourceSegment = source;
            TagsInfo = new List<TagInfo>();
            dict = GetSourceTagsDict();
        }

        private Dictionary<string, MtTag> GetSourceTagsDict()
        {
            dict = new Dictionary<string, MtTag>(); //try this
                                                    //build dict
            for (var i = 0; i < _sourceSegment.Elements.Count; i++)
            {
                var elType = _sourceSegment.Elements[i].GetType();

                if (elType.ToString() == "Sdl.LanguagePlatform.Core.Tag") //if tag, add to dictionary
                {
                    var theTag = new MtTag((Tag)_sourceSegment.Elements[i].Duplicate());
                    var tagText = string.Empty;

                    var tagInfo = new TagInfo
                    {
                        TagType = theTag.SdlTag.Type,
                        Index = i,
                        IsClosed = false,
                        TagId = theTag.SdlTag.TagID
                    };
                    if (!TagsInfo.Any(n => n.TagId.Equals(tagInfo.TagId)))
                    {
                        TagsInfo.Add(tagInfo);
                    }

                    var tag = GetCorrespondingTag(theTag.SdlTag.TagID);
                    if (theTag.SdlTag.Type == TagType.Start)
                    {
                        if (tag != null)
                        {
                            tagText = "<tg" + tag.TagId + ">";
                        }
                    }
                    if (theTag.SdlTag.Type == TagType.End)
                    {
                        if (tag != null)
                        {
                            tag.IsClosed = true;
                            tagText = "</tg" + tag.TagId + ">";
                        }
                    }
                    if (theTag.SdlTag.Type == TagType.Standalone || theTag.SdlTag.Type == TagType.TextPlaceholder)
                    {
                        if (tag != null)
                        {
                            tagText = "<tg" + tag.TagId + "/>";
                        }
                    }
                    PreparedSourceText += tagText;
                    //now we have to figure out whether this tag is preceded and/or followed by whitespace
                    if (i > 0 && !_sourceSegment.Elements[i - 1].GetType().ToString().Equals("Sdl.LanguagePlatform.Core.Tag"))
                    {
                        var prevText = _sourceSegment.Elements[i - 1].ToString();
                        if (!prevText.Trim().Equals(""))//and not just whitespace
                        {
                            //get number of trailing spaces for that segment
                            var whitespace = prevText.Length - prevText.TrimEnd().Length;
                            //add that trailing space to our tag as leading space
                            theTag.PadLeft = prevText.Substring(prevText.Length - whitespace);
                        }
                    }
                    if (i < _sourceSegment.Elements.Count - 1 && !_sourceSegment.Elements[i + 1].GetType().ToString().Equals("Sdl.LanguagePlatform.Core.Tag"))
                    {
                        //here we don't care whether it is only whitespace
                        //get number of leading spaces for that segment
                        var nextText = _sourceSegment.Elements[i + 1].ToString();
                        var whitespace = nextText.Length - nextText.TrimStart().Length;
                        //add that trailing space to our tag as leading space
                        theTag.PadRight = nextText.Substring(0, whitespace);
                    }
                    dict.Add(tagText, theTag); //add our new tag code to the dict with the corresponding tag
                }
                else
                {
                    PreparedSourceText += _sourceSegment.Elements[i].ToString();
                }
            }
            TagsInfo.Clear();
            return dict;
        }

        private TagInfo GetCorrespondingTag(string tagId)
        {
            return TagsInfo.FirstOrDefault(t => t.TagId.Equals(tagId));

        }

        /// <summary>
        /// puts returned string into an array of elements
        /// </summary>
        /// <returns></returns>
        private string[] GetTargetElements()
        {
            //first create a regex to put our array separators around the tags
            var str = _returnedText;
            var pattern = @"(<tg[0-9a-z]*\>)|(<\/tg[0-9a-z]*\>)|(\<tg[0-9a-z]*/\>)";
            var rgx = new Regex(pattern);
            var matches = rgx.Matches(_returnedText);

            foreach (Match myMatch in matches)
            {
                str = str.Replace(myMatch.Value, "```" + myMatch.Value + "```"); //puts our separator around tagtexts
            }
            var stringSeparators = new[] { "```" }; //split at our inserted marker....is there a better way?
            var strAr = str.Split(stringSeparators, StringSplitOptions.None);
            return strAr;
        }
    }
}
