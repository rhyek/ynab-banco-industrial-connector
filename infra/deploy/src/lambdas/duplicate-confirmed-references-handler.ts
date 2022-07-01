import * as aws from '@pulumi/aws';
import { SESClient, SendEmailCommand } from '@aws-sdk/client-ses';
import { projectName, projectTags } from '../../../consts';

export const duplicateConfirmedReferencesHandler =
  new aws.lambda.CallbackFunction(
    `${projectName}-duplicate-confirmed-refs-handler`,
    {
      callback: async (evt: aws.sqs.QueueEvent) => {
        const client = new SESClient({});
        for (const record of evt.Records) {
          const references: string[] = JSON.parse(record.body);
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
                  Data: `references: ${references.join(', ')}`,
                },
              },
            },
          });
          await client.send(command);
        }
      },
      tags: {
        ...projectTags,
      },
    }
  );
