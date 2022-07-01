import * as aws from '@pulumi/aws';
import { projectName, projectTags } from '../../../../consts';

const lambdaRole = new aws.iam.Role(`${projectName}-lambda-role`, {
  assumeRolePolicy: aws.iam.assumeRolePolicyForPrincipal({
    Service: 'lambda.amazonaws.com',
  }),
  tags: {
    ...projectTags,
  },
});

const lambdaRolePolicy = new aws.iam.Policy(
  `${projectName}-lambda-role-policy`,
  {
    policy: {
      Version: '2012-10-17',
      Statement: [
        {
          Effect: 'Allow',
          Action: [
            'logs:CreateLogGroup',
            'logs:CreateLogStream',
            'logs:PutLogEvents',

            'sqs:SendMessage',
            'sqs:ReceiveMessage',
            'sqs:DeleteMessage',
            'sqs:GetQueueAttributes',

            'dynamodb:GetRecords',
            'dynamodb:GetShardIterator',
            'dynamodb:DescribeStream',
            'dynamodb:ListStreams',

            's3:PutObject',

            'ses:SendEmail',
          ],
          Resource: '*',
        },
      ],
    },
    tags: {
      ...projectTags,
    },
  }
);

new aws.iam.RolePolicyAttachment(`${projectName}-role-policy-attachment`, {
  role: lambdaRole.name,
  policyArn: lambdaRolePolicy.arn,
});

// insteresting snippet from https://www.pulumi.com/blog/aws-lambda-functions-powered-by-graviton2/:
// const fullAccess = new aws.iam.RolePolicyAttachment("mylambda-access", {
//   role: role,
//   policyArn: aws.iam.ManagedPolicy.LambdaFullAccess,
// });

export { lambdaRole };
