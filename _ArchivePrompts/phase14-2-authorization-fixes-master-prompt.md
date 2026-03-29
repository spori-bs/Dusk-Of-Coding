# Phase 14-2: Authorization Fixes and Refinements

## Objective
Finalize the authorization and role-based access control (RBAC) implementation in the PracticePlatform project.

## Goals
- **Role Refactoring**: Transition all "magic string" role checks (admin, tutor, student) into a centralized `AppRoles` constants class.
- **Claim Mapping**: Fix OIDC/JWT claim mapping by disabling `DefaultMapInboundClaims` and explicitly setting the `RoleClaimType` to `roles`.
- **Login Experience**: Implement an immediate server-side authentication challenge to eliminate the two-step redirection delay.
- **Execution Resilience**: Resolve `403 Forbidden` issues for privileged users and fix `400 Bad Request` errors in the execution engine when tasks lack test bundles.
- **Tutor Hardening**: Update the Socratic Tutor prompt with stricter guardrails and optimize model parameters.

## Status
Completed and Committed.
