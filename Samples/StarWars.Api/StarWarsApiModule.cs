﻿using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {
  public class StarWarsApiModule: GraphQLModule {
    public StarWarsApiModule() {
      // Register types
      RegisterTypes(
        typeof(IStarWarsQuery), typeof(IStarWarsMutation),
        typeof(Episode), typeof(LengthUnit),
        typeof(ICharacter_), typeof(Human_), typeof(Droid_), 
        typeof(Starship_), typeof(Review_), typeof(SearchResult_), typeof(ReviewInput_), 
        typeof(StarWarsResolvers)
        );
      // map Api object types to app types
      Map<Human, Human_>(h => new Human_() { 
        // Id, Name, AppearsIn, HomePlanet, Mass are mapped automatically
      });

    } //constructor  
    
  }
}