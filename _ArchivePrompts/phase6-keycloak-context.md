# Keycloak Integration Context (State Holder)

> **Goal**: Integrate Keycloak centralized IAM into DuskOfCoding via .NET Aspire.
> **Source**: `keycloak-master-prompt.md`

## 📈 Status tracker

- [x] **Phase 1: Aspire Orchestration (AppHost)**
  - [x] Install `Keycloak.AuthServices.Aspire.Hosting`
  - [x] Add Keycloak container resource inside `AppHost/Program.cs`
  - [x] Configure `DuskOfCodingRealm` (clients, test user)
  - [x] Inject Keycloak OIDC reference to `WebApi` and `WebUi`

- [x] **Phase 2: Securing WebApi**
  - [x] Install `Aspire.Keycloak.Authentication`
  - [x] Register `AddKeycloakJwtBearer` and Middleware
  - [x] Add `[Authorize]` endpoints

- [x] **Phase 3: Securing WebUi**
  - [x] Install `Aspire.Keycloak.Authentication` and `OpenIdConnect`
  - [x] Configure `.AddKeycloakOpenIdConnect` with `SaveTokens = true`
  - [x] Attach Bearer tokens in `ApiClient.cs`
  - [x] Add `LoginDisplay.razor`, Authentication UI logic, and `App.razor` `<CascadingAuthenticationState>`
