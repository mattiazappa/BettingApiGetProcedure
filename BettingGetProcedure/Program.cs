using RestSharp;
using System;
using System.Text.Json;
using MongoDB.Driver;
using BettingGetProcedure.Models;
using System.Collections.Generic;
using System.Net.Mail;

namespace BettingGetProcedure
{
    class Program
    {
        private static string connectionString = "mongodb://localhost:27017/";
        private static IMongoCollection<Match> _matchCollection;
        private static bool error = false;
        private static string errorList = "";
        private static DateTime time = DateTime.Now;

        public static void Main(string[] args)
        {
            var mongoClient = new MongoClient();
            _matchCollection = mongoClient.GetDatabase("betting").GetCollection<Match>("match");
            //deleteMatch();
            var client = new RestClient("https://api.the-odds-api.com/v3/odds/?apiKey=0ad4dae5435d39374c9a09366e157a69&sport=soccer_italy_serie_a&region=eu&dateFormat=iso"); //&mkt=h2h
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful)
            {
                ResultApi result = JsonSerializer.Deserialize<ResultApi>(response.Content);
                foreach (MatchApi match in result.data)
                {
                    Console.WriteLine(match.teams[0] + " - " + match.teams[1]);
                    bool matchExist = matchExists(match);
                    if (!matchExist)
                    {
                        createMatch(match);
                    }
                    else
                    {
                        updateMatch(match);
                    }
                }
            }
            else
            {
                error = true;
                errorList += "Chiamata API fallita";
            }

            if (error)
            {
                sendErrorEmail();
            }
        }

        public static void sendErrorEmail()
        {
            MailMessage message = new MailMessage("errorbetapp@gmail.com", "errorbetapp@gmail.com");
            message.Subject = "Errori nella procedura di importazione";
            message.Body = time.ToString("dd/MM/yyyy HH:mm:ss") + "\n\n"+ errorList;
            var gmailClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential("errorbetapp@gmail.com", "BetAppError")
            };

            try
            {
                gmailClient.Send(message);
                Console.WriteLine("Invio messaggio errori eseguito");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateTestMessage2(): {0}",
                    ex.ToString());
            }
        }

        //verifico esistenza match
        public static bool matchExists(MatchApi match)
        {
            var res = _matchCollection.Find(x => x.ApiId == match.id).FirstOrDefault();
            if (res != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //elimino match
        //public static void deleteMatch()
        //{
        //     
        //}

        //creo match
        public static void createMatch(MatchApi match)
        {
            var currentSites = new List<Site>();
            foreach (var site in match.sites)
            {
                var currentOdds = new List<Odds>();
                currentOdds.Add(new Odds
                {
                    H2h = site.odds.h2h,
                    LayH2h = site.odds.h2h_lay,
                    Time = time,
                });
                currentSites.Add(new Site
                {
                    LastUpdate = site.last_update,
                    SiteKey = site.site_key,
                    SiteName = site.site_nice,
                    odds = currentOdds
                });
            }
            var element = new Match
            {
                ApiId = match.id,
                CommenceTime = match.commence_time,
                SportChampionship = match.sport_nice,
                SportKey = match.sport_key,
                HomeTeam = match.home_team,
                Sites = currentSites,
                Teams = match.teams
            };
            try
            {
                _matchCollection.InsertOne(element);
            }
            catch
            {
                error = true;
                errorList += "Creazione match fallita: " + match.sport_key + " || " + match.teams[0] + " - " + match.teams[1]+"\n";
            }
        }

        public static void updateMatch(MatchApi match)
        {
            //aggiorno data di inizio per partite rinviate
            _matchCollection.UpdateOne(Builders<Match>.Filter.Where(x => x.ApiId == match.id),
                Builders<Match>.Update.Set(x => x.CommenceTime, match.commence_time));
            //aggiorno odds
            foreach (var elem in match.sites)
            {
                var filter = Builders<Match>.Filter.And(
                    Builders<Match>.Filter.Eq(x => x.ApiId, match.id),
                    Builders<Match>.Filter.ElemMatch(x => x.Sites, c => c.SiteKey == elem.site_key)
                    );
                var odd = new Odds
                {
                    H2h = elem.odds.h2h,
                    LayH2h = elem.odds.h2h_lay,
                    Time = time,
                };

                if (_matchCollection.Find(filter).FirstOrDefault() != null)
                {
                    try
                    {
                        _matchCollection.UpdateOne(filter,
                            Builders<Match>.Update.Set(x => x.Sites[-1].LastUpdate, elem.last_update)
                            .AddToSet(x => x.Sites[-1].odds, odd));

                        //_matchCollection.UpdateOne(filter,
                        //    Builders<Match>.Update.Set(x => x.Sites.Find(x=>x.SiteKey == elem.site_key).LastUpdate, elem.last_update)
                        //    .AddToSet(x => x.Sites.Find(x => x.SiteKey == elem.site_key).odds, odd));
                    }
                    catch
                    {
                        error = true;
                        errorList += "Aggiornamento match fallito: " + match.id + " || " + elem.site_key + "\n";
                    }
                }
                else
                {
                    var currentOdds = new List<Odds>();
                    currentOdds.Add(odd);
                    try
                    {
                        _matchCollection.UpdateOne(
                        Builders<Match>.Filter.Eq(x => x.ApiId, match.id),
                        Builders<Match>.Update.AddToSet(x => x.Sites, new Site
                        {
                            LastUpdate = elem.last_update,
                            SiteKey = elem.site_key,
                            SiteName = elem.site_nice,
                            odds = currentOdds
                        }));
                    }
                    catch
                    {
                        error = true;
                        errorList += "Inserimento nuovo site fallito: " + match.id + " || " + elem.site_key + "\n";

                    }
                }
                //var update = Builders<Match>.Update.Set(x=>x.Sites.fin)
                //_matchCollection.UpdateOne(filter, update)
            }
        }
    }
}
