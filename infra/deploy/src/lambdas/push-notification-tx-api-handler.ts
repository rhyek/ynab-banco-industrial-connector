import * as aws from '@pulumi/aws';
import * as awsx from '@pulumi/awsx';
import { DynamoDB } from 'aws-sdk';
import { nanoid } from 'nanoid';
import { projectName, projectTags } from '../../../consts';
import { pushNotificationTxsTableName } from '../dynamodb';
import { parseBody } from '../utils/parse-body';

export const pushNotificationTxApiHandler = new aws.lambda.CallbackFunction(
  `${projectName}-push-notif-tx-api-handler`,
  {
    environment: {
      variables: {
        TABLE_NAME: pushNotificationTxsTableName,
      },
    },

    callback: async (ev: awsx.apigateway.Request) => {
      const body = parseBody<{ text: string }>(ev);
      console.log('received tx:', body.text);
      console.log('table name:', process.env.TABLE_NAME);
      const client = new aws.sdk.DynamoDB({});
      const response = await new Promise<DynamoDB.PutItemOutput>(
        (resolve, reject) => {
          client.putItem(
            {
              TableName: process.env.TABLE_NAME!,
              Item: {
                id: {
                  S: nanoid(),
                },
                text: {
                  S: body.text,
                },
                timestamp: {
                  S: new Date().toISOString(),
                },
              },
            },
            (error, data) => {
              if (error) {
                reject(error);
              } else {
                resolve(data);
              }
            }
          );
        }
      );
      console.log('dynamodb response:', JSON.stringify(response, null, 2));
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
