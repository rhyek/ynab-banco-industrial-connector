import * as aws from '@pulumi/aws';
import * as awsx from '@pulumi/awsx';
import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient, PutCommand } from '@aws-sdk/lib-dynamodb';
import { nanoid } from 'nanoid';
import { projectName, projectTags } from '../../../consts';
import { pushNotificationTxsTableName } from '../dynamodb';
import { parseBody } from '../utils/parse-body';

function makeDocClient() {
  const ddbClient = new DynamoDBClient({});
  const ddbDocClient = DynamoDBDocumentClient.from(ddbClient);
  return {
    client: ddbDocClient,
    destroy: () => {
      ddbClient.destroy();
      ddbDocClient.destroy();
    },
  };
}

export const pushNotificationTxApiHandler = new aws.lambda.CallbackFunction(
  `${projectName}-push-notif-tx-api-handler`,
  {
    environment: {
      variables: {
        TABLE_NAME: pushNotificationTxsTableName,
      },
    },
    callback: async (
      evt: awsx.apigateway.Request
    ): Promise<awsx.apigateway.Response> => {
      const body = parseBody<{ text: string }>(evt);
      console.log('received tx:', body.text);
      console.log('table name:', process.env.TABLE_NAME);
      const { client, destroy } = makeDocClient();
      const response = await client.send(
        new PutCommand({
          TableName: process.env.TABLE_NAME!,
          Item: {
            id: nanoid(),
            text: body.text,
            timestamp: new Date().toISOString(),
          },
        })
      );
      console.log('dynamodb response:', JSON.stringify(response, null, 2));
      destroy();
      return {
        statusCode: 200,
        body: 'Hello, API Gateway!',
      };
    },
    tags: {
      ...projectTags,
    },
  }
);
