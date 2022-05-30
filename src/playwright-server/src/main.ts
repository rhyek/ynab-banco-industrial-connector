import ngrok from 'ngrok';
import { chromium } from 'playwright';

async function main() {
  const port = 3800;
  const playwrightServerToken = process.env.PLAYWRIGHT_SERVER_TOKEN;
  const ngrokAuthToken = process.env.NGROK_AUTH_TOKEN;
  const ngrokSubdomain = 'playwright.rhyek';

  await chromium.launchServer({
    headless: true,
    port,
    wsPath: playwrightServerToken,
  });

  while (true) {
    try {
      await ngrok.connect({
        authtoken: ngrokAuthToken,
        subdomain: ngrokSubdomain,
        addr: port,
      });
      break;
    } catch (error) {
      console.error(error);
      const waitTime = 10;
      console.log(`Waiting for ${waitTime} seconds...`);
      await new Promise((resolve) => setTimeout(resolve, waitTime * 1_000));
    }
  }

  console.log(
    'Playwright Url:',
    `wss://${ngrokSubdomain}.ngrok.io/${playwrightServerToken}`
  );
}

main();
