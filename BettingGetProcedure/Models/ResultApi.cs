using System; 
namespace BettingGetProcedure
{
    class ResultApi
    {
        public bool? success { get; set; }
        public MatchApi [] data { get; set; }
    }

    class MatchApi
    {
        public string id { get; set; }
        public string sport_key { get; set; }
        public string sport_nice { get; set; }
        public string[] teams { get; set; }
        public DateTime commence_time { get; set; }
        public string home_team { get; set; }
        public SitesApi[] sites { get; set; }
    }

    class SitesApi
    {
        public string site_key { get; set; }
        public string site_nice { get; set; }
        public DateTime last_update { get; set; }
        public OddsApi odds { get; set; }

    }

    class OddsApi
    {
        public double[] h2h { get; set; }
        public double[] h2h_lay { get; set; }
    }
}
