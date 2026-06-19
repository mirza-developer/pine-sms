# Performance Optimization Plan — `PineAI.OrderTrack`

> **Status:** Documentation only. Nothing in this file is applied to the codebase yet.
> Implementation will happen in follow-up changes, in the order listed under
> [Suggested Execution Order](#suggested-execution-order).

## Targets

| Metric                  | Goal                                  | Measurement                                          |
| ----------------------- | ------------------------------------- | ---------------------------------------------------- |
| **LCP** (mobile, 4G)    | **< 3 s** on cold cache               | Lighthouse mobile, "Slow 4G" throttling              |
| **First-load size**     | **< 3 MB** total transferred (Brotli) | Sum of `wwwroot/_framework/*.br` + `index.html` + critical assets after `dotnet publish -c Release` |
| **CLS**                 | < 0.1                                 | Lighthouse mobile                                    |

## Hard Constraints

- **The app must remain a pure Blazor WebAssembly static-hosted SPA.** It is served
  to a large user base, and any per-request server work would consume capacity at
  scale.
- **Do NOT** enable any of the following — they are out of scope for this plan:
  - Blazor WebAssembly **prerendering** (`InteractiveWebAssembly` rendered from
	a host) — adds a per-user request to the server on first load.
  - **Blazor Server** / `InteractiveServer` rendering — keeps a SignalR circuit
	open per user.
  - Any **server-side rendering** that produces per-request HTML for this app.
- **CDN / edge caching is allowed** — it offloads work from the origin instead
  of adding to it.

## Baseline Findings

Observations gathered from the current codebase (do not re-edit during this review):

- `PineAI.OrderTrack/PineAI.OrderTrack.csproj` — already has
  `PublishTrimmed=true`, `TrimMode=full`, `BlazorEnableCompression=true`,
  `CompressionEnabled=true`. **Missing** `InvariantGlobalization`.
- `PineAI.OrderTrack/Properties/PublishProfiles/IIS.pubxml` — has
  `RunAOTCompilation=true` and `SelfContained=true`. AOT typically inflates the
  download 2–4× for a tiny SPA like this.
- `PineAI.OrderTrack/web.config` — applies a **global**
  `Cache-Control: no-store, no-cache, must-revalidate` header to **every**
  response, defeating browser caching of fingerprinted `_framework/*` assets.
  No MIME maps for `.br` / `.gz`, no rewrite rule to serve precompressed assets.
- `PineAI.OrderTrack/wwwroot/index.html` —
  - Contains an empty `<link rel="preload" id="webassembly" />` (no `href`,
	so it is a no-op).
  - No `<link rel="preconnect">` for `https://ananas-collectionn.com` (the logo
	origin).
  - No `<link rel="preload" as="font">` for `Vazirmatn.woff2`.
- `PineAI.OrderTrack/Layout/SiteHeader.razor` — header logo currently has
  `loading="lazy"`. That image is the **LCP candidate** above the fold; lazy-
  loading it actively delays LCP.
- `PineAI.OrderTrack/Layout/SiteFooter.razor` — footer logo + social icons
  use `loading="lazy"`. Correct as-is (below the fold).
- `PineAI.OrderTrack/wwwroot/lib/bootstrap/**` — present in `wwwroot` but **not
  referenced** by `index.html` or `app.css`. Still ships with publish output
  unless excluded.
- `PineAI.OrderTrack/wwwroot/css/app.css` — 1223 lines, minified to
  `app.min.css` via `bundleconfig.json` ✅.
- `PineAI.OrderTrack/Pages/Home.razor.cs` — only uses `ToEnglishDigits()` from
  `PineAI.Shared`. The `PineAI.Shared` reference may otherwise be unnecessary
  and pulls in additional graph (e.g. `Tools/XlsxBuilder.cs` with OpenXml).
- No `<InvariantGlobalization>` set, so ICU data (`icudt*.dat`) ships with the
  app — typically **300–500 KB** Brotli.

## Quick-Win Bundle

Doing **A1, A2, A3, A4, B8, B9, B10** alone is usually enough to meet both
targets on a typical Blazor WASM SPA of this size. Each item is independently
shippable; ship them in the order below to verify the savings step by step.

---

## Section A — Reduce First-Load Size

### A1. Enable invariant globalization

**Why:** Ships ICU-less runtime; saves ~300–500 KB Brotli.

**Where:** `PineAI.OrderTrack/PineAI.OrderTrack.csproj`, in the existing
`<PropertyGroup>`.

```xml
<InvariantGlobalization>true</InvariantGlobalization>
<BlazorWebAssemblyLoadAllGlobalizationData>false</BlazorWebAssemblyLoadAllGlobalizationData>
```

**Risk:** Any culture-sensitive parsing/formatting breaks. Current code is
invariant-safe (`Uri.EscapeDataString`, `Trim`, JSON via the source-generated
`OrderTrackResultContext`). Re-verify at implementation time — search for
`DateTime.Parse`, `decimal.Parse`, `ToString("…")` with no `CultureInfo`.

### A2. Disable AOT for production publish

**Why:** AOT-compiled WASM is 2–4× larger than IL-trimmed WASM. The app is a
single tracking form; CPU is not the bottleneck — download size is.

**Where:** `PineAI.OrderTrack/Properties/PublishProfiles/IIS.pubxml`.

```xml
<RunAOTCompilation>false</RunAOTCompilation>
```

**Alternative** (if runtime CPU matters more than download, e.g. heavy client
work is added later): keep AOT but strip IL afterwards.

```xml
<RunAOTCompilation>true</RunAOTCompilation>
<WasmStripILAfterAOT>true</WasmStripILAfterAOT>
```

### A3. Serve precompressed `.br` / `.gz` from IIS

**Why:** Without this, IIS serves uncompressed `.wasm` / `.dll` — the **#1
cause** of >3 MB first loads. Brotli files are emitted by the publish step
(`BlazorEnableCompression=true` is already on) but IIS doesn't pick them up
without rewrite rules.

**Where:** `PineAI.OrderTrack/web.config`, inside `<system.webServer>`.

```xml
<staticContent>
  <!-- existing entries kept… -->
  <remove fileExtension=".br" />
  <remove fileExtension=".gz" />
  <mimeMap fileExtension=".br" mimeType="application/octet-stream" />
  <mimeMap fileExtension=".gz" mimeType="application/octet-stream" />
</staticContent>

<rewrite>
  <rules>
	<!-- Serve Brotli when the client supports it. -->
	<rule name="Serve .br precompressed" stopProcessing="true">
	  <match url="(.*)" />
	  <conditions logicalGrouping="MatchAll">
		<add input="{HTTP_ACCEPT_ENCODING}" pattern="br" />
		<add input="{REQUEST_FILENAME}.br" matchType="IsFile" />
	  </conditions>
	  <action type="Rewrite" url="{R:1}.br" />
	  <serverVariables>
		<set name="RESPONSE_Content-Encoding" value="br" />
	  </serverVariables>
	</rule>
	<!-- Fallback: gzip. -->
	<rule name="Serve .gz precompressed" stopProcessing="true">
	  <match url="(.*)" />
	  <conditions logicalGrouping="MatchAll">
		<add input="{HTTP_ACCEPT_ENCODING}" pattern="gzip" />
		<add input="{REQUEST_FILENAME}.gz" matchType="IsFile" />
	  </conditions>
	  <action type="Rewrite" url="{R:1}.gz" />
	  <serverVariables>
		<set name="RESPONSE_Content-Encoding" value="gzip" />
	  </serverVariables>
	</rule>
	<!-- existing _framework / SPA fallback rules follow… -->
  </rules>
</rewrite>
```

**Note:** IIS requires the `RESPONSE_Content-Encoding` server variable to be
allow-listed in `applicationHost.config` (`<rewrite><allowedServerVariables>`),
or the rules must be installed at server level. Document this in the deploy
runbook.

### A4. Fix the global `Cache-Control: no-store`

**Why:** The current `web.config` sends `no-store, no-cache, must-revalidate`
on every response. Fingerprinted `_framework/*` files have content-addressed
URLs and should be cached forever; only `index.html`, `service-worker.js`, and
`appsettings.json` should bypass cache.

**Where:** `PineAI.OrderTrack/web.config`.

Replace the global `<httpProtocol><customHeaders>` block with rule-based
headers:

```xml
<!-- Long cache for fingerprinted framework + static assets. -->
<rule name="Immutable cache for _framework" stopProcessing="false">
  <match url="^_framework/(.*)" />
  <serverVariables>
	<set name="RESPONSE_Cache-Control" value="public, max-age=31536000, immutable" />
  </serverVariables>
  <action type="None" />
</rule>

<!-- No-store for shell + dynamic config. -->
<rule name="No cache for shell" stopProcessing="false">
  <match url="^(index\.html|service-worker\.js|appsettings(\..+)?\.json)$" />
  <serverVariables>
	<set name="RESPONSE_Cache-Control" value="no-store, no-cache, must-revalidate" />
  </serverVariables>
  <action type="None" />
</rule>
```

**Risk:** If asset fingerprinting is ever turned off, immutable caching will
serve stale files. The Blazor publish pipeline fingerprints `_framework/*` by
default; do not change that.

### A5. Exclude unused Bootstrap from publish output

**Why:** `wwwroot/lib/bootstrap/**` is not referenced by any HTML or CSS in
this project but still ships. Several hundred KB of dead weight.

**Where:** `PineAI.OrderTrack/PineAI.OrderTrack.csproj`.

```xml
<ItemGroup>
  <Content Remove="wwwroot\lib\bootstrap\**" />
</ItemGroup>
```

Or simply delete the `wwwroot/lib/bootstrap/` folder if nothing in the project
needs it.

### A6. Trim the `PineAI.Shared` dependency

**Why:** Only `ToEnglishDigits()` is used (`Pages/Home.razor.cs`). Pulling in
the whole shared assembly drags `Tools/XlsxBuilder.cs` (OpenXml) and any other
incidental graph into the trim closure.

**Options:**

- **(a) Preferred** — drop the `<ProjectReference Include="..\PineAI.Shared\PineAI.Shared.csproj" />`
  from `PineAI.OrderTrack.csproj` and inline `ToEnglishDigits()` into a local
  `PineAI.OrderTrack/Extensions/StringExtensions.cs`.
- **(b) Keep the reference** but verify the trimmer fully removes
  `XlsxBuilder` / OpenXml. Search for `[DynamicDependency]`,
  `[DynamicallyAccessedMembers]`, or reflection that defeats trimming. If found,
  refactor or move those types to a separate assembly.

### A7. Strip PDBs and debug symbols from publish

**Why:** Default Release publish can copy `.pdb` files into the WASM cache,
which the service worker dutifully precaches.

**Where:** `PineAI.OrderTrack/PineAI.OrderTrack.csproj` — Release-only
property group:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <CopyOutputSymbolsToPublishDirectory>false</CopyOutputSymbolsToPublishDirectory>
</PropertyGroup>
```

After this, confirm `wwwroot/service-worker.published.js`'s
`offlineAssetsInclude` regex `/\.pdb$/` matches no published assets — the
regex can stay, it just won't pull anything.

---

## Section B — Reduce LCP

### B8. Revert `loading="lazy"` on the LCP image

**Why:** The header logo in `Layout/SiteHeader.razor` is above the fold and
is the LCP element. Lazy-loading it forces the browser to wait for layout
before requesting it.

**Where:** `PineAI.OrderTrack/Layout/SiteHeader.razor`, line 7.

```html
<img src="https://ananas-collectionn.com/wp-content/uploads/2024/12/ananas-logo.png"
	 class="site-logo"
	 alt="مزون اناناس"
	 fetchpriority="high"
	 decoding="async"
	 width="160" height="48" />
```

- **Keep** `loading="lazy"` on the footer logo (`Layout/SiteFooter.razor`)
  and the Rubika / Bale social icons — they are below the fold.
- Adding explicit `width` / `height` (replace `160`/`48` with the actual
  natural pixel dimensions) also avoids CLS.

### B9. Preconnect and preload the logo origin

**Why:** The logo is served from a third-party origin (`ananas-collectionn.com`).
Without `preconnect` the TLS handshake happens after CSS parsing, pushing LCP
back by 200–600 ms on mobile. Without `preload` it isn't requested until the
image element is parsed.

**Where:** `PineAI.OrderTrack/wwwroot/index.html`, in `<head>`.

```html
<link rel="preconnect" href="https://ananas-collectionn.com" crossorigin>
<link rel="preload"
	  as="image"
	  href="https://ananas-collectionn.com/wp-content/uploads/2024/12/ananas-logo.png"
	  fetchpriority="high">
```

**Better alternative:** copy the logo into `wwwroot/img/ananas-logo.png` (or
`.webp`) and update `SiteHeader.razor` / `SiteFooter.razor` `src` attributes
to `img/ananas-logo.png`. That removes the cross-origin handshake entirely
and the file ships with the SPA's HTTP/2 connection.

### B10. Preload Vazirmatn font

**Why:** `app.css` declares `@font-face { font-family: Vazirmatn; src: url('../fonts/Vazirmatn.woff2'); }`.
The font request only fires after CSS is parsed; preloading it cuts a round-trip
off first text paint.

**Where:** `PineAI.OrderTrack/wwwroot/index.html`, in `<head>`, **before** the
stylesheet `<link>`.

```html
<link rel="preload"
	  href="fonts/Vazirmatn.woff2"
	  as="font"
	  type="font/woff2"
	  crossorigin>
```

`crossorigin` is required even for same-origin fonts — `as="font"` requests
are made anonymously.

### B11. Remove or fix the empty WASM preload

**Why:** `<link rel="preload" id="webassembly" />` has no `href` and does
nothing. It also confuses humans reading the markup.

**Where:** `PineAI.OrderTrack/wwwroot/index.html`.

- **Simplest:** delete the line. With
  `OverrideHtmlAssetPlaceholders=true` (already set in the csproj) the
  Blazor publish pipeline will inject the correct preload for `dotnet.js` and
  the wasm bootstrap bundle.
- **If you want explicit hints:** after publish, inspect the generated
  `index.html` to see the fingerprinted file names, and pin the preload
  yourself. Re-pin every time `dotnet`/the app is upgraded.

### B12. Inline critical CSS, defer the rest

**Why:** `app.min.css` is render-blocking. The header / hero only needs a
small subset of rules; inlining them lets the browser paint immediately while
the full stylesheet downloads in parallel.

**Where:** `PineAI.OrderTrack/wwwroot/index.html`.

1. Extract the rules used by `Layout/SiteHeader.razor` and the top of
   `Pages/Home.razor` (roughly: `:root` variables, `html/body`, `.spinner`,
   `.app-loading`, `.site-header`, `.header-inner`, `.site-logo`,
   `.ot-wrap`, `.ot-page-title`) from `wwwroot/css/app.css` into a `<style>`
   block in `<head>`.
2. Switch the existing stylesheet to non-blocking load:

   ```html
   <link rel="preload" href="css/app.min.css" as="style"
		 onload="this.onload=null;this.rel='stylesheet'">
   <noscript><link rel="stylesheet" href="css/app.min.css"></noscript>
   ```

**Risk:** Critical CSS drifts from `app.css`. Add a comment in both files
pointing at each other, or wire up a small build step (e.g. `critical` /
`critters`) at a later stage.

### B13. Ensure HTTP/2 is on at the IIS binding

**Why:** Blazor WASM downloads a large number of small `_framework/*` files.
HTTP/1.1 serializes them; HTTP/2 multiplexes them on a single TLS connection.

**How:** Operations task — verify the IIS site has a TLS binding and HTTP/2
is enabled at the OS level (Windows Server 2016+ enables it by default for
TLS bindings). No code change.

### B14. Replace the spinner with a real skeleton

**Why:** The current `app-loading` block in `index.html` is a generic spinner.
The browser's LCP candidate from the **initial HTML** is just text in Persian
("در حال بارگذاری..."). Painting a skeleton that mirrors the eventual UI lets
the browser pick a meaningful LCP element from the first response, before
WASM boots.

**Where:** `PineAI.OrderTrack/wwwroot/index.html`, the `<div id="app">`
content.

Sketch (mirror `Pages/Home.razor`'s headline + form, styled with the inlined
critical CSS from B12):

```html
<div id="app">
  <header class="site-header" aria-hidden="true">
	<div class="header-inner">
	  <img class="site-logo"
		   src="https://ananas-collectionn.com/wp-content/uploads/2024/12/ananas-logo.png"
		   alt="مزون اناناس"
		   width="160" height="48"
		   fetchpriority="high" decoding="async">
	</div>
  </header>
  <main class="ot-wrap" aria-hidden="true">
	<h1 class="ot-page-title">پیگیری سفارش</h1>
	<div class="nk-track-container">
	  <div class="skeleton skeleton-input"></div>
	  <div class="skeleton skeleton-button"></div>
	</div>
  </main>
</div>
```

Once Blazor renders, it replaces the contents of `#app` automatically.

---

## Section C — Optional, Longer-Term (does NOT add server load)

### C16. Front the static site with a CDN

- **Examples:** Cloudflare, Azure Front Door, Bunny.net, AWS CloudFront.
- **What it gives you:** edge Brotli (often Brotli-11 quality), far-edge
  caching of `_framework/*`, automatic HTTP/3, geographic latency reduction.
- **Why it's safe under the hard constraint:** the origin (IIS) sees fewer
  requests, not more. There is no per-user server-side rendering involved.

### C17. (Intentionally NOT recommended) Prerendering / Blazor Server

Listed here only to document the decision:

- **Blazor WASM with prerendering** would cut LCP further but requires an
  ASP.NET host that produces HTML **per request**. With many concurrent
  users, this consumes server CPU and memory at scale. **Not pursued.**
- **Blazor Server / InteractiveServer** keeps a SignalR circuit open per
  user. **Not pursued.**

---

## Suggested Execution Order

Each step is independently shippable. Measure after each one and keep going if
targets are not met yet.

1. **A1** — invariant globalization
2. **A2** — disable AOT (or strip IL after AOT)
3. **A3** — serve precompressed `.br` / `.gz`
4. **A4** — fix `Cache-Control` per asset class
5. **A5** — exclude unused Bootstrap from publish
6. **A7** — strip PDBs from publish
7. **A6** — trim `PineAI.Shared` dependency
8. **B8** — revert `loading="lazy"` on the header logo
9. **B9** — preconnect + preload the logo (or self-host it)
10. **B10** — preload Vazirmatn font
11. **B11** — remove the empty WASM preload
12. **B12** — inline critical CSS, defer the rest
13. **B14** — real skeleton inside `#app`
14. **B13** — verify HTTP/2 on the IIS binding
15. **C16** — front with a CDN (optional)

## How to Measure

### LCP / CLS / TTFB

- Lighthouse mobile (Chrome DevTools → Lighthouse → Mobile, "Slow 4G").
- Run on a **cold cache** (DevTools → Network → "Disable cache") and an
  Incognito window so service-worker state doesn't skew numbers.
- Record LCP, CLS, TBT, total transfer size before each change.

### First-load size

```powershell
dotnet publish .\PineAI.OrderTrack\PineAI.OrderTrack.csproj -c Release -o .\publish-perf
$framework = Join-Path .\publish-perf 'wwwroot\_framework'
Get-ChildItem $framework -Recurse -Filter *.br |
  Measure-Object -Property Length -Sum |
  Select-Object @{n='MB';e={[math]::Round($_.Sum/1MB, 2)}}, Count
```

Add `index.html`, `css/app.min.css(.br)`, `fonts/Vazirmatn.woff2`, and the
logo to the total — that is the "first load" the browser actually downloads.

### Verify Brotli + Cache-Control are served

```powershell
curl.exe -sI -H "Accept-Encoding: br" https://<your-host>/_framework/dotnet.js
curl.exe -sI https://<your-host>/index.html
```

Expect `Content-Encoding: br` on framework files and the per-asset
`Cache-Control` from A4 on each response class.

### Before / after table (fill in during implementation)

| Change       | First-load (MB, Brotli) | LCP (s, mobile 4G) | CLS  | Notes |
| ------------ | ----------------------- | ------------------ | ---- | ----- |
| Baseline     |                         |                    |      |       |
| After A1     |                         |                    |      |       |
| After A2     |                         |                    |      |       |
| After A3     |                         |                    |      |       |
| After A4     |                         |                    |      |       |
| After A5–A7  |                         |                    |      |       |
| After B8–B11 |                         |                    |      |       |
| After B12    |                         |                    |      |       |
| After B14    |                         |                    |      |       |
| After C16    |                         |                    |      |       |

## Out-of-Scope (explicit)

- **No Blazor Server.**
- **No Blazor WebAssembly prerendering** (no `InteractiveWebAssembly` rendered
  from a host component).
- **No per-request server processing for first paint.**
- **No HTTPS / domain / DNS changes** — operations concern, not part of this
  plan.
- **No new client-side framework or UI library** — the goal is to make the
  current SPA smaller and faster, not to rewrite it.
