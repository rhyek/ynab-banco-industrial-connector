import * as pulumi from '@pulumi/pulumi';
import * as aws from '@pulumi/aws';
import { backendEnvironmentVariableKeys } from '../../../../.scripts/consts/backend-environment-variable-keys';
import {
  projectTags,
  pushNotificationTxDocumentHandlerNamespace,
} from '../../../consts';
import { buildStack } from '../build-stack';
import { lambdaRole } from './common/lambda-role';

const config = new pulumi.Config();

const pushNotificationTxDocumentHandlerImage = buildStack.getOutput(
  'pushNotificationTxDocumentHandlerImage'
);

export const pushNotificationTxDocumentHandlerFunc = new aws.lambda.Function(
  `${pushNotificationTxDocumentHandlerNamespace}-lambda`,
  {
    packageType: 'Image',
    imageUri: pushNotificationTxDocumentHandlerImage,
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
      },
    },
  }
);
