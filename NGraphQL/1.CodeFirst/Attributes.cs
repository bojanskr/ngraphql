﻿using System;
using System.Net.NetworkInformation;
using NGraphQL.Model;

namespace NGraphQL.CodeFirst {

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
                   AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method |
                   AttributeTargets.Parameter)]
  public class GraphQLNameAttribute : Attribute {
    public string Name;
    public GraphQLNameAttribute(string name) {
      Name = name;
    }
  }

  /// <summary>Marks the value as nullable. The mapped GraphQL type will not have a not-null marker (!). </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property 
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class NullAttribute: Attribute { }

  /// <summary>Marks a list as possibly containing null values. The mapped GraphQL type will not have a non-null
  /// marker for the element type.  </summary>
  /// <remarks>
  ///   Example: 
  ///     int[] x; -&gt; [Int!]!
  ///     [WithNulls] int[] x; -&gt; [int]!
  /// </remarks>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class WithNullsAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property
                | AttributeTargets.Method | AttributeTargets.Parameter)]
  public class ScalarAttribute : Attribute {
    public readonly string ScalarName; 
    public ScalarAttribute(string scalarName) {
      ScalarName = scalarName;
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class GraphQLIgnoreAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
                  AttributeTargets.Field | AttributeTargets.Property)]
  public class HiddenAttribute : Attribute { }

  public abstract class ResolverTargetBaseAttribute : Attribute {
    public string FieldName;
    public abstract OperationType OperationType { get; }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class QueryAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Query; 
    public QueryAttribute() { }
    public QueryAttribute(string name) { FieldName = name; }
  }

  public abstract class GraphQLTypeBaseAttribute : Attribute {
    public string Name;
    private TypeKind _kind;
    public GraphQLTypeBaseAttribute(string name, TypeKind kind) {
      Name = name;
      _kind = kind; 
    }
    public TypeKind GetKind() => _kind;
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLObjectTypeAttribute : GraphQLTypeBaseAttribute {
    public GraphQLObjectTypeAttribute(string name = null) : base(name, TypeKind.Object) { }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLInputAttribute : GraphQLTypeBaseAttribute {
    public GraphQLInputAttribute(string name = null) : base(name, TypeKind.InputObject) { }
  }

  [AttributeUsage(AttributeTargets.Interface)]
  public class GraphQLInterfaceAttribute : GraphQLTypeBaseAttribute {
    public GraphQLInterfaceAttribute(string name = null) : base(name, TypeKind.Interface) { }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class GraphQLUnionAttribute : GraphQLTypeBaseAttribute {
    public GraphQLUnionAttribute(string name = null) : base(name, TypeKind.Union) { }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class MutationAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Mutation;
    public MutationAttribute() { }
    public MutationAttribute(string name) { FieldName = name; }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class SubscriptionAttribute : ResolverTargetBaseAttribute {
    public override OperationType OperationType => OperationType.Subscription;
    public SubscriptionAttribute() { }
    public SubscriptionAttribute(string name) { FieldName = name; }
  }

  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
  public class ResolverAttribute : Attribute {
    public Type ResolverClass;
    public string MethodName;
    public ResolverAttribute(string methodName, Type resolverClass = null) {
      MethodName = methodName;
      ResolverClass = resolverClass; 
    }
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
  public class ResolverClassAttribute : Attribute {
    public readonly Type Type;
    public ResolverClassAttribute(Type resolverClass) {
      Type = resolverClass; 
    }
  }

  /*
  [AttributeUsage(AttributeTargets.Method)]
  public class FieldAttribute : ResolverTargetBaseAttribute {
    public Type OnType;
    public override OperationType OperationType => OperationType.Query;
    public FieldAttribute() { }
    public FieldAttribute(string name) { FieldName = name; }
  }
  */


}
