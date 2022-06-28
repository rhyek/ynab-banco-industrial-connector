import * as aws from '@pulumi/aws';
import { sendScrapeTxsMessage } from './common/send-scrape-txs-message';

export const scrapeConfirmedTxsScheduleEventHandler: aws.cloudwatch.EventRuleEventHandler =
  async (_: aws.cloudwatch.EventRuleEvent) => {
    console.log('sending message sqs queue');
    const response = await sendScrapeTxsMessage('CONFIRMED');
    console.log(JSON.stringify(response, null, 2));
  };
