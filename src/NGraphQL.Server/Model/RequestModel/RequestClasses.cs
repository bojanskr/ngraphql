﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model.Request {

  public enum SelectionItemKind {
    Field,
    FragmentSpread,
  }

  public abstract class RequestObjectBase {
    public RequestObjectBase Parent; 
    public SourceLocation SourceLocation { get; internal set; } 
  }

  public abstract class NamedRequestObject: RequestObjectBase {
    public string Name { get; internal set; }
    public override string ToString() => Name; 
  }

  public abstract class SelectionItem : NamedRequestObject {
    public SelectionItemKind Kind; 
    public IList<RequestDirective> Directives { get; internal set; }
    public SelectionItem(SelectionItemKind kind) { 
      Kind = kind; 
    }
  }

  public class SelectionField : SelectionItem, ISelectionField {
    public string Alias;
    public string Key => Alias ?? Name;    //alias or name
    public IList<InputValue> Args;
    public SelectionSubset SelectionSubset;
    public IList<MappedArg> MappedArgs;

    public SelectionField() : base(SelectionItemKind.Field) { }

    public override string ToString() => $"{Key}";
  }

  public class GraphQLOperation : SelectionField {
    public OperationType OperationType;
    public ObjectTypeDef OperationTypeDef;
    public IList<VariableDef> Variables = new List<VariableDef>();
    public IList<FragmentDef> UsesFragments { get; } = new List<FragmentDef>();

    public override string ToString() => $"{OperationType}: {SelectionSubset.Items.Count} fields";
  }

  public class FragmentSpread : SelectionItem {
    public bool IsInline; 
    public FragmentDef Fragment; // might be inline fragment

    public FragmentSpread(): base(SelectionItemKind.FragmentSpread) { }
  }

  public class SelectionSubset: RequestObjectBase {
    public List<SelectionItem> Items;
    public readonly IList<MappedSelectionSubSet> MappedSubSets = new List<MappedSelectionSubSet>(); 

    public SelectionSubset(RequestObjectBase parent, List<SelectionItem> items, SourceLocation location) {
      Parent = parent; 
      Items = items;
      this.SourceLocation = location; 
    }

  }

  public class InputValue : NamedRequestObject {
    public ValueSource ValueSource;
    // indicates that argument is for field on Union; the target owner field (function) depends on specific instance/type returned as union member; 
    // ArgDef is null in this case, so it should be looked up at execution time when evaluation argument
    public bool NonStatic; 

    public InputValue() { }
    public static readonly InputValue[] EmptyList = new InputValue[] { };
  }

  public class VariableDef : NamedRequestObject {

    public InputValueDef InputDef; 
    public ValueSource ParsedDefaultValue;
    public IList<RequestDirective> DirectiveRefs;

    public VariableDef() { }

    public override string ToString() => $"{Name}/{InputDef.TypeRef}";

    public static readonly VariableDef[] EmptyList = new VariableDef[] { };
  }

  public class OnTypeRef : NamedRequestObject {
    public TypeDefBase TypeDef; //ObjectType, interface or union 
  }

  public class FragmentDef : NamedRequestObject {
    public OnTypeRef OnTypeRef; 
    public List<RequestDirective> Directives;
    public SelectionSubset SelectionSubset;
    public bool IsInline;
    public int DependencyTreeLevel = -1;// index used in ordering fragments by dependency

    // directly referenced fragments, only in top-level items/spreads
    public IList<FragmentDef> UsesFragments { get; } = new List<FragmentDef>();
    // all fragments used through dependency tree
    public IList<FragmentDef> UsesFragmentsAll = new List<FragmentDef>();

    public static readonly IList<FragmentDef> EmptyList = new FragmentDef[] { };
  }

  public class VariableValue : RequestObjectBase {
    public VariableDef Variable;
    public object Value;
    public override string ToString() => $"{Variable.Name}:{Value}";
  }

  public class RequestDirective : NamedRequestObject {
    public static IList<RequestDirective> EmptyList = new RequestDirective[] { };

    public DirectiveDef Def;
    public DirectiveLocation Location;
    public IList<InputValue> Args;
    public IList<MappedArg> MappedArgs;

    public RequestDirective() { }
    public override string ToString() => Def.Name;
    public object[] StaticArgValues;   // dirs that do not use variables
  }


  public class ParsedGraphQLRequest {
    public List<GraphQLOperation> Operations = new List<GraphQLOperation>();
    public List<FragmentDef> Fragments = new List<FragmentDef>();
    // all directives from all elements
    public List<RuntimeDirective> AllDirectives = new List<RuntimeDirective>(); 
  }

} //ns
