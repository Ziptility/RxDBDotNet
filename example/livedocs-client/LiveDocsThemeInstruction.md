# LiveDocs Application Theme and Design Philosophy

## Core Design Principle

1. Adhere to the philosophy: "Perfection is achieved when there is nothing left to take away."
2. Prioritize simplicity and minimalism in all design decisions.
3. Question the necessity of every element in the interface.

## Theme Consistency

1. Reference `theme.ts` as the source of truth for all theme configurations.
2. Do not modify `theme.ts` without explicit instruction to do so.
3. Utilize theme values for colors, typography, spacing, and other design elements.

## Color Palette

1. Use the monochromatic color scheme defined in `theme.ts`.
2. Primary color: #212121
3. Secondary color: #757575
4. Background color: #f5f5f5
5. Surface color: #ffffff
6. Text colors: Primary #212121, Secondary #757575
7. Accent colors (use sparingly):
   - Error: #d32f2f
   - Success: #388e3c
   - Warning: #f57c00
   - Info: #1976d2

## Typography

1. Use the system font stack defined in `theme.ts`.
2. Adhere to heading sizes and weights as specified in the theme.
3. Limit font variations to maintain consistency.

## Component Styling

1. Prioritize use of components from `StyledComponents.tsx`.
2. Create new styled components only when existing ones are insufficient.
3. Ensure new components align with the established design system.
4. Use `sx` prop for minor, one-off styling needs.

## Layout and Spacing

1. Apply the theme's spacing function for margins and padding.
2. Utilize `FlexBox` and `CenteredBox` for layout structures.
3. Maintain ample white space to enhance readability and focus.

## Forms and Inputs

1. Implement `StyledTextField` and `StyledSelect` for form inputs.
2. Ensure consistency in styling for labels, hints, and error messages.
3. Minimize the number of form fields to essential inputs only.

## Error Handling and Feedback

1. Apply `ErrorText` component for error messages.
2. Use `StyledAlert` for prominent notifications.
3. Craft error messages to be clear, concise, and actionable.

## Loading States

1. Implement `StyledCircularProgress` for loading indicators.
2. Center loading indicators using `CenteredBox`.

## Responsive Design

1. Ensure all layouts and components are responsive.
2. Utilize Material-UI's responsive utilities and breakpoints.
3. Prioritize critical information and functions on smaller screens.

## Accessibility

1. Maintain WCAG 2.1 AA compliance.
2. Ensure sufficient color contrast within the monochromatic scheme.
3. Provide clear focus indicators for keyboard navigation.

## Code Organization

1. Maintain styled components in `StyledComponents.tsx`.
2. Use `globals.css` for baseline styles and utility classes.

## Continuous Improvement

1. Regularly refactor components for simplicity and reusability.
2. Incorporate Material-UI best practices when relevant.
3. Seek opportunities to enhance the monochromatic, minimalist design.

## AI-Specific Instructions

1. When generating or modifying UI components, always refer to this document and `theme.ts`.
2. Prioritize simplicity in all design suggestions or code generation.
3. Before adding new elements, evaluate if existing components can fulfill the requirement.
4. When suggesting changes, provide rationale based on the core design principle.
5. Generate code that adheres to the established color palette and typography.
6. Recommend removal of unnecessary elements to align with the minimalist aesthetic.
7. Ensure all generated code and suggestions maintain accessibility standards.
8. When in doubt, err on the side of simplicity and minimalism.

## Evaluation Criteria

When assessing design decisions or generated code, consider:

1. Does it adhere to the core design principle of simplicity?
2. Does it utilize the established theme and styled components?
3. Does it maintain consistency with existing design patterns?
4. Does it enhance or detract from the user's ability to complete their task?
5. Can any element be removed without compromising functionality?

Apply these guidelines in all interactions related to the LiveDocs application's design and development. Constantly evaluate against the core principle of simplicity and minimalism.
