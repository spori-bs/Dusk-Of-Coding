# Keycloak Integration - Master Prompt

## Objective
Implement Keycloak as the centralized Identity and Access Management (IAM) provider for the "Dusk of Coding" platform, leveraging .NET Aspire for seamless local orchestration.

## Context
The application is a Modular Monolith built with .NET 10 consisting of:
- **DuskOfCoding.AppHost**: The .NET Aspire orchestrator.
- **DuskOfCoding.WebApi**: The backend API serving the tasks and handling execution requests.
- **DuskOfCoding.WebUi**: The Blazor Server frontend UI.

Currently, the application lacks authentication. We need to secure the WebApi and provide a standardized user authentication flow in the WebUi using OpenID Connect (OIDC).

## Implementation Roadmap

### Phase 1: Aspire Orchestration (AppHost)
1. Add the `Keycloak.AuthServices.Aspire.Hosting` NuGet package to the `AppHost` project.
2. Update `Program.cs` to spin up a Keycloak container resource (`builder.AddKeycloak("keycloak")`).
3. Configure the Keycloak resource to mount or import a predefined realm configuration (`realm.json`). This realm (`DuskOfCodingRealm`) should include:
   - An OIDC public client for the `WebUi` (with appropriate redirect URIs).
   - A bearer-only client for the `WebApi`.
   - A default test user (e.g., `user: admin`, `password: admin`).
4. Inject the Keycloak endpoints into the `WebApi` and `WebUi` references so they can dynamically discover the OIDC configuration.

### Phase 2: Securing the Backend (WebApi)
1. Add the `Aspire.Keycloak.Authentication` (and standard JWT bearer) packages to `WebApi`.
2. In `Program.cs`, register authentication and authorization utilizing the Aspire Keycloak extensions (`.AddKeycloakJwtBearer(...)`).
3. Ensure `UseAuthentication()` and `UseAuthorization()` are in the middleware pipeline.
4. Protect sensitive endpoints (e.g., `/api/tasks`, `/api/execution`) by applying the `[Authorize]` attribute.

### Phase 3: Securing the Frontend (WebUi)
1. Add `Aspire.Keycloak.Authentication` and `Microsoft.AspNetCore.Authentication.OpenIdConnect` packages to `WebUi`.
2. Update `Program.cs` to configure OpenID Connect (`.AddKeycloakOpenIdConnect(...)`).
    - Map claims correctly (e.g., `preferred_username` to the user's name).
    - Enable `SaveTokens = true` so the Blazor server can access the JWT.
3. **API Client Integration**:
    - Modify the internal `ApiClient.cs` logic to utilize an `AuthenticationStateProvider` or `IHttpContextAccessor` to retrieve the saved access token and attach it as a `Bearer` header on outgoing requests to the WebApi.
4. **UI Components & Auth State**:
    - Wrap the `App.razor` routing in an `<CascadingAuthenticationState>` component.
    - Create `Components/Layout/LoginDisplay.razor`. Use the `<AuthorizeView>` component to display:
        - The user's name and a "Logout" action when authenticated.
        - A "Login" action when unauthenticated.
    - Integrate `LoginDisplay` into the `NavMenu.razor` or the top horizontal navigation bar.
    - Protect the `/practice` and `/tasks` pages using the `@attribute [Authorize]` directive.
    - Implement the backend endpoint routes (e.g., `/login`, `/logout`) required to trigger the OIDC challenges and sign-outs, since Blazor Server interactive mode requires traditional HTTP redirects to complete the OIDC flow.

## UX & Design Guidelines
- Any new UI controls (like the Login/Logout buttons in the header) must adhere to the premium **Dusk of Coding** glassmorphism aesthetic defined in `app.css` (e.g., leveraging the `.dusk-btn-outline` class).
- Ensure unauthorized API calls are handled gracefully in the frontend rather than causing unhandled Blazor exceptions.
