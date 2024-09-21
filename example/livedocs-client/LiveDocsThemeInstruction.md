# LiveDocs Application Theme and Design Philosophy

## Core Design Principle

1. Adhere to Material Design 3 (Material You) principles while maintaining the unique identity of LiveDocs.
2. Prioritize adaptability, accessibility, and user-centric design in all decisions.
3. Leverage dynamic theming to create a personalized and cohesive user experience.

## Theme Implementation

1. Utilize `@material/material-color-utilities` for generating dynamic color schemes based on a key color.
2. Reference `theme.ts` as the source of truth for all theme configurations.
3. Implement both light and dark themes with smooth transitions between them.

## Color System

1. Use a dynamic color system that generates a full palette based on a key color.
2. Implement color roles as defined in Material Design 3 for consistent application across the UI.
3. Support both light and dark themes, allowing for user preference and system settings.
4. Ensure all color combinations meet WCAG 2.1 AA standards for contrast.

## Typography

1. Adopt Material Design 3's updated type scale, including new styles like "Display" for large headlines.
2. Implement responsive typography that adjusts based on screen size.
3. Use the system font stack as defined in `theme.ts`, with fallbacks for cross-platform consistency.

## Shape and Elevation

1. Implement Material Design 3's shape system with variable corner styles for different component sizes.
2. Use shape to create distinct hierarchies in the UI.
3. Apply a consistent elevation system using Material Design 3 guidelines to create depth and focus.

## Component Styling

1. Update existing components in `StyledComponents.tsx` to align with Material Design 3 specifications.
2. Implement new Material Design 3 components such as the Navigation Bar and Search Bar.
3. Ensure all components adapt to both light and dark themes.

## Layout and Spacing

1. Implement Material Design 3's adaptive layout grids for consistent spacing across devices.
2. Use responsive design patterns to ensure optimal layout on various screen sizes and orientations.

## Motion and Animation

1. Implement Material Design 3's motion system for transitions and micro-interactions.
2. Use meaningful motion to guide users, provide feedback, and reinforce hierarchies.

## Iconography

1. Adopt the Material Design 3 icon set with variable stroke weights.
2. Implement variable icon weights to match typography and create better visual harmony.

## Accessibility

1. Ensure all interactive elements have a minimum touch target size of 48x48dp.
2. Implement robust keyboard navigation support throughout the application.
3. Use semantic HTML and ARIA attributes where appropriate to enhance screen reader compatibility.

## Continuous Improvement

1. Regularly review and update the theme implementation to align with the latest Material Design specifications.
2. Seek opportunities to enhance the user experience through thoughtful application of Material Design principles.

## AI-Specific Instructions

1. When generating or modifying UI components, always refer to the latest Material Design 3 specifications.
2. Utilize `@material/material-color-utilities` for color-related calculations and theme generation.
3. Ensure all generated code adheres to accessibility standards and responsive design principles.
4. When in doubt, defer to Material Design 3 guidelines while maintaining the core functionality and identity of LiveDocs.

## Evaluation Criteria

When assessing design decisions or generated code, consider:

1. Does it align with Material Design 3 principles?
2. Does it utilize the dynamic theming capabilities provided by `@material/material-color-utilities`?
3. Is it accessible and responsive across different devices and user preferences?
4. Does it maintain consistency with existing LiveDocs design patterns?
5. Does it enhance the user's ability to complete their tasks efficiently?

Apply these guidelines in all interactions related to the LiveDocs application's design and development. Continuously evaluate against Material Design 3 principles while ensuring the unique identity and functionality of LiveDocs are maintained.
