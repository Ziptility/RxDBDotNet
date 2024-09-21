# LiveDocs Application Design and Development Guidelines

## Core Principles

1. Adhere to Material Design 3 (MD3) principles, optimized for desktop/laptop browsers.
2. Target software engineers learning RxDBDotNet, focusing on 13" to 27" screens.
3. Prioritize accessibility, ergonomics, and extended use comfort.
4. Implement dynamic theming using `@material/material-color-utilities`.

## Implementation Guidelines

1. Use `theme.ts` as the central configuration for all theming.
2. Ensure WCAG 2.1 AA compliance for all color combinations.
3. Implement MD3's type scale, with responsive adjustments for desktop viewing.
4. Use monospace fonts for code elements.
5. Design layouts for widescreen displays, allowing flexible workspaces.
6. Implement keyboard shortcuts and multi-window support.
7. Use subtle, meaningful animations that don't distract from coding.

## Code Structure and Styling

1. Prefer standard Material-UI (MUI) components over custom ones.
2. Use MUI's `sx` prop for component-specific styles.
3. Create styled components only for frequently reused or complex styles.
4. Utilize MUI's built-in features: spacing system, breakpoints, and theming.
5. Implement responsive designs using MUI's Grid and responsive props.
6. Keep all theme configurations in `theme.ts`.

## Component-Specific Guidelines

1. Forms: Use standard MUI components with consistent props.
2. Lists/Tables: Use MUI Table components directly.
3. Cards: Use MUI Card components, customizing via `sx` prop when necessary.
4. Buttons: Use custom PrimaryButton for main actions, standard MUI Button for secondary.
5. Layout: Use MUI Box, Grid, and Container components.
6. Typography: Use MUI Typography component directly.
7. Alerts: Use MUI Alert and Snackbar components.

## Performance and Accessibility

1. Use virtualization for large datasets (e.g., react-window).
2. Ensure keyboard navigation support throughout the application.
3. Use semantic HTML and ARIA attributes for screen reader compatibility.
4. Implement proper contrast ratios for all text and UI elements.

## Code Quality

1. Maintain strict TypeScript typing throughout the codebase.
2. Provide clear documentation for custom components and complex logic.
3. Ensure consistent prop usage across similar components.
4. Optimize for simplicity and maintainability in all code generation.

## AI Assistant Specific Instructions

When generating code or making design decisions:

1. Always reference MD3 specifications, adapting for desktop/laptop use.
2. Prioritize standard MUI components and built-in features.
3. Implement responsive designs optimized for 13" to 27" screens.
4. Ensure strict type safety in TypeScript code.
5. Generate concise documentation explaining design rationales.
6. Use `@material/material-color-utilities` for color calculations.
7. Adhere to accessibility standards and responsive design principles.
8. Balance MD3 guidelines with LiveDocs' core functionality and user needs.

## Evaluation Criteria

Assess all generated code and design decisions against these criteria:

1. Alignment with MD3 principles, optimized for desktop use.
2. Effective use of dynamic theming capabilities.
3. Accessibility and responsiveness across target screen sizes.
4. Consistency with LiveDocs design patterns and engineer needs.
5. Enhancement of user ability to learn and implement RxDBDotNet.
6. Adherence to simplicity and maintainability guidelines.
7. Effective utilization of standard MUI components and features.
8. Minimization and consistency of custom styling.
9. Proper use of TypeScript features for type safety and maintainability.

Apply these guidelines to all aspects of LiveDocs development, continuously evaluating against MD3 principles while optimizing for desktop/laptop use by software engineers. Prioritize creating a codebase that is visually appealing, functional, maintainable, and tailored to the needs of desktop-based software engineers.
