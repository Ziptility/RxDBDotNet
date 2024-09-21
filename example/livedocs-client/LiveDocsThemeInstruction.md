Certainly! I'll update the AI custom instructions to focus on optimizing LiveDocs for a desktop/laptop browser experience while maintaining adherence to Material Design 3 guidelines. Here's the revised version:

# LiveDocs Application Theme and Design Philosophy

## Core Design Principle

1. Adhere to Material Design 3 (Material You) principles, optimized for desktop/laptop browser experiences.
2. Prioritize adaptability, accessibility, and user-centric design tailored for software engineers using laptops.
3. Leverage dynamic theming to create a personalized and cohesive user experience suitable for extended desktop use.

## Target User and Environment

1. The primary user is a software engineer learning to build a RxDBDotNet client app on their laptop.
2. Optimize for larger screens (13" to 27") and higher resolutions common in desktop/laptop environments.
3. Design for extended use sessions, considering ergonomics and eye comfort for prolonged coding periods.

## Theme Implementation

1. Utilize `@material/material-color-utilities` for generating dynamic color schemes based on a key color, with consideration for larger screen estates.
2. Reference `theme.ts` as the source of truth for all theme configurations, ensuring it's optimized for desktop viewing.

## Color System

1. Use a dynamic color system that generates a full palette based on a key color, with special attention to larger UI elements typical in desktop applications.
2. Implement color roles as defined in Material Design 3, ensuring they're appropriate for extended desktop use (e.g., softer background colors for reduced eye strain).
3. Ensure all color combinations meet WCAG 2.1 AA standards for contrast, with special consideration for code readability on larger screens.

## Typography

1. Adopt Material Design 3's updated type scale, including new styles like "Display" for large headlines, optimized for desktop viewing distances.
2. Implement responsive typography that adjusts based on screen size, with a focus on readability for code snippets and documentation.
3. Use a monospace font for code elements to enhance readability and distinguish code from regular text.

## Layout and Spacing

1. Implement Material Design 3's adaptive layout grids, optimized for widescreen desktop/laptop displays.
2. Use responsive design patterns that take advantage of larger screen real estate, such as multi-column layouts and side-by-side comparisons.
3. Implement a flexible workspace that allows users to resize panels or sections, catering to different coding preferences.

## Component Styling

1. Update existing components in `StyledComponents.tsx` to align with Material Design 3 specifications, with a focus on desktop interactions (e.g., hover states, larger click targets).
2. Implement new Material Design 3 components such as the Navigation Rail instead of bottom navigation, optimized for desktop layouts.
3. Design components with consideration for keyboard shortcuts and multi-window operations common in development environments.

## Motion and Animation

1. Implement Material Design 3's motion system for transitions and micro-interactions, ensuring smooth performance on desktop hardware.
2. Use meaningful motion to guide users, provide feedback, and reinforce hierarchies, with subtler animations that don't distract from the coding experience.

## Accessibility

1. Ensure all interactive elements have appropriate sizing for mouse and trackpad interactions.
2. Implement robust keyboard navigation support throughout the application, essential for efficient desktop use.
3. Use semantic HTML and ARIA attributes to enhance screen reader compatibility, considering users who may use assistive technologies alongside development tools.

## Desktop-Specific Optimizations

1. Implement efficient keyboard shortcuts for common actions to enhance productivity.
2. Design for multi-monitor setups, allowing users to detach certain UI components into separate windows.
3. Optimize performance for handling large datasets and complex operations typical in development scenarios.
4. Include features that support development workflows, such as integrated documentation viewers and code snippet libraries.

## Continuous Improvement

1. Regularly review and update the theme implementation to align with the latest Material Design specifications, always considering the desktop/laptop context.
2. Seek opportunities to enhance the user experience through thoughtful application of Material Design principles in a development environment.

## AI-Specific Instructions

1. When generating or modifying UI components, always refer to the latest Material Design 3 specifications, adapting them for optimal desktop/laptop use.
2. Utilize `@material/material-color-utilities` for color-related calculations and theme generation, with a focus on creating a comfortable environment for extended coding sessions.
3. Ensure all generated code adheres to accessibility standards and responsive design principles, optimized for larger screens and higher resolutions.
4. When in doubt, defer to Material Design 3 guidelines while maintaining the core functionality of LiveDocs and considering the needs of software engineers in a development environment.

## Evaluation Criteria

When assessing design decisions or generated code, consider:

1. Does it align with Material Design 3 principles while optimizing for desktop/laptop use?
2. Does it utilize the dynamic theming capabilities provided by `@material/material-color-utilities` in a way that enhances the development experience?
3. Is it accessible and responsive across different desktop/laptop screen sizes and resolutions?
4. Does it maintain consistency with existing LiveDocs design patterns while catering to the needs of software engineers?
5. Does it enhance the user's ability to learn and implement an offline-first RxDBDotNet client efficiently in a desktop development environment?

Apply these guidelines in all interactions related to the LiveDocs application's design and development. Continuously evaluate against Material Design 3 principles while ensuring the functionality of LiveDocs is maintained and optimized for desktop/laptop use by software engineers.
