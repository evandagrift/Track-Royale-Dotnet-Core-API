﻿using Newtonsoft.Json;
using RoyaleTrackerAPI.Models;
using RoyaleTrackerClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoyaleTrackerAPI.Repos
{
    public class ClansRepo
    {
        //DB Access
        private TRContext context;
        private Client client;

        //assigns argumented context
        public ClansRepo(Client c, TRContext ct) { context = ct; client = c; }

        //adds given clan to context
        public void AddClan(Clan clan) 
        { 
            context.Clans.Add(clan);
            context.SaveChanges();
        }

        //gets clan data from the official api via their clan tag
        public async Task<Clan> GetOfficialClan(string tag)
        {
            string officialConnectionString = "/v1/clans/%23";

            if (tag != null)
            {
                try
                {
                    //connection string for clan in offical API
                    string connectionString = officialConnectionString + tag.Substring(1);

                    //fetches clan data
                    var result = await client.officialAPI.GetAsync(connectionString);


                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();

                        //deseriealizes json into Clan object
                        Clan clan = JsonConvert.DeserializeObject<Clan>(content);

                        //sets location code to a format that the DB can consume
                        clan.LocationCode = (clan.Location["isCountry"] == "false") ? "International" : clan.Location["countryCode"];

                        //update time in same format as official API
                        clan.UpdateTime = DateTime.UtcNow.ToString("yyyyMMddTHHmmss");

                        return clan;
                    }
                }
                catch { return null; }
            }
            return null;
        }
        //Deletes clan with given clanTag
        public void DeleteClan(int id)
        {
            //fetches clan with given clan tag
            Clan clanToDelete = context.Clans.Find(id);

            //if a valid clan is fetched from the database it removes it from the context
            if(clanToDelete != null)
            {
                context.Clans.Remove(clanToDelete);
                context.SaveChanges();
            }
        }

        //Returns a List of all Clans in DB
        public List<Clan> GetAllClans() { return context.Clans.ToList(); }

        //gets clan with given clanTag
        public Clan GetClanById(int id) { return context.Clans.Find(id); }

        //updates clan at given clantag
        public void UpdateClan(Clan clan)
        {
            //fetches clan at given Tag
            Clan clanToUpdate = GetClanById(clan.Id);

            //if a valid clan is fetched it updates the fields to those of the argumented clan
            if(clanToUpdate != null)
            {
                clanToUpdate.Name = clan.Name;
                clanToUpdate.Type = clan.Type;
                clanToUpdate.Description = clan.Description;
                clanToUpdate.BadgeId = clan.BadgeId;
                clanToUpdate.LocationCode = clan.LocationCode;
                clanToUpdate.RequiredTrophies = clan.RequiredTrophies;
                clanToUpdate.DonationsPerWeek = clan.DonationsPerWeek;
                clanToUpdate.ClanChestStatus = clan.ClanChestStatus;
                clanToUpdate.ClanChestLevel = clan.ClanChestLevel;
                clanToUpdate.ClanScore = clan.ClanScore;
                clanToUpdate.ClanWarTrophies = clan.ClanWarTrophies;
                clanToUpdate.Members = clan.Members;
                clanToUpdate.UpdateTime = clan.UpdateTime;
                context.SaveChanges();
            }
        }
    }
}
