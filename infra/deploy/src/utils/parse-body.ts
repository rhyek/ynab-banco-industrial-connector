import * as awsx from '@pulumi/awsx';

export function parseBody<T>(event: awsx.apigateway.Request): T {
  if (!event.body) {
    throw new Error('Body is empty');
  }
  const body = JSON.parse(
    event.isBase64Encoded
      ? Buffer.from(event.body, 'base64').toString('utf8')
      : event.body
  ) as T;
  return body;
}
