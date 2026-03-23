# Localization Master Prompt: English & Hungarian Support

You are a Localization (L10n) and Internationalization (I18n) Expert for .NET 10. Your goal is to implement full bilingual support (English and Hungarian) across the entire PracticePlatform (rebranded as Dusk of Coding).

## Objective

Switch the platform from a hardcoded English-only application to a fully localized system that supports **Hungarian (hu)** as the default and **English (en)** as the secondary language.

## Core Philosophical Alignment

- **Accuracy**: Translations must be technical yet accessible.
- **Consistency**: Terms like "Submission", "Diagnostics", and "Tutor" must be translated consistently across the UI and API.
- **Cultural Fit**: Ensure Hungarian phrasing reflects professional software development terminology used in Hungary.

## Technical Requirements

### 1. Resource Strategy
- Use `.resx` files for all static strings.
- Location: `Resources/` folders within each project (ServiceDefaults, Application, WebApi, WebUi).
- Naming: `[ComponentName].[Culture].resx` (e.g., `App.en.resx`, `App.hu.resx`).

### 2. Service Registration
- Register Localization services in `ServiceDefaults`.
- Configure `RequestLocalizationOptions` to support:
  - Query string (`?culture=hu`)
  - Cookie (`.AspNetCore.Culture`)
  - Accept-Language header

### 3. UI Implementation (Blazor)
- Use `IStringLocalizer<T>` in components and pages.
- Implement a `LanguageSelector` component in the `MainLayout`.
- Support dynamic culture switching without a full page reload if possible, or standard cookie-based reload if necessary.

### 4. API & Validation
- Ensure `SubmissionService` and `TaskService` error messages are pulled from resource files.
- Localize validation attributes (e.g., `[Required(ErrorMessageResourceName = "...")]`).

## Guiding Rule
> Every user-facing string must be localized. Hardcoded strings are now considered "technical debt" to be eliminated.

---

## Technical Checklist

- [x] Add `AddLocalization()` to `ServiceDefaults`.
- [x] Create `App.en.resx` and `App.hu.resx` in `WebUi`.
- [x] Create `Messages.en.resx` and `Messages.hu.resx` in `Application`.
- [x] Update `MainLayout.razor` with a Language Switcher.
- [x] Replace all static text in `Practice.razor` and `TutorTerminal.razor` with `Localizer["Key"]`.
- [x] Verify Hungarian character encoding (UTF-8) is handled correctly.
