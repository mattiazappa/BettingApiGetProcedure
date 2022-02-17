using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingGetProcedure.Models
{
    public class Match
    {
        [BsonElement("_id")]
        [JsonProperty("_id")]
        [BsonId]
        public ObjectId Id { get; set; }
        public string ApiId { get; set; }
        public string SportKey { get; set; }
        public string SportChampionship { get; set; }
        public string[] Teams { get; set; }
        public DateTime CommenceTime { get; set; }
        public string HomeTeam { get; set; }
        public List<Site> Sites { get; set; }
    }

    public class Site
    {
        public string SiteKey { get; set; }
        public string SiteName { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<Odds> odds { get; set; }
    }

    public class Odds
    {
        public double[] H2h { get; set; }
        public double[] LayH2h { get; set; }
        public DateTime Time { get; set; }
    }
}
