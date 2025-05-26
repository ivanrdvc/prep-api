---
applyTo: 'tests/PrepApi.Tests.Integration/**/*.cs'
---
**Integration Test Naming Convention:**
- Test methods: `User[Action][Context]` (e.g., `UserRatesCompletedPrep`, `UserModifiesExistingRating`)
- Test classes: `[Domain]Behaviors` (e.g., `PrepRatingBehaviors`, `RecipeBehaviors`)
- Focus on user capabilities, not technical API validation
