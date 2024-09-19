// src\errors\customErrors.ts
export class LiveDocsError extends Error {
  constructor(
    message: string,
    public code: string
  ) {
    super(message);
    this.name = 'LiveDocsError';
  }
}

export class AuthenticationError extends LiveDocsError {
  constructor(message: string) {
    super(message, 'AUTHENTICATION_ERROR');
    this.name = 'AuthenticationError';
  }
}

export class ReplicationError extends LiveDocsError {
  constructor(message: string) {
    super(message, 'REPLICATION_ERROR');
    this.name = 'ReplicationError';
  }
}
