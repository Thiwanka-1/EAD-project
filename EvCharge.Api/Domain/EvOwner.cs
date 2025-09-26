using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EvCharge.Api.Domain
{
    public class EvOwner
    {
        [BsonId] // primary key
        [BsonRepresentation(BsonType.String)]
        public string NIC { get; set; } = string.Empty; // NIC as primary key

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true; // default active
    }
}
