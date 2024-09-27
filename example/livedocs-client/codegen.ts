import type { CodegenConfig } from '@graphql-codegen/cli';

/**
 * GraphQL Code Generator Configuration
 *
 * This configuration file is used to generate TypeScript types from our GraphQL schema.
 * It's tailored to work with our strict TypeScript and ESLint configuration, ensuring
 * type safety and consistency across the project.
 *
 * @see https://www.graphql-code-generator.com/docs/config-reference/codegen-config
 */
const config: CodegenConfig = {
  // Overwrite existing files
  overwrite: true,

  // Path to our GraphQL schema
  schema: './schema/schema.graphql',

  ignoreNoDocuments: true,

  // Output configuration
  generates: {
    // Generate TypeScript types in this file
    'src/generated/graphql.ts': {
      // Use the TypeScript plugin to generate types
      plugins: ['typescript'],

      // Configuration options for the TypeScript plugin
      config: {
        // Use 'import type' for better tree-shaking
        useTypeImports: true,

        // Define custom scalar types to match our TypeScript types
        scalars: {
          DateTime: 'string',
          UUID: 'string',
          EmailAddress: 'string',
        },

        // Enum configuration
        enumsAsTypes: false,
        enumsAsConst: false,
        constEnums: false,
        enumsAsEnum: true, // Generate enums as TypeScript enums for better type safety

        // Don't generate read-only types (we handle immutability elsewhere)
        immutableTypes: false,

        // Use 'T | null' for nullable fields, matching our strict null checks
        maybeValue: 'T | null',

        // Don't generate optional fields to ensure all required fields are provided
        avoidOptionals: true,

        // Ensure scalar types are strictly typed
        strictScalars: true,

        // Skip generating __typename field to reduce noise in types
        skipTypename: true,

        // Remove duplicate fragments to reduce generated code size
        dedupeFragments: true,

        // Generate types for all schema elements, not just those used in operations
        onlyOperationTypes: false,

        // Naming conventions
        namingConvention: {
          typeNames: 'pascal-case#pascalCase', // Use PascalCase for type names
          enumValues: 'keep', // Keep original enum value names
        },

        // Generate types as exported to allow usage across the project
        noExport: false,

        // Avoid using index signatures for better type safety
        useIndexSignature: false,

        // Use interfaces instead of types to match our ESLint rules
        declarationKind: 'interface',

        // Add underscore to args type names to avoid naming conflicts
        addUnderscoreToArgsType: true,

        // Allow null or undefined for input fields for more precise typing
        inputMaybeValue: 'T | null | undefined',
      },

      // Hooks to run after file generation
      hooks: {
        afterOneFileWrite: [
          'prettier --write', // Run Prettier to ensure consistent formatting
          'eslint --fix', // Run ESLint to fix any linting issues
        ],
      },
    },
  },
};

/* eslint-disable import/no-default-export */
/* this is required for the GraphQL Code Generator */
export default config;

/**
 * Configuration Notes for Maintainers:
 *
 * 1. Scalar Types:
 *    - If you add new custom scalar types to the GraphQL schema, make sure to add them to the 'scalars' configuration.
 *    - Ensure the TypeScript types match the expected runtime types.
 *
 * 2. Enums:
 *    - We generate enums as TypeScript enums for better type safety and autocompletion.
 *    - If you need to change this behavior, adjust the enum-related configuration options.
 *
 * 3. Nullability:
 *    - The 'maybeValue' and 'inputMaybeValue' options control how nullable fields are typed.
 *    - Adjust these if you need to change how null values are handled in the generated types.
 *
 * 4. Naming Conventions:
 *    - The 'namingConvention' option ensures generated types follow our project's naming standards.
 *    - Modify this if you need to change how type names are generated.
 *
 * 5. Code Style:
 *    - We use Prettier and ESLint to ensure the generated code matches our project's style guide.
 *    - If you change code style rules, you may need to update the 'hooks' section.
 *
 * 6. Performance:
 *    - Options like 'skipTypename' and 'dedupeFragments' help reduce the size of generated code.
 *    - Consider the trade-offs if you modify these options.
 *
 * 7. Schema Changes:
 *    - When you make changes to the GraphQL schema, re-run the code generation to update the TypeScript types.
 *    - Command: `npm run generate`
 *
 * For more detailed information on configuration options, refer to the GraphQL Code Generator documentation:
 * https://www.graphql-code-generator.com/docs/plugins/typescript
 */
