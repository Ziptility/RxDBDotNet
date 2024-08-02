import { WorkspaceDocType, UserDocType, LiveDocDocType } from '@/lib/schemas';

export interface Workspace extends WorkspaceDocType {}
export interface User extends UserDocType {}
export interface LiveDoc extends LiveDocDocType {}