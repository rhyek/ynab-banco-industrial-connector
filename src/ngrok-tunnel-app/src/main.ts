import ngrok from 'ngrok';
import findUp from 'find-up';
import { config as dotenv } from 'dotenv';

(async function () {
  const dotEnvFilePath = await findUp('.env');
  if (!dotEnvFilePath) {
    throw new Error('Could not find .env file');
  }
  dotenv({
    path: dotEnvFilePath,
    debug: true,
  });
  // ngrok http --region=us --hostname=ynab-controller.rhyek.ngrok.io 80
  while (true) {
    try {
      const url = await ngrok.connect({
        authtoken: process.env.NGROK_AUTH_TOKEN,
        addr: 3700,
        subdomain: 'ynab-controller.rhyek',
      });
      console.log(`ngrok tunnel available at ${url}`);
      break;
    } catch (error) {
      console.error(error);
      await new Promise((resolve) => setTimeout(resolve, 2_000));
    }
  }
})();
