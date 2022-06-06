import * as aws from '@pulumi/aws';
import * as awsx from '@pulumi/awsx';
import { SQSClient, SendMessageCommand } from '@aws-sdk/client-sqs';
import { nanoid } from 'nanoid';
import { projectName, projectTags } from '../../../consts';
import { scrapeBankTransactionsSqsUrl } from '../sqs-scrape-bank-transactions';

export const scrapeConfirmedBankTxsApiHandler = new aws.lambda.CallbackFunction(
  `${projectName}-scrape-confirmed-bank-txs-api-handler`,
  {
    environment: {
      variables: {
        SQS_URL: scrapeBankTransactionsSqsUrl,
      },
    },
    callback: async (): Promise<awsx.apigateway.Response> => {
      const response = await new SQSClient({}).send(
        new SendMessageCommand({
          QueueUrl: process.env.SQS_URL!,
          MessageBody: 'CONFIRMED',
          // fifo queue properties
          MessageDeduplicationId: nanoid(),
          MessageGroupId: 'default',
        })
      );
      return {
        statusCode: 200,
        headers: {
          'content-type': 'application/json',
        },
        body: JSON.stringify({
          sqsResponse: response,
        }),
      };
    },
    tags: {
      ...projectTags,
    },
  }
);
