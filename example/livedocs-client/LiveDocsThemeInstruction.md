# LiveDocs Application Theme and Design Philosophy

## Core Design Principle

1. Adhere to the philosophy: "Perfection is achieved when there is nothing left to take away."
2. Prioritize simplicity and minimalism in all design decisions.
3. Question the necessity of every element in the interface.
4. Utilize shades of purple as the primary color palette to create a unique and cohesive visual identity.

## Theme Consistency

1. Reference `theme.ts` as the source of truth for all theme configurations.
2. Utilize `globals.css` for baseline styles, CSS variables, and utility classes.
3. Prefer styled components from `StyledComponents.tsx` for consistent styling across the application.
4. Do not modify `theme.ts` or `globals.css` without explicit instruction to do so.

## Color Palette

1. Use the dark mode color scheme defined in `theme.ts`.
2. Primary color: #ce93d8 (light purple)
3. Secondary color: #9c27b0 (deep purple)
4. Background color: #121212
5. Surface color: #1e1e1e
6. Text colors: Primary #ffffff, Secondary #b0bec5
7. Accent colors (use sparingly):
   - Error: #f44336
   - Success: #66bb6a
   - Warning: #ffa726
   - Info: #29b6f6

## Typography

1. Use the system font stack defined in `theme.ts` and `globals.css`.
2. Adhere to heading sizes and weights as specified in the theme.
3. Limit font variations to maintain consistency.

## Component Styling

1. Prioritize use of components from `StyledComponents.tsx`.
2. Create new styled components only when existing ones are insufficient.
3. Ensure new components align with the established design system.
4. Use `sx` prop for minor, one-off styling needs.

## Layout and Spacing

1. Apply the theme's spacing function for margins and padding.
2. Utilize `PageContainer`, `ContentPaper`, and `ListContainer` for consistent layout structures.
3. Use `SpaceBetweenBox` and `CenteredBox` for flexible layouts.
4. Maintain ample white space to enhance readability and focus.

## Forms and Inputs

1. Implement `StyledTextField`, `StyledFormControl`, and `StyledSelect` for form inputs.
2. Use `PrimaryButton` and `SecondaryButton` for form actions.
3. Ensure consistency in styling for labels, hints, and error messages.
4. Minimize the number of form fields to essential inputs only.

## Error Handling and Feedback

1. Apply `ErrorText` component for inline error messages.
2. Use `StyledAlert` for prominent notifications.
3. Craft error messages to be clear, concise, and actionable.

## Loading States

1. Implement `StyledCircularProgress` for loading indicators.
2. Center loading indicators using `CenteredBox`.

## Tables and Lists

1. Use `StyledTableCell` and `StyledTableRow` for consistent table styling.
2. Implement `ListContainer` to wrap list-like components.

## Responsive Design

1. Ensure all layouts and components are responsive.
2. Utilize Material-UI's responsive utilities and breakpoints.
3. Prioritize critical information and functions on smaller screens.

## Accessibility

1. Maintain WCAG 2.1 AA compliance.
2. Ensure sufficient color contrast within the dark color scheme.
3. Provide clear focus indicators for keyboard navigation.
4. Utilize the `.visually-hidden` class from `globals.css` for screen reader-only content.

## Code Organization

1. Maintain styled components in `StyledComponents.tsx`.
2. Use `globals.css` for baseline styles and utility classes.
3. Keep theme configuration in `theme.ts`.

## Dark Mode Implementation

1. The application uses a dark mode theme by default.
2. Ensure all new components and styles are designed with dark mode in mind.
3. Use the color variables defined in `globals.css` for consistency.
4. When adding new colors, consider their appearance in dark mode.

## Continuous Improvement

1. Regularly refactor components for simplicity and reusability.
2. Incorporate Material-UI best practices when relevant.
3. Seek opportunities to enhance the dark mode, minimalist design.
4. Consistently evaluate the necessity of each UI element.

## AI-Specific Instructions

1. When generating or modifying UI components, always refer to this document, `theme.ts`, and `StyledComponents.tsx`.
2. Prioritize simplicity in all design suggestions or code generation.
3. Before adding new elements, evaluate if existing components can fulfill the requirement.
4. When suggesting changes, provide rationale based on the core design principle.
5. Generate code that adheres to the established dark mode color palette and typography.
6. Recommend removal of unnecessary elements to align with the minimalist aesthetic.
7. Ensure all generated code and suggestions maintain accessibility standards.
8. When in doubt, err on the side of simplicity and minimalism.

## Evaluation Criteria

When assessing design decisions or generated code, consider:

1. Does it adhere to the core design principle of simplicity?
2. Does it utilize the established dark mode theme and styled components?
3. Does it maintain consistency with existing design patterns?
4. Does it enhance or detract from the user's ability to complete their task?
5. Can any element be removed without compromising functionality?
6. Does it maintain good contrast and readability in dark mode?
7. Does it effectively use the purple-focused color scheme?

Apply these guidelines in all interactions related to the LiveDocs application's design and development. Constantly evaluate against the core principle of simplicity and minimalism, while ensuring a comfortable dark mode experience with a cohesive purple-focused theme.
