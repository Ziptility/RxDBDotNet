import { toTypedRxJsonSchema, RxJsonSchema } from 'rxdb';

const workspaceSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      maxLength: 36
    },
    name: {
      type: 'string'
    },
    updatedAt: {
      type: 'string',
      format: 'date-time'
    },
    isDeleted: {
      type: 'boolean'
    }
  },
  required: ['id', 'name', 'updatedAt', 'isDeleted']
} as const;

const userSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      maxLength: 36
    },
    firstName: {
      type: 'string'
    },
    lastName: {
      type: 'string'
    },
    email: {
      type: 'string'
    },
    role: {
      type: 'string',
      enum: ['User', 'Admin', 'SuperAdmin']
    },
    workspaceId: {
      type: 'string',
      maxLength: 36
    },
    updatedAt: {
      type: 'string',
      format: 'date-time'
    },
    isDeleted: {
      type: 'boolean'
    }
  },
  required: ['id', 'firstName', 'lastName', 'email', 'role', 'workspaceId', 'updatedAt', 'isDeleted']
} as const;

const liveDocSchemaLiteral = {
  version: 0,
  primaryKey: 'id',
  type: 'object',
  properties: {
    id: {
      type: 'string',
      maxLength: 36
    },
    content: {
      type: 'string'
    },
    ownerId: {
      type: 'string',
      maxLength: 36
    },
    workspaceId: {
      type: 'string',
      maxLength: 36
    },
    updatedAt: {
      type: 'string',
      format: 'date-time'
    },
    isDeleted: {
      type: 'boolean'
    }
  },
  required: ['id', 'content', 'ownerId', 'workspaceId', 'updatedAt', 'isDeleted']
} as const;

export type WorkspaceDocType = typeof workspaceSchemaLiteral.properties;
export type UserDocType = typeof userSchemaLiteral.properties;
export type LiveDocDocType = typeof liveDocSchemaLiteral.properties;

export const workspaceSchema: RxJsonSchema<WorkspaceDocType> = toTypedRxJsonSchema(workspaceSchemaLiteral);
export const userSchema: RxJsonSchema<UserDocType> = toTypedRxJsonSchema(userSchemaLiteral);
export const liveDocSchema: RxJsonSchema<LiveDocDocType> = toTypedRxJsonSchema(liveDocSchemaLiteral);