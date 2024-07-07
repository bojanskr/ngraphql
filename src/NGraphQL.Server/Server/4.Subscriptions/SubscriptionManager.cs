﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Json;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;
using NGraphQL.Subscriptions;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Subscriptions;

public class SubscriptionManager {
  IMessageSender _sender;
  GraphQLServer _server;
  ClientSubscriptionStore _subscriptionStore = new();

  public SubscriptionManager(GraphQLServer server) {
    _server = server;
  }

  // provides a way for SignalRSender singleton to register with SubscriptionManager. 
  // It's a bit complicated, SignalRListener (hub, transient object) and SignalRSender are created by DI, late, when the first subscribe message is received.
  // It starts from Listener, it has Sender as parameter(not used) that forces DI to create the MessageSender instance (singleton),
  // and sender immediately registers itself with SubscriptionManager. 
  public void SetSender(IMessageSender sender) {
    if (_sender == null)
      _sender = sender;
  }

  public void OnClientConnected(string connectionId, ClaimsPrincipal user, string userId) {
    var conn = new ClientConnection() { ConnectionId = connectionId, User = user, UserId = userId };
    _subscriptionStore.AddClient(conn);
  }

  public void OnClientDisconnected(string connectionId, Exception exc) {
    _subscriptionStore.RemoveClient(connectionId);
  }

  public async Task MessageReceived(string connectionId, string message) {
    var ctx = new SubscriptionContext() { ConnectionId = connectionId, MessageJson = message };
    try {
      Util.Check(_sender != null, "Subscription manager not initialized, message sender not set.");
      await MessageReceivedImpl(ctx);
      _server.Events.OnSubscriptionAction(ctx);
    } catch (Exception ex) {
      ctx.Exception = ex;
      _server.Events.OnSubscriptionActionError(ctx);
//      Debugger.Break();
    }
  }

  private async Task MessageReceivedImpl(SubscriptionContext context) {
    context.Client = _subscriptionStore.GetClient(context.ConnectionId);
    if (context.Client == null)
      return;
    var msg = SerializationHelper.DeserializePartial<PayloadMessage>(context.MessageJson);
    context.ClientSubscriptionId = msg.Id; 
    switch (msg.Type) {
      case SubscriptionMessageTypes.Subscribe:
        await HandleSubscribeMessage(context, msg);
        break;
      case SubscriptionMessageTypes.Complete:
        break;
      default:
        break;
    }
  }

  // Sequence: SignalRListener -> HandleSubscribeMessage -> ExecuteRequest -> Resolver -> AddSubscription(here)
  private async Task HandleSubscribeMessage(SubscriptionContext context, PayloadMessage message) {
    var payloadElem = (JsonElement)message.Payload;
    Util.Check(payloadElem.ValueKind == JsonValueKind.Object, "Subscribe.Payload is a JsonElement of invalid type {0}.", message.Type);
    var pload = payloadElem.Deserialize<SubscribePayload>(JsonDefaults.JsonOptions);
    // parse the query
    var rawReq = new GraphQLRequest() { OperationName = pload.OperationName, Query = pload.Query, Variables = pload.Variables };
    var requestContext = new RequestContext(this._server, rawReq, CancellationToken.None);
    requestContext.Subscription = context;
    await _server.ExecuteRequestAsync(requestContext); //in the call, the resolver adds subscription using AddSubscription method below
  }

  // To be called by Subscription Resolver method
  public ClientSubscriptionInfo SubscribeCaller(IFieldContext field, string topic) {
    var reqCtx = (RequestContext)field.RequestContext;
    var subCtx = reqCtx.Subscription;
    var clientSub = _subscriptionStore.AddSubscription(subCtx.Client, subCtx.ClientSubscriptionId, topic,
        reqCtx.ParsedRequest);
    return clientSub;
  }

  public void UnsubscribeCaller(string topic, string connectionId) {
    _subscriptionStore.RemoveSubscription(topic, connectionId);
  }

  public async Task Publish(string topic, object data) {
    if (!_subscriptionStore.HasSubscribers(topic)) //quick check if there are any
      return; 
    // Execute it on background thread, to avoid blocking caller for too long
    var task = Task.Run(async () => await PublishImpl(topic, data));
    await Task.CompletedTask;
  }

  private async Task PublishImpl(string topic, object data) {
    var ctx = new PublishContext() { Topic = topic, Data = data };
    var subs = _subscriptionStore.GetTopicSubscriptions(topic);
    // Group by sub variant
    var varGroups = subs.GroupBy(sub => sub.Variant);
    // Each group contains Subscriptions with the same Variant (topic and query), so all clients will get identical data 
    foreach (var grp in varGroups) {
      var subscrVariant = grp.Key;
      var payloadJson = await GetPublishMessagePayloadJson(subscrVariant, data);
      foreach(var clientSub in grp) {
        var msgJson = FormatMessageToPublish(clientSub.Id, payloadJson);
        await SendMessage(ctx, clientSub.Client, msgJson);
      }
    }
  }

  private async Task SendMessage(PublishContext ctx, ClientConnection client, string msgJson) {
    try {
      ctx.Client = client;
      await _sender.SendMessage(client.ConnectionId, msgJson);
    } catch (Exception exc) {
      ctx.Exception = exc;
      ctx.ErrorCount++;
      if (ctx.ErrorCount < 3)
        _server.Events.OnSubscriptionPublishError(ctx);
      ctx.Exception = null; 
      // we swallow individual failures, no rethrow
    }
  }

  private async Task<string> GetPublishMessagePayloadJson(SubscriptionVariant sub, object data) {
    var opId = sub.Topic;
    try {
      var reqContext = new RequestContext(_server, sub.ParsedRequest, null);
      var reqHandler = new RequestHandler(_server, reqContext);
      var topOp = sub.ParsedRequest.Operations.First();
      var topScope = new OutputObjectScope(new RequestPath(), null, null) { IsSubscriptionNextTopScope = true, SubscriptionNextResolverResult = data };
      await reqHandler.ExecuteOperationAsync(topOp, topScope);
      // top scope is similar to Data node in regular query; it contains top node corresponding 
      //  to subscription method, like 'subscribeToX' with the actual selection data under it.
      // we need to retrieve this data under root node. 
      var result = topScope.FirstOrDefault().Value; // It can be ObjectScope or plain value
      var json = SerializationHelper.Serialize(result);
      return json;
    } catch (Exception ex) {
      Trace.WriteLine("Error: " + ex.ToString());
      Debugger.Break();
      return null;
    }
  }

  private string FormatMessageToPublish(string id, string payload) {
    const string startStr = @"{
  ""id"": """;
    const string middleStr = @""", ""type"": ""next"", ""payload"": ";
    const string endStr = @"
}";

    var json = startStr + id + middleStr + payload + endStr;
    return json;
  }

  /* sample next message
  {
    "id": "ThingUpdate/1/abcdef",
    "type": "next",
    "data": {
      "id": 1,
      "name": "newName",
      "kind": "KIND_ONE"
    }
  }

  */
}
