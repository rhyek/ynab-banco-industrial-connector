import * as aws from '@pulumi/aws';
import { SESClient, SendEmailCommand } from '@aws-sdk/client-ses';
import { projectName, projectTags } from '../../../consts';
import { lambdaRole } from './common/lambda-role';

interface IConfirmedBankTransaction {
  date: string;
  description: string;
  reference: string;
  amount: number;
}

export const duplicateConfirmedReferencesHandler =
  new aws.lambda.CallbackFunction(
    `${projectName}-duplicate-confirmed-refs-handler`,
    {
      role: lambdaRole,
      runtime: 'nodejs14.x',
      callback: async (evt: aws.sqs.QueueEvent) => {
        console.log(`ey man. got duplicates: ${JSON.stringify(evt, null, 2)}`);
        const client = new SESClient({});
        for (const record of evt.Records) {
          const txs: IConfirmedBankTransaction[] = JSON.parse(record.body);
          const command = new SendEmailCommand({
            Source: 'carlos.rgn@gmail.com',
            Destination: {
              ToAddresses: ['carlos.rgn@gmail.com'],
            },
            Message: {
              Subject: {
                Data: 'ynab-bi-connector: found duplicate confirmed tx references',
              },
              Body: {
                Text: {
                  Data: `txs: ${JSON.stringify(txs, null, 2)}`,
                },
              },
            },
          });
          console.log(
            `sending email with config:`,
            JSON.stringify(command, null, 2)
          );
          const response = await client.send(command);
          console.log('response:', JSON.stringify(response, null, 2));
        }
      },
      tags: {
        ...projectTags,
      },
    }
  );
