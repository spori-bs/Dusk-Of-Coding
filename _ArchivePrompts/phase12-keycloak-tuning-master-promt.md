# Keycloak Tuning & UI Refinement - Master Prompt

## Objective
Act as a world-class principal engineer with extensive experience in Keycloak and Blazor. Your goal is to deeply analyze, rethink, and robustly implement the Keycloak integration and associated UI/UX elements. The authentication and authorization flow must be exceptionally seamless, secure, and resilient.

## Context
The platform utilizes Keycloak for IAM and is deployed using Podman for some components. We have encountered issues like HTTP 431 (Request Header Fields Too Large) and need a bomb-proof Keycloak integration that handles load balancing, local development, and production scenarios flawlessly. Furthermore, the UI must intelligently reflect the user's authentication state and privileges without confusing visual artifacts.

## Implementation Roadmap

### Phase 1: Keycloak Workflow & Architecture Deep Dive
1. **Analyze Authentication Flow**: Deeply analyze the current Keycloak workflow and methods used for solving HTTP 431 errors and token chunking.
2. **Action Plan**: If the current implementation is suboptimal, create a robust plan to correct it.
3. **Seamless & Secure Strategy**: Rethink every scenario (login, logout, token refresh, multi-tab access, expired sessions) to ensure the flow is exceptionally seamless and secure.
4. **Load Balancing & Resilience**: Design for load balancing and robust operation within our Podman container ecosystem. The Keycloak integration must be "robust as hell."

### Phase 2: Authorization & UI Intelligence
1. **Role-Based Visibility**: If a function (e.g., managing tasks) is not available to the current user's role, the site MUST hide it entirely. Only available features should be rendered.
2. **Access Denied & Localization**: If a user attempts to access an unauthorized page via a direct link, the localization and user message must be correct and human-like.
3. **Visual Clean-up**: Ensure visual elements are flawless. For instance, inaccessible sites must not show duplicate/multiple login buttons.

### Phase 3: Login UX & Navigation Flow
1. **Clear Login Actions**: There is currently no clear login button, only a "Get Started" function. Change this to a clear, unambiguous **Login** button.
2. **Smart Redirects (ReturnUrl)**: If a user navigates to a protected page (e.g., Practice page) and login is necessary, the redirect post-login must go back to that specific page.
3. **Persistent User Identity**: The `userid` (or username) must be clearly visible on every page after login.
4. **Privilege Indicators**: If a privileged user (e.g., admin) is logged in, create a clearly visible sign on every page so the user knows if they are an admin or hold any other privileged role.

## UX & Design Guidelines
- **Premium Aesthetics**: Maintain the modern dark aesthetic with glassmorphism.
- **Fail-safes**: Use `AuthorizeView` and policy checks diligently.
- **Localization**: Ensure all new messages (unauthorized access, privilege badges) use `IStringLocalizer` for English and Hungarian translations.
