var error: any = null; // for subsequent action
// var lastStatus: any = null;

async function main() {
  while (true) {
    try {
      // 10 second timeout:
      // const controller = new AbortController();
      // const timeoutId = setTimeout(() => controller.abort(), 5_000);

      const origin = 'https://txm44kpo64.execute-api.us-east-1.amazonaws.com';
      const path = 'stage/new-push-notification-tx';
      const url = `${origin}/${path}`;
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'content-type': 'application/json',
        },
        body: JSON.stringify({
          title: antitle,
          text: antext,
        }),
        // signal: controller.signal,
      });
      // clearTimeout(timeoutId);
      // lastStatus = response.status.toString();
      if (!response.ok) {
        const text = await response.text();
        const message = {
          status: response.status,
          text,
        };
        throw new Error(JSON.stringify(message));
      }
      exit(); // exit because of success
    } catch (e: any) {
      error = e?.message;
      flash(error);
      // delay 1s
      await new Promise((resolve) => setTimeout(resolve, 1_000));
    }
  }
}
main();
