// src\lib\schemas.ts
import { toTypedRxJsonSchema, type RxJsonSchema } from 'rxdb';
import type { ExtractDocumentTypeFromTypedRxJsonSchema } from 'rxdb';

export enum UserRole {
  StandardUser = 'StandardUser',
  WorkspaceAdmin = 'WorkspaceAdmin',
  SystemAdmin = 'SystemAdmin',
}

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
  required: ['id', 'name', 'updatedAt'],
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
    role: {
      type: 'string',
      enum: Object.values(UserRole),
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
  required: ['id', 'firstName', 'lastName', 'email', 'role', 'workspaceId', 'updatedAt'],
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
  required: ['id', 'content', 'ownerId', 'workspaceId', 'updatedAt'],
} as const;

// Create typed schemas
const workspaceSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof workspaceSchemaLiteral>> =
  toTypedRxJsonSchema(workspaceSchemaLiteral);

const userSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof userSchemaLiteral>> =
  toTypedRxJsonSchema(userSchemaLiteral);

const liveDocSchema: RxJsonSchema<ExtractDocumentTypeFromTypedRxJsonSchema<typeof liveDocSchemaLiteral>> =
  toTypedRxJsonSchema(liveDocSchemaLiteral);

// Export types
export type Workspace = ExtractDocumentTypeFromTypedRxJsonSchema<typeof workspaceSchemaLiteral>;
export type User = ExtractDocumentTypeFromTypedRxJsonSchema<typeof userSchemaLiteral>;
export type LiveDoc = ExtractDocumentTypeFromTypedRxJsonSchema<typeof liveDocSchemaLiteral>;

// Export schemas
export { workspaceSchema, userSchema, liveDocSchema };
