# Forma: working agreements

How code in this repository is structured. Follow these when adding or changing features; the README describes what exists.

## API (`src/api`)

Flow: Endpoint → (supporting service) → core service → repository → DAO.

### Core services are CRUD only

- One core service per entity (`PageCoreService`, `PageContentCoreService`, …). It only **gets, creates, updates and deletes** that entity, so it can be reused by every feature.
- Shape:
  - `Get{Entities}Async({Entity}Filter filter)` and/or `Get{Entity}Async({Entity}Filter filter)` (first match or `null`).
  - `CreateAsync({Entity}Insert insert)`: the values a new row gets.
  - `UpdateAsync(id, {Entity}Update update)`: every property nullable; `null` means "leave as it is". A state change such as publishing is an update (`new PageContentUpdate { Status = Published }`), not its own method.
  - `DeleteAsync(id)`.
- `{Entity}Insert` and `{Entity}Update` live next to the entity in `Forma.Application/Entities`.
- No rules, checks or workflows in a core service. Those go in a **supporting service** (e.g. `PageContentValidationService`) that calls core services, or in the endpoint when it is just a few core-service calls in a row.

### Filters guard against IDOR

Read through filters, and narrow each read by what was already checked, so an id from the request can't reach another website's data:

```csharp
var page = await pageCoreService.GetPageAsync(new PageFilter { Id = pageId, WebsiteId = websiteId });
var content = await pageContentCoreService.GetPageContentAsync(new PageContentFilter { PageId = page.Id });
```

### Repositories are CRUD only

- Keep the database layer thin so it can be reused for many use cases: `Get…Async(filter)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`. No business logic, no multi-step workflows, no special-purpose queries like `SaveDraftAsync`.
- Every repository has a `{Entity}Filter` (all properties optional) and a `{Entity}QueryExtensions.ApplyFilter` that chains one small `With…` method per property using `WhereIf`. Reuse `ApplyFilter`; don't write ad-hoc `Where` clauses in the repository methods.
- The build enforces this (`Forma.Analyzers`, errors in `Forma.Infrastructure`): **FORMA001** no `Where` or predicate overload (`AnyAsync(x => …)`, `FirstOrDefaultAsync(x => …)`…) on a query outside the `*QueryExtensions` classes; **FORMA002** every query a `*Repository` runs on a `DbSet` (and `Find`) goes through `ApplyFilter`, with an empty filter to read everything; **FORMA003** a repository only has `Get{Entities}Async({Entity}Filter)` returning a list, and `Add…`, `Update…` and `Delete…`. There's no `GetByIdAsync` or `ExistsAsync`: the core service calls the plural read with a filter and takes the first match. Add a filter property instead of suppressing them.

### Validation

- Validate requests with a FluentValidation validator on the request DTO (`.WithValidation<TRequest>()`), which answers `400` `application/problem+json`. Rules that need lookups (e.g. "the image exists") run in a supporting service called from the endpoint, and errors also go back as a `400` `ValidationProblem`.
- No custom exception handlers or global property-name resolvers to shape validation errors: use what ASP.NET Core and FluentValidation give.
- Write a rule's message next to the rule (`.Must(DocumentRules.IsLink).WithMessage("…")`). Shared helpers may hold predicates and limits, not hidden rule chains.

### Code style

- Small pieces with room around them: one rule or step per statement, blank lines between them, small private methods for anything with a name.
- No braces around a single-statement `if`:

  ```csharp
  if (page is null)
      return TypedResults.NotFound();
  ```

### Tests (`Forma.Tests`)

- Build test data with builders/fixtures (`PageDocumentBuilder`), not with shared JSON files or by poking `JsonNode["key"]`.
- Read responses as the API's DTOs and compare with the expected result, as real JSON where the shape matters (`JsonAssert.Equal(expectedJson, actual)`).

## App (`src/app`)

- A composable per entity or concern (`usePageContent`, `useSavedItems`, `useStudioMedia`, `useStudioHistory`, …), each returning only what it is about. A large feature composable such as `useStudio` puts those together instead of holding everything itself.
- API calls go through a querier (`src/queriers/use…Querier.ts`) built on the generated client; run `npm run generate:api -w forma-app` after changing the API.
