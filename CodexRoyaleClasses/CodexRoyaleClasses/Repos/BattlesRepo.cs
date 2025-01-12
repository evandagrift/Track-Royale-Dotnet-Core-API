﻿using CodexRoyaleClasses.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodexRoyaleClasses.Repos
{
    public class BattlesRepo
    {
        //DB Access
        private TRContext _context;
        private Client _client;
        private DecksRepo _decksRepo;
        //constructor, connects Connect argumented context




        //connection sting for the Codex API Controller that is being handled\
        private string officialConnectionString = "players/%23";


        public BattlesRepo(Client c, TRContext ct)
        {
            _context = ct;
            _client = c;
            _decksRepo = new DecksRepo(_client, _context);
        }

        public void AddBattle(Battle battle)
        {
            List<Battle> battles = new List<Battle>();

            battles.Add(battle);

            AddBattles(battles);
        }
        //adds given battle to the context
        public void AddBattles(List<Battle> battles)
        {
            //creates repos to handle deck and team intake/finding
            DecksRepo decksRepo = new DecksRepo(_client, _context);
            TeamsRepo teamsRepo = new TeamsRepo(_context);
            GameModesRepo gameModesRepo = new GameModesRepo(_context);

            //sorts newest first 
            battles.OrderByDescending(b => b.BattleTime);

            //battles that aren't already in the DB will be added to this list to be added
            List<Battle> battlesToSave = new List<Battle>();



            //battles not saved in DB will be added to the list to be saved
            battles.ForEach((b) =>
            {
                Battle savedBattle = null;

                if (b.Team != null)
                {
                    //sets team Id's for the players(Teams are unique to the Codex)
                    b.Team1Id = teamsRepo.GetSetTeamId(b.Team).TeamId;
                    b.Team2Id = teamsRepo.GetSetTeamId(b.Opponent).TeamId;
                    //fetches any battles at this time with these players in any combination
                    savedBattle = _context.Battles.Where(t => t.Team1Id == b.Team1Id && t.BattleTime == b.BattleTime ||
                    t.Team2Id == b.Team1Id && t.BattleTime == b.BattleTime).FirstOrDefault();

                }
                //if this battle isn't in DB save it adds it to the context to be saved
                if (savedBattle == null)
                {
                    battlesToSave.Add(b);
                }
            });

            //battles that weren't already saved will be prepped then added to the DB
            battlesToSave.ForEach(b =>
            {
                //sets the battle Id to 0 so EF Core can auto assign
                if (b.BattleId != 0) b.BattleId = 0;

                if (b.GameMode != null)
                {
                    //makes sure this gamemode is saved in DB
                    gameModesRepo.AddGameModeIfNew(b.GameMode);
                    //sets GameModeId
                    b.GameModeId = b.GameMode.Id;
                }
                if (b.Team != null)
                {
                    //trophies and how much they changed from the game
                    //only fetches one player from each team because 1v1 is the only format with trophies
                    b.Team1StartingTrophies = b.Team[0].StartingTrophies;
                    b.Team1TrophyChange = b.Team[0].TrophyChange;

                    b.Team2StartingTrophies = b.Opponent[0].StartingTrophies;
                    b.Team2TrophyChange = b.Opponent[0].TrophyChange;

                    //sets team names
                    b.Team1Name = b.Team[0].Name;
                    b.Team2Name = b.Opponent[0].Name;

                    //if 2v2 sets both allied names to their team name
                    if (b.Team.Count > 1)
                    {
                        b.Team1Name += " " + b.Team[1].Name;
                        b.Team2Name += " " + b.Opponent[1].Name;
                    }



                    //sets winners and losers, crowns are the overall score for a game
                    if (b.Team[0].Crowns > b.Opponent[0].Crowns)
                    {
                        b.Team1Win = true;
                        b.Team2Win = false;
                    }
                    else
                    {
                        b.Team1Win = false;
                        b.Team2Win = true;
                    }


                    //deck needs to be created because it is recieved from the official API as a List<Card>
                    Deck d = new Deck(b.Team[0].Cards);
                    //gets the deck Id, if this deck doesn't it exist it will also add it
                    b.Team1DeckAId = decksRepo.GetDeckId(d);

                    //repeat of above for opponent
                    d = new Deck(b.Opponent[0].Cards);
                    b.Team2DeckAId = decksRepo.GetDeckId(d);

                    //if it's 2v2 it will add the deckId for the second teammate
                    if (b.Team.Count > 1)
                    {
                        d = new Deck(b.Team[1].Cards);
                        b.Team1DeckBId = decksRepo.GetDeckId(d);
                        d = new Deck(b.Opponent[1].Cards);
                        b.Team2DeckBId = decksRepo.GetDeckId(d);
                    }

                    //setting score variables(2v2 both teammates will have the same values so variables are grabbed from player 1)
                    b.Team1Crowns = b.Team[0].Crowns;
                    b.Team1KingTowerHp = b.Team[0].KingTowerHitPoints;
                    b.Team1PrincessTowerHpA = b.Team[0].PrincessTowerA;
                    b.Team1PrincessTowerHpB = b.Team[0].PrincessTowerB;

                    b.Team2Crowns = b.Opponent[0].Crowns;
                    b.Team2KingTowerHp = b.Opponent[0].KingTowerHitPoints;
                    b.Team2PrincessTowerHpA = b.Opponent[0].PrincessTowerA;
                    b.Team2PrincessTowerHpB = b.Opponent[0].PrincessTowerB;


                }


                //adds this battle to context to be saved
                _context.Battles.Add(b);
            });

            //after all new battles are added to context changes are saved
            _context.SaveChanges();
        }

        //returns a list of all battles from DB
        public List<Battle> GetAllBattles() { return PopulateBattleDecks(_context.Battles.ToList()); }

        //returns a list of all battles from DB with specific tag
        public List<Battle> GetAllBattles(string playerTag)
        {
            if (playerTag != null)
            {
                Team playerTeam = _context.Teams.Where(t => t.Tag == playerTag && t.Tag2 == null).FirstOrDefault();
                if (playerTeam != null)
                {
                    List<Battle> playerBattles = _context.Battles.Where(b => b.Team1Id == playerTeam.TeamId || b.Team2Id == playerTeam.TeamId).OrderByDescending(b => b.BattleTime).ToList();
                    return PopulateBattleDecks(playerBattles);
                }
            }

            return null;
        }
        //returns a list of all battles from DB with specific tag
        public List<Battle> GetPlayersRecentBattles(string playerTag)
        {

            if (playerTag != null && _context.Battles.Count() > 0)
            {
                Team playerTeam = _context.Teams.Where(t => t.Tag == playerTag && t.TwoVTwo == false).FirstOrDefault();
                int numPlayerBattles = _context.Battles.Where(b => b.Team1Id == playerTeam.TeamId || b.Team2Id == playerTeam.TeamId).Count();

                int fetchThisMany = 30;

                if (fetchThisMany > numPlayerBattles) fetchThisMany = numPlayerBattles;

                if (fetchThisMany != 0)
                {
                    return PopulateBattleDecks(_context.Battles.Where(b => b.Team1Id == playerTeam.TeamId || b.Team2Id == playerTeam.TeamId).OrderByDescending(b => b.BattleTime).Take(fetchThisMany).ToList());
                }
                else return null;
            }
            else { return null; }
        }
        public List<Battle> GetRecentBattles()
        {
            int fetchThisMany = 30;

            if (fetchThisMany > _context.Battles.Count()) fetchThisMany = _context.Battles.Count();

            List<Battle> battlesToReturn = _context.Battles.OrderByDescending(b => b.BattleTime).Take(fetchThisMany).ToList();

            //battlesToReturn.ForEach(b =>
            //{
            //    b.Team1DeckA = _decksRepo.GetDeckByID(b.Team1DeckAId);
            //    b.Team2DeckA = _decksRepo.GetDeckByID(b.Team2DeckAId);
            //    if (b.Team1DeckBId != 0)
            //    {
            //        b.Team1DeckB = _decksRepo.GetDeckByID(b.Team1DeckBId);
            //        b.Team2DeckB = _decksRepo.GetDeckByID(b.Team2DeckBId);

            //    }
            //});

            return PopulateBattleDecks(battlesToReturn);
        }


        //gets battle with given battleID
        public Battle GetBattleByID(int battleID) { return _context.Battles.Find(battleID); }

        public Battle GetBattleWithId(Battle battle)
        {
            TeamsRepo teamsRepo = new TeamsRepo(_context);

            //gets/creates teams for Team and Opponent
            battle.Team1Id = teamsRepo.GetSetTeamId(battle.Team).TeamId;
            battle.Team2Id = teamsRepo.GetSetTeamId(battle.Opponent).TeamId;


            //finds battle in DB
            var battleToReturn = _context.Battles.Where(t => t.Team1Id == battle.Team1Id && t.BattleTime == battle.BattleTime ||
            t.Team2Id == battle.Team1Id && t.BattleTime == battle.BattleTime).FirstOrDefault();

            //if no battle is found it adds it
            if (battleToReturn == null)
            {
                //if battle doesn't exist add's battle
                AddBattle(battle);

                //searches for the added battle
                battleToReturn = _context.Battles.Where(t => t.Team1Id == battle.Team1Id && t.BattleTime == battle.BattleTime ||
             t.Team2Id == battle.Team1Id && t.BattleTime == battle.BattleTime).FirstOrDefault();
            }

            //returns fetched battle that is now assigned an Id
            return battleToReturn;
        }

        //deletes battle at given ID
        public void DeleteBattle(int battleID)
        {
            //checks if this battles exists before trying to delete
            if (_context.Battles.Any(b => b.BattleId == battleID))
            {
                //fetches the battle at given ID
                Battle battleToDelete = GetBattleByID(battleID);

                //removes it from the database and saves chages
                _context.Battles.Remove(battleToDelete);
                _context.SaveChanges();

            }
        }



        //updates battle at given ID
        public void UpdateBattle(Battle battle)
        {
            //checks if this battles exists before trying to Update it
            if (_context.Battles.Any(b => b.BattleId == battle.BattleId))
            {
                //fetches battle with given ID
                Battle battleToUpdate = GetBattleByID(battle.BattleId);

                //updates all the fields
                battleToUpdate.BattleTime = battle.BattleTime;

                battleToUpdate.Team1Name = battle.Team1Name;
                battleToUpdate.Team1Id = battle.Team1Id;
                battleToUpdate.Team1Win = battle.Team1Win;
                battleToUpdate.Team1StartingTrophies = battle.Team1StartingTrophies;
                battleToUpdate.Team1TrophyChange = battle.Team1TrophyChange;
                battleToUpdate.Team1DeckAId = battle.Team1DeckAId;
                battleToUpdate.Team1DeckBId = battle.Team1DeckBId;
                battleToUpdate.Team1Crowns = battle.Team1Crowns;
                battleToUpdate.Team1KingTowerHp = battle.Team1KingTowerHp;
                battleToUpdate.Team1PrincessTowerHpA = battle.Team1PrincessTowerHpA;
                battleToUpdate.Team1PrincessTowerHpB = battle.Team1PrincessTowerHpB;

                battleToUpdate.Team2Name = battle.Team2Name;
                battleToUpdate.Team2Id = battle.Team2Id;
                battleToUpdate.Team2Win = battle.Team2Win;
                battleToUpdate.Team2StartingTrophies = battle.Team2StartingTrophies;
                battleToUpdate.Team2TrophyChange = battle.Team2TrophyChange;
                battleToUpdate.Team2DeckAId = battle.Team2DeckAId;
                battleToUpdate.Team2DeckBId = battle.Team2DeckBId;
                battleToUpdate.Team2Crowns = battle.Team2Crowns;
                battleToUpdate.Team2KingTowerHp = battle.Team2KingTowerHp;
                battleToUpdate.Team2PrincessTowerHpA = battle.Team2PrincessTowerHpA;
                battleToUpdate.Team2PrincessTowerHpB = battle.Team2PrincessTowerHpB;

                battleToUpdate.Type = battle.Type;
                battleToUpdate.DeckSelection = battle.DeckSelection;
                battleToUpdate.IsLadderTournament = battle.IsLadderTournament;
                battleToUpdate.GameModeId = battle.GameModeId;

                //saves the changes to the database
                _context.SaveChanges();
            }
        }
        public List<Battle> PopulateBattleDecks(List<Battle> battles)
        {
            DecksRepo decksRepo = new DecksRepo(_client, _context);

            if (battles != null)
            {
                battles.ForEach(b =>
                {
                    b.Team1DeckA = decksRepo.GetDeckByID(b.Team1DeckAId);
                    b.Team2DeckA = decksRepo.GetDeckByID(b.Team2DeckAId);
                    if (b.Team1DeckBId != 0)
                    {
                        b.Team1DeckB = decksRepo.GetDeckByID(b.Team1DeckBId);
                        b.Team2DeckB = decksRepo.GetDeckByID(b.Team2DeckBId);

                    }
                });
                return battles;
            }
            else return null;
        }

        public async Task<List<Battle>> GetOfficialPlayerBattles(string tag)
        {
            //connection string to fetch player battles with given Tag
            string connectionString = officialConnectionString + tag.Substring(1) + "/battlelog/";

            //calls the official API
            var result = await _client.officialAPI.GetAsync(connectionString);

            //if the call is a success it returns the List of Battles
            if (result.IsSuccessStatusCode)
            {
                //content to json string once recieved and parsed
                var content = await result.Content.ReadAsStringAsync();

                //deserielizes the json to list of battles
                var battles = JsonConvert.DeserializeObject<List<Battle>>(content);

                //cleans up the time string, the official API includes a non functioning TimeZone offset to their datetime string
                battles.ForEach(b =>
                {
                    b.BattleTime = b.BattleTime.Substring(0, 15);
                });

                //returns fetched list of battles
                return battles;
            }
            return null;
        }


    }
}
