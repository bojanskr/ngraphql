﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  public class StarWarsResolvers : IResolverClass {
    StarWarsApp _app;

    // Begin/end request method
    public void BeginRequest(IRequestContext request) {
      // Get app instance
      var swApi = (StarWarsApi)request.Server.Api;
      _app = swApi.App;
    }

    public void EndRequest(IRequestContext request) {
    }

    // Queries
    public IList<ICharacter_> GetCharacters(IFieldContext fieldContext, Episode episode) { 
      return default; 
    }

    public IList<Starship> GetStarships(IFieldContext fieldContext) { 
      return default; 
    }

    public IList<Review> GetReviews(IFieldContext fieldContext, Episode episode) { 
      return default; 
    }

    public ICharacter_ GetCharacter(IFieldContext fieldContext, string id) { 
      return default; 
    }

    public Starship_ GetStarship(IFieldContext fieldContext, string id) { 
      return default;
    }

    public IList<SearchResult_> Search(IFieldContext fieldContext, string text) { 
      return default; 
    }

    // Mutations
    public Review_ CreateReview(IFieldContext fieldContext, Episode episode, ReviewInput_ reviewInput) {
      return default; 
    }

    // Fields 

    // this is a regular version, not used - we use batched version instead
    public IList<Character> GetFriends(IFieldContext fieldContext, Character character) {
      var friends = _app.Characters.Where(c => character.FriendIds.Contains(c.Id)).ToList();
      return friends; 
    }

    public IList<Character> GetFriendsBatched(IFieldContext fieldContext, Character character) {
      // batch execution (aka DataLoader); we retrieve all pending parents (characters)
      //  get all their friend lists as a dictionary, and then post it into context - 
      //  the engine will use this dictionary to lookup values and will not call resolver anymore
      var allParents = fieldContext.GetAllParentEntities<Character>();
      var friendsByCharacter =  _app.GetFriendLists(allParents);
      fieldContext.SetBatchedResults<Character, IList<Character>>(friendsByCharacter); 
      return null; // the engine will use batch results dict to lookup the value
    }
    
  }
}