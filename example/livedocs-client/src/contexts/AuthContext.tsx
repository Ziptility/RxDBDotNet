// src/contexts/AuthContext.tsx
import React, { useState, useEffect, useCallback, ReactNode } from 'react';
import { User, Workspace } from '@/lib/schemas';
import { getDatabase } from '@/lib/database';
import { RxDocument, RxCollection } from 'rxdb';
import { createTypedContext } from '@/utils/createTypedContext';
import { handleAsyncError, handleError } from '@/utils/errorHandling';
import { LiveDocsDatabase } from '@/types';

interface AuthContextType {
  currentUser: User | null;
  currentWorkspace: Workspace | null;
  jwtAccessToken: string | null;
  isLoggedIn: boolean;
  isInitialized: boolean;
  login: (userId: string, workspaceId: string) => Promise<void>;
  logout: () => void;
  workspaces: Workspace[];
  users: User[];
  fetchWorkspaces: () => Promise<void>;
  fetchUsers: (workspaceId: string) => Promise<void>;
}

const [useAuth, AuthProvider] = createTypedContext<AuthContextType>();

interface AuthProviderComponentProps {
  children: ReactNode;
}

const AuthProviderComponent: React.FC<AuthProviderComponentProps> = ({ children }): JSX.Element => {
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [jwtAccessToken, setJwtAccessToken] = useState<string | null>(null);
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [isInitialized, setIsInitialized] = useState<boolean>(false);
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [users, setUsers] = useState<User[]>([]);

  const initializeDatabase = useCallback(async (): Promise<void> => {
    await handleAsyncError(async () => {
      await getDatabase();
    }, 'Initializing database');
  }, []);

  const login = useCallback(async (userId: string, workspaceId: string): Promise<void> => {
    const result = await handleAsyncError(async () => {
      const db: LiveDocsDatabase = await getDatabase();
      const usersCollection: RxCollection<User> = db.users;
      const workspacesCollection: RxCollection<Workspace> = db.workspaces;

      const user: RxDocument<User> | null = await usersCollection.findOne({ selector: { id: userId } }).exec();
      const workspace: RxDocument<Workspace> | null = await workspacesCollection
        .findOne({ selector: { id: workspaceId } })
        .exec();

      if (user && workspace) {
        const userJson: User = user.toJSON();
        const workspaceJson: Workspace = workspace.toJSON();

        localStorage.setItem('userId', userId);
        localStorage.setItem('workspaceId', workspaceId);
        localStorage.setItem('jwtAccessToken', userJson.jwtAccessToken ?? '');

        setCurrentUser(userJson);
        setCurrentWorkspace(workspaceJson);
        setJwtAccessToken(userJson.jwtAccessToken ?? null);
        setIsLoggedIn(true);

        return { user: userJson, workspace: workspaceJson };
      } else {
        throw new Error('Invalid user or workspace');
      }
    }, 'Login');

    if (!result) {
      throw new Error('Login failed');
    }
  }, []);

  useEffect((): void => {
    const initializeAuth = async (): Promise<void> => {
      await initializeDatabase();
      const storedUserId = localStorage.getItem('userId');
      const storedWorkspaceId = localStorage.getItem('workspaceId');

      if (storedUserId && storedWorkspaceId) {
        try {
          await login(storedUserId, storedWorkspaceId);
        } catch (error) {
          localStorage.removeItem('userId');
          localStorage.removeItem('workspaceId');
          handleError(error, 'Initializing authentication');
        }
      }

      setIsInitialized(true);
    };

    void initializeAuth();
  }, [login, initializeDatabase]);

  const fetchWorkspaces = useCallback(async (): Promise<void> => {
    const result = await handleAsyncError(async () => {
      const db: LiveDocsDatabase = await getDatabase();
      const workspacesCollection: RxCollection<Workspace> = db.workspaces;
      return await workspacesCollection.find().exec();
    }, 'Fetching workspaces');

    if (result) {
      setWorkspaces(result);
    } else {
      throw new Error('Failed to fetch workspaces');
    }
  }, []);

  const fetchUsers = useCallback(async (workspaceId: string): Promise<void> => {
    const result = await handleAsyncError(async () => {
      const db: LiveDocsDatabase = await getDatabase();
      const usersCollection: RxCollection<User> = db.users;
      return await usersCollection.find({ selector: { workspaceId } }).exec();
    }, 'Fetching users');

    if (result) {
      setUsers(result);
    } else {
      throw new Error('Failed to fetch users');
    }
  }, []);

  const logout = useCallback((): void => {
    localStorage.removeItem('userId');
    localStorage.removeItem('workspaceId');
    localStorage.removeItem('jwtAccessToken');
    setCurrentUser(null);
    setCurrentWorkspace(null);
    setJwtAccessToken(null);
    setIsLoggedIn(false);
  }, []);

  const contextValue: AuthContextType = {
    currentUser,
    currentWorkspace,
    jwtAccessToken,
    isLoggedIn,
    isInitialized,
    login,
    logout,
    workspaces,
    users,
    fetchWorkspaces,
    fetchUsers,
  };

  return <AuthProvider value={contextValue}>{children}</AuthProvider>;
};

export { useAuth, AuthProviderComponent as AuthProvider };
