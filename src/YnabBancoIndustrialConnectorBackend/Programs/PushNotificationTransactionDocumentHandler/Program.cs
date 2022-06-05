// https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/

using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;

var handler = (DynamoDBEvent evt, ILambdaContext context) => {
  foreach (var record in evt.Records) {
    if (record.EventName == OperationType.INSERT) {
      context.Logger.Log("notification text:");
      context.Logger.Log(record.Dynamodb.NewImage["text"].S);
    }
  }
};

await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
  .Build().RunAsync();
