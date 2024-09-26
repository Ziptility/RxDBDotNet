// src/lib/schemas.ts
import { toTypedRxJsonSchema, type RxJsonSchema } from 'rxdb';
import { type User, UserRole, type LiveDoc, type Workspace } from '@/generated/graphql';

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
    topics: {
      type: 'array',
      items: {
        type: 'string',
      },
    },
  },
  required: ['id', 'name', 'updatedAt', 'topics'],
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
    topics: {
      type: 'array',
      items: {
        type: 'string',
      },
    },
    fullName: { type: 'string', maxLength: 512 },
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
    topics: {
      type: 'array',
      items: {
        type: 'string',
      },
    },
  },
  required: ['id', 'content', 'ownerId', 'workspaceId', 'updatedAt'],
} as const;

// Create typed schemas
const workspaceSchema: RxJsonSchema<Workspace> = toTypedRxJsonSchema(workspaceSchemaLiteral);
const userSchema: RxJsonSchema<User> = toTypedRxJsonSchema(userSchemaLiteral);
const liveDocSchema: RxJsonSchema<LiveDoc> = toTypedRxJsonSchema(liveDocSchemaLiteral);

// Export schemas
export { workspaceSchema, userSchema, liveDocSchema };
