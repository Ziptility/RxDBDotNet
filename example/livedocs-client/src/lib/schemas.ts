import { toTypedRxJsonSchema, RxJsonSchema, ExtractDocumentTypeFromTypedRxJsonSchema } from 'rxdb';

// Workspace Schema
const workspaceSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    name: {
      type: 'string',
    },
    updatedAt: {
      type: 'string',
      format: 'date-time',
    },
    isDeleted: {
      type: 'boolean',
    },
  },
  required: ['id', 'name', 'updatedAt', 'isDeleted'],
} as const;

// User Schema
const userSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    firstName: {
      type: 'string',
      maxLength: 256,
    },
    lastName: {
      type: 'string',
      maxLength: 256,
    },
    email: {
      type: 'string',
      format: 'email',
      maxLength: 256,
    },
    jwtAccessToken: {
      type: 'string',
      maxLength: 2000,
    },
    workspaceId: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    updatedAt: {
      type: 'string',
      format: 'date-time',
    },
    isDeleted: {
      type: 'boolean',
    },
  },
  required: ['id', 'firstName', 'lastName', 'email', 'workspaceId', 'updatedAt', 'isDeleted'],
} as const;

// LiveDoc Schema
const liveDocSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    content: {
      type: 'string',
    },
    ownerId: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    workspaceId: {
      type: 'string',
      format: 'uuid',
      maxLength: 36,
    },
    updatedAt: {
      type: 'string',
      format: 'date-time',
    },
    isDeleted: {
      type: 'boolean',
    },
  },
  required: ['id', 'content', 'ownerId', 'workspaceId', 'updatedAt', 'isDeleted'],
} as const;

// Create typed schemas
const workspaceSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof workspaceSchemaLiteral>> =
  toTypedRxJsonSchema(workspaceSchemaLiteral);

const userSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof userSchemaLiteral>> =
  toTypedRxJsonSchema(userSchemaLiteral);

const liveDocSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof liveDocSchemaLiteral>> =
  toTypedRxJsonSchema(liveDocSchemaLiteral);

// Export types
export type WorkspaceDocType = ExtractDocumentTypeFromTypedRxJsonSchema<typeof workspaceSchemaLiteral>;
export type UserDocType = ExtractDocumentTypeFromTypedRxJsonSchema<typeof userSchemaLiteral>;
export type LiveDocDocType = ExtractDocumentTypeFromTypedRxJsonSchema<typeof liveDocSchemaLiteral>;

// Export schemas
export { workspaceSchema, userSchema, liveDocSchema };

// Define and export enum for User roles
export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  SuperAdmin = 'SuperAdmin',
}
