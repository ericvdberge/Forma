# Prompt: implement drafts and publishing

Paste everything below the line into a new Claude Code session started in your local Forma clone, on your own computer. It is written to stand on its own.

---

Implement the design in `docs/design/drafts-and-publish.md` for real, API and app, then run it in Docker on this computer so I can try it in the browser. The design is settled; build it as written. Where something doesn't fit the code, pick the option closest to the design, note it in the PR description, and carry on.

## 0. Before you start

- Run `git status`. If there are uncommitted changes, stop and ask me what to do with them. Don't stash, reset or discard anything on your own.
- `git fetch origin` and create a new branch `feature/drafts-and-publish` from `origin/claude/great-shannon-n3znbv`. That branch holds PR #20 (collections, still open) plus the design docs and `src/api/README.md`. If PR #20 has been merged by now, branch from `origin/main` instead and check that `docs/design/drafts-and-publish.md` is there.
- Read these first, in this order:
  1. `CLAUDE.md`: how code is structured. Core services and repositories are CRUD only. Rules go in supporting services. Reads go through `{Entity}Filter` (IDOR). The analyzers FORMA001–003 fail the build. Code style rules.
  2. `docs/design/drafts-and-publish.md`: **the spec**. § 4 is the UX, § 5 the database, § 6 publishing, § 7 the API, § 9 the build order.
  3. `docs/design/collections.md`, `docs/design/page-content.md`, `docs/design/page-content-api.md`: what exists today.
  4. `src/api/README.md` (data model diagram) and `src/app/README.md` (routes, generated client).

## 1. What to build

Follow § 9 of the design, in its order, committing after each step with the build and tests green.

**API**
1. **Migration** (`dotnet ef migrations add AddDrafts`). It adds:
   - the `Draft…` columns and `PublishedAt` from § 5;
   - `CK_Pages_Kind`, and the composite foreign key `Pages (WebsiteId, CollectionId)` → `Collections (WebsiteId, Id)` with the alternate key it needs;
   - the tables `PageBindingUsages`, `ScheduledPublishes` and `ScheduledPublishEntries`. The entries table gets a check constraint that exactly one of its three foreign keys is set.

   Existing rows become live with no draft: set `PublishedAt` from the current status and `UpdatedAt`. `PageContents.Status` and `CollectionItems.Status` are replaced by `PublishedAt`. If removing them touches too much, keep them and keep them in sync, and say so in the PR. Leave `Pages.Status` alone.
2. **Drafts**:
   - `ITransactionRunner` in Infrastructure, wrapping `db.Database.BeginTransactionAsync`;
   - the draft endpoints for pages, collections and items;
   - `GET /drafts?websiteId=`.

   `{Entity}Insert`, `{Entity}Update` and `{Entity}Filter` get the new properties, each filter property with its own `With…` method.
3. **Publishing**: `PublishCheckService`, `PublishService`, `POST /publish/check` and `POST /publish`, exactly as § 6 describes. This includes `needs`, moving item values with `DraftValueMoves`, rebuilding `PageBindingUsages`, and rolling everything back on any failure. Reuse `CollectionItemValidationService` and `PageContentValidationService`; don't duplicate their rules. Remove `PUT /pages/{id}/content`.
4. **Scheduling**:
   - `ScheduledPublishWorker`, a `BackgroundService`. Make its interval a setting, `Publishing:WorkerInterval`, default one minute, so I can set it to 10 seconds when trying it out.
   - The `/scheduled-publishes` endpoints. An entity can be in at most one pending scheduled publish.
   - A publish that fails when it runs is marked `Failed` with its issues, and nothing is published.
5. **Seeding**: the development sample data still works, and the sample *Blog posts* collection and `/blog` page are live.

**App** (`src/app`). Run `npm run generate:api -w forma-app` after the API changes. Calls go through queriers, with one composable per concern (e.g. `useDrafts`, `usePublishing`, `useScheduledPublishes`). Feature composables such as `useStudio` combine them; don't grow one composable that holds everything.
1. **In every editor** (studio, collection field editor, item form):
   - the status chip, **Save** (also `Ctrl+S`), and the **Publish** split button with *Publish now*, *Schedule…*, *Publish with other changes…* and *Discard changes*, as in § 4.1;
   - the *"Publish together?"* prompt when Publish needs other drafts.

   The studio already has `StudioToolbar.vue`, `PublishDialog.vue` and `useStudioPublishing.ts`; change those rather than adding parallel ones. The studio now saves drafts to the server, so a reload no longer loses work.
2. **Top bar** (`AppTopbar.vue`): *"N unpublished changes"*, which opens the publish dialog of § 4.2. The dialog lists every draft with a checkbox, ticks what's needed automatically (*needed by …*), shows the live check result and **Open** links, and has **Publish now** and **Schedule…**.
3. **Scheduling** (§ 4.3):
   - the date and time picker, with the time-zone name shown;
   - the **Scheduled** tab with *Change time*, *Publish now* and *Cancel*;
   - the banner in an editor whose draft is scheduled;
   - the *"A scheduled publish didn't go live"* state, with *Try again*.
4. The collection field editor shows *"Used by…"* for each field, from `PageBindingUsages`. Removing a field that has values asks *Move values to… / Remove values*.

Write the UI copy the way § 4 does: plain words, the action named on the button, toasts that say what happened.

## 2. Tests

In `Forma.Tests`, with builders and DTOs as `CLAUDE.md` asks. At least:
- a publish rolls back completely when one step fails;
- removing a field blocks the publish until the pages that use it are in it, and `needs` names them;
- moved values end up in the new field in every item;
- an id from another website is not found (IDOR), for drafts and for publish;
- a scheduled publish runs when it's due (call the worker's run method directly; don't wait for the timer);
- a scheduled publish that is blocked when it runs publishes nothing and is marked `Failed`;
- discarding a scheduled draft takes it out of the scheduled publish.

Everything must pass: `dotnet test src/api/Forma.Api.slnx` and `npm run build -w forma-app`.

## 3. Docs

- Update `docs/design/drafts-and-publish.md` § 0 or its status line to say what's built and where it differs.
- Update `docs/design/page-content.md` goal 2, the publish part of `docs/design/page-content-api.md`, and the root `README.md` endpoint table.
- Update the data model diagram in `src/api/README.md` to the new schema. § 5 of the design is the starting point; check it against the model snapshot.

## 4. Run it in Docker on this computer

1. `docker compose up --build forma`, with `Publishing__WorkerInterval=00:00:10` set for the container so scheduled publishes run within seconds. Add it to `docker-compose.yml` under `environment:` only if I agree; otherwise pass it on the command line.
2. The existing `forma-data` volume keeps the old database, and the migration runs on start. If the migration fails on old data, fix the migration rather than deleting the volume. Ask me before running `docker compose down -v`.
3. Check http://localhost:8080 yourself first (Playwright works). Then tell me it's ready, with a short list of things to try:
   - edit a page, **Save**, reload: the draft is still there and the live page is unchanged;
   - in *Blog posts*, remove *Cover image*, add *Hero image* and move the values; **Publish**. Confirm it asks to publish together with the pages that use the field;
   - open the publish dialog from the top bar, untick a needed page, and see why it blocks;
   - schedule a publish one minute ahead, and watch it go live;
   - schedule one, then break it (e.g. bind a page to a field you then remove and publish), and see the failed state with its reason.

## 5. Finish

Commit on `feature/drafts-and-publish` and push it. **Ask me before opening a PR.** The base is `main` if PR #20 is merged, otherwise PR #20's branch `claude/amazing-lovelace-2qi0wh`. The PR description lists what was built, where it differs from the design and why, and the test results.
