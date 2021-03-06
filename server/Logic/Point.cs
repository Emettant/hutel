using System;
using System.Linq;
using System.Collections.Generic;
using hutel.Models;

namespace hutel.Logic
{
    public class Point
    {
        public static readonly IList<string> ReservedFields =
            new List<string> { "id", "tagId", "date" };

        public Guid Id { get; set; }
        
        public string TagId { get; set; }
        
        public HutelDate Date { get; set; }

        public Dictionary<string, object> Extra { get; set; }

        public PointWithIdDataContract ToDataContract(Dictionary<string, Tag> tags)
        {
            var tag = tags[TagId];
            var jsonExtra = Extra.ToDictionary(
                kvPair => kvPair.Key,
                kvPair => tag.Fields[kvPair.Key].ValueToDataContract(kvPair.Value));
            return new PointWithIdDataContract
            {
                Id = Id,
                TagId = TagId,
                Date = Date.ToString(),
                Extra = jsonExtra
            };
        }

        public static Point FromDataContract(
            PointDataContract input, Guid id, Dictionary<string, Tag> tags)
        {
            return FromFields(id, input.TagId, input.Date, input.Extra, tags);
        }

        public static Point FromDataContract(
            PointWithIdDataContract input, Dictionary<string, Tag> tags)
        {
            return FromFields(input.Id, input.TagId, input.Date, input.Extra, tags);
        }

        private static Point FromFields(
            Guid id,
            string tagId,
            string date,
            Dictionary<string, Object> extra,
            Dictionary<string, Tag> tags)
        {
            if (!tags.ContainsKey(tagId))
            {
                throw new PointValidationException($"Unknown tag: {tagId}");
            }
            var tag = tags[tagId];
            foreach (var pointField in extra.Keys)
            {
                if (!tag.Fields.ContainsKey(pointField))
                {
                    throw new PointValidationException($"Unknown property: {pointField}");
                }
            }
            var pointExtra = new Dictionary<string, Object>();
            foreach (var tagField in tag.Fields.Values)
            {
                if (!extra.ContainsKey(tagField.Name))
                {
                    throw new PointValidationException($"Property not found: {tagField.Name}");
                }
                try
                {
                    pointExtra.Add(
                        tagField.Name, tagField.ValueFromDataContract(extra[tagField.Name]));
                }
                catch(TypeValidationException ex)
                {
                    throw new PointValidationException(
                        $"Malformed property: {extra[tagField.Name]}", ex);
                }
            }
            try
            {
                var pointDate = new HutelDate(date);
                return new Point
                {
                    Id = id,
                    TagId = tagId,
                    Date = pointDate,
                    Extra = pointExtra
                };
            }
            catch (FormatException ex)
            {
                throw new PointValidationException($"Malformed date: {date}", ex);
            }
        }
    }
    public class PointValidationException: Exception
    {
        public PointValidationException()
        {
        }

        public PointValidationException(string message): base(message)
        {
        }

        public PointValidationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
