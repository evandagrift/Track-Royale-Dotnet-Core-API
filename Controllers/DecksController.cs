﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RoyaleTrackerAPI.Models;
using RoyaleTrackerAPI.Repos;
using RoyaleTrackerClasses;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RoyaleTrackerAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DecksController : ControllerBase
    {
        //Authentication Manager for handling Bearer Token
        private readonly CustomAuthenticationManager customAuthenticationManager;

        //context to DB and Repo for handling
        private TRContext context;
        private DecksRepo repo;

        //loading in injected dependancies
        public DecksController(CustomAuthenticationManager m, TRContext c)
        {
            customAuthenticationManager = m;
            // commented out while testing 
            context = c;

            //init the repo with DB context
            repo = new DecksRepo(context);
        }

        [Authorize(Policy = "AdminOnly")]
        // GET: api/Decks
        [HttpGet]
        public string Get()
        {
            //fetches all decks in the DB
            List<Deck> decks = repo.GetAllDecks();

            //returns the list to the end user
            return JsonConvert.SerializeObject(decks, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

        }

        [Authorize(Policy = "AdminOnly")]
        // GET api/Decks/1
        [HttpGet("{deckID}", Name = "GetDeck")]
        public string Get(int deckID)
        {
            //gets deck by Id
            Deck deck = repo.GetDeckByID(deckID);

            //returns fetched deck
            return JsonConvert.SerializeObject(deck, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }


        [Authorize(Policy = "AdminOnly")]
        [HttpPost("getdeckwithid")]
        // POST api/Decks/getdeckwithid
        public Deck GetDeckWithId([FromBody] Deck deck)
        {
            //gets deck with the given deck id
            Deck returnDeck = repo.GetDeckWithId(deck);

            //returns the deck to the end user
            return returnDeck;
        }

        [Authorize(Policy = "AdminOnly")]
        // DELETE: api/Decks/{deckTag}
        [HttpDelete("{deckID}")]
        public void Delete(int deckID)
        {
            //deletes deck at given Id
            repo.DeleteDeck(deckID);
        }

        [Authorize(Policy = "AdminOnly")]
        // Update: api/Decks
        [HttpPut]
        public void Update([FromBody] Deck deck)
        {
            //updates the deck with given Id
            repo.UpdateDeck(deck);
        }

    }
}
