import * as pulumi from '@pulumi/pulumi';
import * as aws from '@pulumi/aws';
import { backendEnvironmentVariableKeys } from '../../../../../.scripts/consts/backend-environment-variable-keys';
import {
  scrapeBankTransactionsConsumerNamespace,
  projectTags,
} from '../../../../consts';
import { buildStack } from '../../build-stack';
import { playwrightTracesBucketName } from '../../playwright-traces-s3-bucket';
import { lambdaRole } from '../common/lambda-role';

const config = new pulumi.Config();

const scrapeBankTransactionsConsumerImage = buildStack.getOutput(
  'scrapeBankTransactionsConsumerImage'
);

export const scrapeBankTransactionsConsumerFunc = new aws.lambda.Function(
  `${scrapeBankTransactionsConsumerNamespace}-lambda`,
  {
    packageType: 'Image',
    imageUri: scrapeBankTransactionsConsumerImage,
    role: lambdaRole.arn,
    timeout: 3 * 60,
    tags: {
      ...projectTags,
    },
    environment: {
      variables: {
        ...Object.fromEntries(
          backendEnvironmentVariableKeys.map((key) => [
            key,
            config.requireSecret(key),
          ])
        ),
        PLAYWRIGHT_TRACES_S3_BUCKET_NAME: playwrightTracesBucketName,
        DEBUG: 'pw:*',
      },
    },
  }
);
