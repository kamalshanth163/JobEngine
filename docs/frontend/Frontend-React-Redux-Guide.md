# JobEngine Frontend Code Guide (React + Redux)

This file explains the full frontend in simple terms, using real examples from this codebase.

## 1) What This Frontend Is

This app is a **tenant operations console** for JobEngine.

- Built with React + TypeScript + Vite.
- Uses React Router for page navigation.
- Uses Redux Toolkit for app state.
- Uses RTK Query for API calls, caching, and polling.

At a high level:

1. User lands on auth page.
2. Login/register stores auth in Redux + localStorage.
3. Protected pages load inside a shared layout.
4. Pages query/mutate backend APIs with RTK Query hooks.

---

## 2) Folder Map (Mental Model)

- `src/main.tsx`: app bootstrap (Provider + Router)
- `src/App.tsx`: route table + route protection
- `src/app/store.ts`: Redux store configuration
- `src/app/hooks.ts`: typed Redux hooks
- `src/services/api.ts`: RTK Query API definitions
- `src/features/*`: Redux slices (auth, ui, activity)
- `src/components/*`: reusable UI pieces
- `src/pages/*`: route-level screens
- `src/types/contracts.ts`: API data contracts/types

Think of pages as "screens", components as "building blocks", slices as "state containers", and `api.ts` as the "network layer".

---

## 3) App Startup Flow

In `src/main.tsx`, React mounts the app and wraps it with:

- `<Provider store={store}>` so every component can use Redux state.
- `<BrowserRouter>` so route URLs (`/jobs`, `/settings`, etc.) work.

Simple version of what happens:

```tsx
createRoot(root).render(
  <Provider store={store}>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </Provider>
)
```

This is the foundation for all React + Redux behavior in the app.

---

## 4) Routing and Protected Routes

`src/App.tsx` defines routes.

### Public route

- `/auth` (login/register)

### Protected routes

- `/dashboard`
- `/jobs`
- `/jobs/new`
- `/jobs/:jobId`
- `/api-keys`
- `/execution-lab`
- `/settings`

Protection logic:

- `ProtectedRoutes` checks `state.auth.accessToken`.
- If token exists: render `<AppShell />` (layout + nested pages)
- If missing: redirect to `/auth`

Example idea:

```tsx
const isAuthenticated = Boolean(state.auth.accessToken)
return isAuthenticated ? <AppShell /> : <Navigate to="/auth" replace />
```

This is a core React Router concept: **conditional rendering + redirect for route guards**.

---

## 5) AppShell Layout Pattern

`src/components/layout/AppShell.tsx` is the shared shell for all protected pages.

It demonstrates key React concepts:

- `Outlet` for nested route content.
- `NavLink` for active navigation styles.
- local `useState` for theme (`light`/`dark`).
- `useEffect` to sync theme to DOM + localStorage.
- Redux state usage for mobile menu and activity feed.

### Why this matters

This gives you one consistent frame (sidebar/top bar/activity panel) while page content changes inside `<Outlet />`.

---

## 6) Redux Store (Global State)

`src/app/store.ts` configures the store using `configureStore`.

Reducers in this app:

- `auth`: authentication/session data
- `ui`: temporary UI state (mobile menu)
- `activity`: local event feed
- `api`: RTK Query cache + request state

Also includes middleware:

- `api.middleware` for RTK Query behavior.

And listeners:

- `setupListeners(store.dispatch)` enables smart refetch behaviors like `refetchOnFocus`.

### Typed hooks

`src/app/hooks.ts` defines:

- `useAppDispatch`
- `useAppSelector`

These avoid repetitive type annotations in components.

---

## 7) Redux Slices Explained

## 7.1 `authSlice` (`src/features/auth/authSlice.ts`)

Stores:

- access/refresh token
- tenant info
- user email
- optional token expiry

Actions:

- `setCredentials(payload)`
- `logout()`

Important behavior:

- Reads persisted auth on startup (`readStoredAuth`).
- Writes auth changes to localStorage (`writeStoredAuth`).

Simple lifecycle:

1. Login success -> `setCredentials`
2. State updates in Redux
3. Same data persisted to localStorage
4. On refresh, initial state is restored from storage

## 7.2 `uiSlice` (`src/features/ui/uiSlice.ts`)

Minimal but useful UI state:

- `mobileMenuOpen: boolean`

Actions:

- `toggleMobileMenu`
- `closeMobileMenu`

Used in `AppShell` for responsive navigation.

## 7.3 `activitySlice` (`src/features/activity/activitySlice.ts`)

Stores client-side event log:

- array of events with id/title/detail/timestamp

Actions:

- `pushActivity`
- `clearActivity`

Neat pattern shown here:

- each event gets `crypto.randomUUID()` and timestamp
- list is capped to latest 40 events

This is a good example of **derived behavior inside reducers** with Redux Toolkit (Immer lets you write mutation-like code safely).

---

## 8) Auth Persistence Utilities

`src/features/auth/authStorage.ts` handles localStorage safely.

Important ideas:

- central storage key (`jobengine.auth`)
- parse + try/catch safety on reads
- remove corrupt storage automatically
- single place for persistence logic

This keeps storage logic out of UI components.

---

## 9) RTK Query API Layer (`src/services/api.ts`)

This file is the network hub.

### Base setup

- `createApi(...)`
- `fetchBaseQuery({ baseUrl, prepareHeaders })`
- `prepareHeaders` injects `Authorization: Bearer <token>` from Redux state.

### Endpoints defined

Mutations:

- `registerTenant`
- `login`
- `createApiKey`
- `submitJob`
- `executeJobType`

Queries:

- `getTenant`
- `listJobs`
- `getJob`

### Caching + invalidation

Tag types: `Jobs`, `Tenant`

Examples:

- `createApiKey` invalidates `Tenant` -> tenant query can refresh.
- `submitJob` invalidates jobs list tag `Jobs/LIST` -> jobs list can refresh.
- `getJob(jobId)` provides job-specific tag -> fine-grained cache control.

### Auto-generated hooks

At the bottom, RTK Query exports hooks like:

- `useListJobsQuery`
- `useSubmitJobMutation`
- `useGetJobQuery`

These hooks are directly used in pages.

---

## 10) Page-by-Page Concepts and Examples

## 10.1 Auth Page (`src/pages/AuthPage.tsx`)

Concepts shown:

- Local form state with `useState`
- Async submit handlers with mutation `.unwrap()`
- Error extraction with `useMemo`
- Conditional UI mode (`login` vs `register`)
- Redirect authenticated users with `<Navigate>`

Flow example (login):

1. User submits form
2. `useLoginMutation` call runs
3. On success: dispatch `setCredentials`
4. Push activity event
5. Navigate to `/dashboard`

## 10.2 Dashboard (`src/pages/DashboardPage.tsx`)

Concepts shown:

- data polling: `useListJobsQuery(..., { pollingInterval: 8000 })`
- live KPIs from query data
- loading placeholders with conditional rendering

This page demonstrates **read-only derived metrics** from server data.

## 10.3 Jobs List (`src/pages/JobsPage.tsx`)

Concepts shown:

- filter/search UI state with `useState`
- `useMemo` for filtered list computation
- manual `refetch()` button
- periodic polling every 5s
- reusable status badge via `StatusPill`

`StatusPill` (`src/components/jobs/StatusPill.tsx`) is a clean reusable component example.

## 10.4 New Job (`src/pages/NewJobPage.tsx`)

Concepts shown:

- controlled form inputs
- mutation submit with value normalization
- optional field handling (`|| undefined`)
- post-success navigation to job details

Pattern example:

```tsx
const jobId = await submitJob(payload).unwrap()
navigate(`/jobs/${jobId}`)
```

## 10.5 Job Details (`src/pages/JobDetailsPage.tsx`)

Concepts shown:

- route params with `useParams`
- conditional query with `skip: !jobId`
- polling details page while job runs
- robust loading/not-found/error branches

This is a good example of defensive UI for async data.

## 10.6 API Keys (`src/pages/ApiKeysPage.tsx`)

Concepts shown:

- combine auth state (`tenantId`) with API requests
- conditionally skip query until dependencies exist
- one-time display pattern for raw secret key

## 10.7 Execution Lab (`src/pages/ExecutionLabPage.tsx`)

Concepts shown:

- direct execute mutation for testing handlers
- render raw JSON response for debugging
- activity feed integration on success

## 10.8 Settings (`src/pages/SettingsPage.tsx`)

Concepts shown:

- read-only display of auth context
- environment value display (`import.meta.env...`)
- dispatching a simple command action (`clearActivity`)

## 10.9 NotFound (`src/pages/NotFoundPage.tsx`)

Simple fallback page for unknown routes (`*`).

---

## 11) React Concepts Used in This Codebase

1. Component composition
- `AppShell` composes shared layout + nested route content.

2. Controlled components
- Forms in auth/new job/execution pages bind input values to state.

3. Hooks
- `useState`: local component state
- `useEffect`: side effects (theme persistence)
- `useMemo`: memoized expensive/derived values (filters/errors)
- `useRef`: mutable values that persist without rerenders (idle timer)

4. Conditional rendering
- Loading states, error states, and auth redirects.

5. Routing
- Static + dynamic routes, route guard, nested routes with `Outlet`.

---

## 12) Redux Concepts Used in This Codebase

1. Store + slices
- App state split by domain (`auth`, `ui`, `activity`).

2. Actions and reducers
- `setCredentials`, `logout`, `toggleMobileMenu`, `pushActivity`, etc.

3. Typed selectors/dispatch
- `useAppSelector`, `useAppDispatch` for type-safe state usage.

4. RTK Query (server state)
- Declarative data fetching with generated hooks.
- Built-in request lifecycle state (`isLoading`, `error`, `data`).
- Caching + invalidation by tags.
- Polling + focus refetch support.

A useful rule in this project:

- **Redux slices** hold client/session/UI state.
- **RTK Query** manages backend/server state.

---

## 13) TypeScript Role in This App

`src/types/contracts.ts` defines shared API contracts and union types.

Benefits:

- safer API usage (request/response shape known)
- better editor IntelliSense
- fewer runtime mistakes from wrong field names/types

Example:

- `JobStatus` union limits status values to known options.
- `JobDto` ensures pages access job fields consistently.

---

## 14) End-to-End Example: "Submit Job" Journey

1. User opens `NewJobPage`.
2. Fills controlled form fields.
3. Submits -> `useSubmitJobMutation` in `api.ts` calls POST `/api/v1/jobs`.
4. Mutation invalidates jobs LIST tag.
5. Jobs list/dashboard queries can refresh with new data.
6. Activity event is pushed to local feed.
7. User is navigated to `/jobs/:jobId` for live status polling.

This single flow shows React form state + Redux action dispatch + RTK Query mutation + cache invalidation + navigation working together.

---

## 15) Quick Glossary (Beginner Friendly)

- Component: reusable UI function that returns JSX.
- JSX: syntax that looks like HTML in JavaScript/TypeScript.
- Hook: special React function for state/lifecycle logic.
- Slice: Redux Toolkit unit containing state + reducers + actions.
- Mutation: API call that changes data on server.
- Query: API call that reads data from server.
- Polling: repeated fetching every N milliseconds.
- Invalidation: mark cache stale so data refetches.

---

## 16) Practical Tips for Extending This Frontend

1. Add new backend endpoint:
- define in `src/services/api.ts`
- export generated hook
- consume hook in page/component

2. Add new global UI state:
- create/extend a slice in `src/features`
- register reducer in `src/app/store.ts`
- use typed hooks in components

3. Add new screen:
- create page in `src/pages`
- register route in `src/App.tsx`
- optionally add nav item in `AppShell.tsx`

4. Keep concerns separated:
- page: orchestration + layout
- component: focused rendering block
- slice: client state logic
- api.ts: network logic

---

If you are new to React/Redux, start by tracing one file chain:

`main.tsx` -> `App.tsx` -> `AppShell.tsx` -> one page (for example `JobsPage.tsx`) -> `services/api.ts` -> slice updates (`activitySlice.ts`).

That path makes the architecture click quickly.
