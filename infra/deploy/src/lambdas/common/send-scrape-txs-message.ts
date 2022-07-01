import { SQSClient, SendMessageCommand } from '@aws-sdk/client-sqs';
import { nanoid } from 'nanoid';
import { scrapeBankTxsQueue } from '../../sqs/sqs-scrape-bank-transactions';

export async function sendScrapeTxsMessage(type: 'RESERVED' | 'CONFIRMED') {
  // wtf - https://www.pulumi.com/blog/lambdas-as-lambdas-the-magic-of-simple-serverless-functions/
  const sqsUrl = scrapeBankTxsQueue.url.get();
  const response = await new SQSClient({}).send(
    new SendMessageCommand({
      QueueUrl: sqsUrl,
      MessageBody: type,
      // fifo queue properties
      MessageDeduplicationId: nanoid(),
      MessageGroupId: 'default',
    })
  );
  return response;
}
