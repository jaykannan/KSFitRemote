# Getting a Google Maps API key (for Street View features)

Planned virtual-walking features (see [ROADMAP.md](ROADMAP.md)) would use Google's Street View. That needs your own API key. **Nothing in the app uses a key yet.** This page is so you can have one ready.

A normal Gmail account is enough. If you sign in with a work/school Google account, an admin may block project or billing creation.

> **Costs.** Google requires a **billing account with a payment card** on the project, even for the free allowance. Using the key within the free allowance has been free, but pricing changes, so read the current [Google Maps Platform pricing](https://mapsplatform.google.com/pricing/) first. Set the budget alert and quota caps in steps 6 and 7 so a mistake can't run up a bill.

Pricing note (as of the last check, verify before relying on it): since 1 March 2025 Google replaced the old $200 monthly credit with a free monthly allowance per product (SKU): 10,000 calls for "Essentials" SKUs, 5,000 for "Pro" SKUs, 1,000 for "Enterprise". Which tier Street View falls under is something to confirm on the pricing page.

## Steps

1. **Sign in** at <https://console.cloud.google.com/> with your Gmail account and accept the terms.
2. **Create a project:** project picker (top bar) → *New project* → name it e.g. `ksfit-remote` → *Create*, then select it.
3. **Enable billing:** menu → *Billing* → link or create a billing account (card required). Google Maps Platform's own onboarding at <https://mapsplatform.google.com/> can walk you through this too.
4. **Enable the APIs you need:** menu → *APIs & Services* → *Library*. Search for and *Enable*:
   - **Maps JavaScript API** (interactive Street View in a web page)
   - **Street View Static API** (still images; optional)
   - **Map Tiles API** only if a feature specifically asks for it
5. **Create the key:** *APIs & Services* → *Credentials* → *Create credentials* → *API key*. Copy it.
6. **Restrict the key** (important; click the key to edit it):
   - *Application restrictions*: for a web page use **Websites (HTTP referrers)** and list only the addresses you use, e.g. `http://localhost:*/*`. For a server or desktop tool that can't send a referrer, use **IP addresses** if you have a fixed one.
   - *API restrictions*: **Restrict key** and tick only the APIs you enabled in step 4.
7. **Cap spending:**
   - *Billing* → *Budgets & alerts* → create a small budget (e.g. $1) with email alerts at 50% and 100%. Note that budgets **alert, they do not stop** spending.
   - *APIs & Services* → *Enabled APIs* → the API → *Quotas & System Limits* → lower the daily/per-minute request quotas to a safe level. Quotas do stop requests.
8. **Test:** open this in a browser (replace the key; the first line only works if you left the key unrestricted or allowed the Street View Static API):

   ```
   https://maps.googleapis.com/maps/api/streetview?size=400x300&location=46.414382,10.013988&key=YOUR_KEY
   ```

   You should get a street-level image. An error page such as `REQUEST_DENIED` usually means billing or the API isn't enabled yet, or the restriction blocks the call.

## Keeping the key safe

- **Never commit the key to git** or paste it in an issue. This repository is public.
- Keep it in an environment variable or a local settings file that is git-ignored. When a feature needs it, it will be read from `KSFIT_GOOGLE_MAPS_KEY` or the app settings, not from source.
- If a key leaks: *Credentials* → the key → *Regenerate key* (or delete it), then update your local copy.
- Web keys are visible to anyone who loads the page, which is why the referrer and API restrictions in step 6 matter.

## Free alternatives (no card needed)

If you don't want to attach a card, there are options that don't need a Google key: walking-tour videos on YouTube played at your speed, or open street-level imagery from [Mapillary](https://www.mapillary.com/) (free account and API token, coverage varies). These are not integrated yet either.

## Sources checked

- [Google Maps Platform pricing](https://mapsplatform.google.com/pricing/)
- [Pricing categories (Google for Developers)](https://developers.google.com/maps/billing-and-pricing/pricing-categories)
- Third-party explainer of the 2026 free allowances: [woosmap.com](https://www.woosmap.com/blog/google-maps-api-key-free)

The Cloud Console menu names above are from memory and Google rearranges them occasionally; the official docs win if they differ.
