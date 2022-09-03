using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using IdentityModel.Client;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Http.Headers;
using System.Xml.Linq;
using WCLAnalysis.Models;
using WCLAPI.Models;

namespace WCLAPI.Controllers
{
    [EnableCors]
    [ApiController]
    [Route("[controller]")]
    public class PlayerStatsController : ControllerBase
    {
        private static string _wclAPITokenUrl = "https://www.warcraftlogs.com/oauth/token";
        private static string _wclAPIUrlBase = "https://classic.warcraftlogs.com/api/v2/client";

        private readonly ILogger<PlayerStatsController> _logger;

        private static string _token = "";

        private async Task<string> GetTokenAsync()
        {
            if (_token != "") return _token;
             using var client = new HttpClient();
            // Setting Base address.  
            client.BaseAddress = new Uri(_wclAPITokenUrl);

            // Setting content type.  
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                // hard coded credentials for WCL using client/secret method to connect fetching data ========
                Address = _wclAPITokenUrl,
                ClientId = "970e124a-0af1-4f27-a9a4-23a22866e6a9",
                ClientSecret = "a74I3jpEnlfjrQxCV7dItLcvEPhas3BWEESWt5Da"
            });
            return tokenResponse.AccessToken;
        }

        public PlayerStatsController(ILogger<PlayerStatsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<PlayerStats>> Get([FromQuery]string code)
        {
            // Initialization.   
            // Posting.  
            if (string.IsNullOrEmpty(code)) return new List<PlayerStats>();


            _token = await GetTokenAsync();

            //var result = await client.GetAsync(_wclAPIUrlBase);
            var graphQLClient = new GraphQLHttpClient(_wclAPIUrlBase, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.SetBearerToken(_token);

            // Initialization.
            // get total fight encounters
            var fightRequest = new GraphQLRequest
            {
                Query = $"query {{ reportData {{ report(code:\"{code}\"){{fights(killType:Encounters){{encounterID}}}}}}}}"
            };


            var masterRequest = new GraphQLRequest
            {
                Query = $"query {{ reportData {{ report(code:\"{code}\"){{masterData{{actors(type:\"Player\"){{id,name,subType}}}},rankings}}}}}}"
            };

            var petRequest = new GraphQLRequest
            {
                Query = $"query {{ reportData {{ report(code:\"{code}\"){{masterData{{actors(type:\"Pet\"){{id,name,petOwner}}}}}}}}}}"
            };

            var graphQLResponse = await graphQLClient.SendQueryAsync<ResponseFightsCollectionType>(fightRequest);
            var totalEncounters = graphQLResponse?.Data?.ReportData?.Report?.Fights?.Count;
            //master data to get players and pets and abilities
            var masterRes = await graphQLClient.SendQueryAsync<ResponseFightsCollectionType>(masterRequest);
            var playerList = masterRes?.Data?.ReportData?.Report?.MasterData;
            var petRes = await graphQLClient.SendQueryAsync<ResponseFightsCollectionType>(petRequest);
            var petList = petRes?.Data?.ReportData?.Report?.MasterData;
            var rankings = masterRes?.Data?.ReportData?.Report?.Rankings;

            //now get fight events for the report
            var manaPotData = await GetEvents(graphQLClient, code, 28499);
            var destroData = await GetEvents(graphQLClient, code, 28507);
            var hasteData = await GetEvents(graphQLClient, code, 28494);
            var strengthData = await GetEvents(graphQLClient, code, 28508);
            var ironShieldData = await GetEvents(graphQLClient, code, 28515);
            var runeData = await GetEvents(graphQLClient, code, 27869);
            var demonicData = await GetEvents(graphQLClient, code, 16666);

            var sumData = manaPotData?.Data?.Concat(destroData.Data).Concat(hasteData.Data).Concat(strengthData.Data).Concat(ironShieldData.Data).Concat(runeData.Data).Concat(demonicData.Data).ToList();

            var agiScrolls = await GetTable(graphQLClient, code, 33077);
            var strScrolls = await GetTable(graphQLClient, code, 33082);
            // food, can add more later on for lvl 80 =================
            var foodOne = await GetTable(graphQLClient, code, 43764);
            var foodTwo = await GetTable(graphQLClient, code, 33261);
            var foodThree = await GetTable(graphQLClient, code, 33265);         
            var foodFour = await GetTable(graphQLClient, code, 33259);
            var foodFive = await GetTable(graphQLClient, code, 43722);
            var foodSix = await GetTable(graphQLClient, code, 33263);
            var foodSeven = await GetTable(graphQLClient, code, 33257);
            var foodEight = await GetTable(graphQLClient, code, 33268);
            var foodNine = await GetTable(graphQLClient, code, 33256);

            var foodUsage = foodOne.Concat(foodTwo).Concat(foodThree).Concat(foodFour).Concat(foodFive).Concat(foodSix).Concat(foodSeven).Concat(foodEight).Concat(foodNine).ToList();

            // digest event and master data
            return DigestData(sumData, playerList, petList, agiScrolls, strScrolls, rankings, foodUsage??new List<TableData>(), totalEncounters??0);


        }

        private async Task<Event> GetEvents(GraphQLHttpClient graphQLClient, string code, int ability)
        {
            var req = new GraphQLRequest
            {
                Query = $"query {{ reportData {{ report(code:\"{code}\"){{events(killType:Encounters, endTime:10000000000, dataType:Casts, abilityID: {ability}){{data}}}}}}}}"
            };
            var res = await graphQLClient.SendQueryAsync<ResponseFightsCollectionType>(req);
            return res?.Data?.ReportData?.Report?.Events?? new Event();
        }


        private async Task<List<TableData>> GetTable(GraphQLHttpClient graphQLClient, string code, int ability)
        {
            var req = new GraphQLRequest
            {
                Query = $"query {{ reportData {{ report(code:\"{code}\"){{table(killType:Encounters, endTime:10000000000, dataType:Buffs, abilityID: {ability})}}}}}}"
            };
            var res = await graphQLClient.SendQueryAsync<ResponseFightsCollectionType>(req);
            return res?.Data?.ReportData?.Report?.Table?.Data?.Auras ?? new List<TableData>();

        }



        private List<PlayerStats> DigestData(List<Data>? eventData, MasterData? playerList, MasterData? petList, List<TableData> agiScrolls, List<TableData> strScrolls, Ranking? rankings, List<TableData> foodUsages, int encounters)
        {
            var sum = new List<PlayerStats>();
            var validRanks = rankings?.Data.Where(x => x.Encounter != null && x.Encounter.Id != 724).ToList(); // exclude kalecgos

            var playerRanks = validRanks?.Select(x => x.Roles).ToList();
            var players = GetPlayerRanks(playerRanks?? new List<Role>());
            

            playerList?.Actors?.ForEach((a) =>
            {
                if (a.SubType == "Unknown") return; // ignore unknown types, mainly noises
                sum.Add(new PlayerStats { Id = a.Id, Name = a.Name, UtilPotUsed = GetUtilPotUsed(eventData, a.Id), ManaPotUsed = GetManaPotUsed(eventData, a.Id), ScrollUsed = GetScrollUsed(agiScrolls, strScrolls, petList?.Actors?.Where(x => x.PetOwner == a.Id).FirstOrDefault(), a.Id), WCL = players.Where(x => x.Name == a.Name).FirstOrDefault() == null ? 0 : players.Where(x => x.Name == a.Name).First().WCLAvgRanking, Encounters = encounters, Food = foodUsages.Where(x => x.id == a.Id).Select(x => x.totalUses).ToList().Sum() }) ;
            });

            return sum;
        }

        private List<Player> GetPlayerRanks(List<Role> playerRanks)
        {
            var players = new List<Player>();
            playerRanks.ForEach(r =>
            {
                r.Tanks.Characters.ForEach(c =>
                {
                    var existingPlayer = players.Where(x => x.Name == c.Name).FirstOrDefault();
                    if (existingPlayer != null)
                    {
                        // only add ranking into the player
                        existingPlayer.EncounterRanking.Add(c.RankPercent);
                    }
                    else
                    {
                        players.Add(new Player { Name = c.Name, EncounterRanking = new List<int>() { c.RankPercent } });
                    }
                });
                r.Healers.Characters.ForEach(c =>
                {
                    var existingPlayer = players.Where(x => x.Name == c.Name).FirstOrDefault();
                    if (existingPlayer != null)
                    {
                        // only add ranking into the player
                        existingPlayer.EncounterRanking.Add(c.RankPercent);
                    }
                    else
                    {
                        players.Add(new Player { Name = c.Name, EncounterRanking = new List<int>() { c.RankPercent } });
                    }
                });
                r.Dps.Characters.ForEach(c =>
                {
                    var existingPlayer = players.Where(x => x.Name == c.Name).FirstOrDefault();
                    if (existingPlayer != null)
                    {
                        // only add ranking into the player
                        existingPlayer.EncounterRanking.Add(c.RankPercent);
                    }
                    else
                    {
                        players.Add(new Player { Name = c.Name, EncounterRanking = new List<int>() { c.RankPercent } });
                    }
                });
            });
            return players;
        }

        private int GetScrollUsed(List<TableData> agiScrolls, List<TableData> strScrolls, Actor? pet, int id)
        {
            // get player usage of agi/str scrolls
            var agi = agiScrolls.Where(x => x.id == id).FirstOrDefault()?.totalUses;
            var str = strScrolls.Where(x => x.id == id).FirstOrDefault()?.totalUses;
            int? petAgi = null;
            int? petStr = null;
            if (pet != null)
            {
                petAgi = agiScrolls.Where(x => x.id == pet.Id).FirstOrDefault()?.totalUses;
                petStr = strScrolls.Where(x => x.id == pet.Id).FirstOrDefault()?.totalUses;
            }

            int agiVal = agi ?? 0;
            int strVal = str ?? 0;
            int petAgiVal = petAgi ?? 0;
            int petStrVal = petStr ?? 0;

            return (int)(agiVal+ strVal + petAgiVal + petStrVal);

        }

        private int GetManaPotUsed(List<Data>? datas, int id)
        {
            // fetch the player' total use of mana pots
            var manaPots = datas?.Where(x => x.SourceID == id && (x.AbilityGameID == 28499)).ToList();
            return manaPots == null ? 0 : manaPots.Count;
        }

        private int GetUtilPotUsed(List<Data>? datas, int sourceID)
        {
            // fetch the player' total use of destruction/insane strength/haste potion
            var utilPots = datas?.Where(x => x.SourceID == sourceID &&  (x.AbilityGameID == 28507 || x.AbilityGameID == 28494 || x.AbilityGameID == 28508 || x.AbilityGameID == 28515 || x.AbilityGameID == 27869 || x.AbilityGameID == 16666)).ToList();
            return utilPots == null ? 0 : utilPots.Count;
        }
    }

    public class ResponseFightsCollectionType
    {
        public ReportData? ReportData { get; set; }
    }

}
