#!/usr/bin/env ts-node-transpile-only
import { config } from 'dotenv';
import { command } from 'execa';
import { backendEnvironmentVariableKeys } from './consts/backend-environment-variable-keys';

config();

(async () => {
  for (const key of backendEnvironmentVariableKeys) {
    await command(
      `pulumi --cwd infra/deploy config set --secret ${key} ${process.env[key]}`,
      {
        stdio: 'inherit',
      }
    );
  }
})();
